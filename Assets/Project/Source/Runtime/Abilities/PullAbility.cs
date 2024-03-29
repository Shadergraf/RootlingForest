using UnityEngine;
using Manatea;
using Manatea.AdventureRoots;
using Manatea.GameplaySystem;
using UnityEngine.Events;

// TODO cleanup this file

public class PullAbility : MonoBehaviour
{
    public Rigidbody Target;
    public Rigidbody Self;
    public float StartSpring = 10;
    public float EndSpring = 500;
    public float StartDamper = 50;
    public float EndDamper = 2;
    public float StartDriveSpring = 10;
    public float EndDriveSpring = 500;
    public float StartDriveDamper = 50;
    public float EndDriveDamper = 2;
    public float DamperSpeed = 2;
    public Transform HandTransform;
    public Vector3 TargetPosition = new Vector3(0, 0, 0);
    public bool AutoTargetPosition;
    public float ThrowForce = 5;
    public float ThrowRotation = 5;
    public Vector3 ThrowDir = new Vector3(0, 1, 1);
    public float BreakForce = 10;
    public float BreakTorque = 10;
    public float LinearLimit = 0;
    public bool EnableCollision = true;
    public Vector3 rotationAngle;
    public float DriverSpring = 1000;
    public float DriverDamper = 10;

    public float MinMovementForce = 10;
    public float MaxMovementForce = 20;
    public float OverburdenedMovementMult = 0.5f;
    public float OverburdenedRotationMult = 0.5f;
    public float MinRotationForce = 10;
    public float MaxRotationForce = 20;

    [SerializeField]
    [Tooltip("The maximum distance to raise the anchor in the y position to keep the grabed object from touching the ground.")]
    private float m_MaxRaiseDistance = 0.5f;
    [SerializeField]
    [Tooltip("How fast to raise the grabed object.")]
    private float m_RaiseSpeed = 6;

    public LayerMask m_RaisingExcludeLayers;

    public float GrabTime = 0.25f;

    public Collider m_HandBlocker;

    public GameplayAttribute m_WalkSpeedAttribute;
    public GameplayAttribute m_RotationRateAttribute;

    public UnityEvent m_GrabStarted;
    public UnityEvent m_GrabEnded;

    private GameplayAttributeOwner m_Attributes;
    private ConfigurableJoint m_Joint;
    private GrabPreferences m_Target_GrabPrefs;

    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float m_GrabTimer;

    private GameplayAttributeModifier m_WalkSpeedModifier;
    private GameplayAttributeModifier m_RotationRateModifier;

    private Vector3 m_SmoothPullingForce;

    private float m_HeavyLoad;
    private float m_PullingLoad;


    private void OnEnable()
    {
        if (!Target.TryGetComponent(out m_Target_GrabPrefs))
        {
            enabled = false;
            return;
        }
        if (Target == Self || Target.transform.IsChildOf(Self.transform))
        {
            enabled = false;
            return;
        }

        TryGetComponent(out m_Attributes);


        // HACK solves an issue with "enableCollision" property of the joint
        Target.detectCollisions = false;

        PreEstablishGrab();

        m_Joint = gameObject.AddComponent<ConfigurableJoint>();
        m_Joint.connectedBody = Target;
        m_Joint.autoConfigureConnectedAnchor = false;
        //m_Joint.enableCollision = EnableCollision;
        m_Joint.breakForce = BreakForce;
        m_Joint.breakTorque = BreakTorque;

        m_Joint.anchor = transform.InverseTransformPoint(HandTransform.position);
        if (AutoTargetPosition)
        {
            // TODO ClosestPointOnBounds uses the bounding box instead of the actual collider. This is imprecise
            if (Target.TryGetComponent(out PullSettings pullSettings))
            {
                switch (pullSettings.PullLocation)
                {
                    case PullLocation.Center:
                        m_Joint.connectedAnchor = Vector3.zero;
                        break;
                    case PullLocation.Bounds:
                        m_Joint.connectedAnchor = Target.transform.InverseTransformPoint(Target.ClosestPointOnBounds(HandTransform.position));
                        break;
                }
            }
        }
        else
        {
            m_Joint.connectedAnchor = TargetPosition;
        }
        //m_Joint.anchor = transform.InverseTransformPoint(Target.transform.TransformPoint(m_Joint.connectedAnchor));



        EstablishGrab();

        m_Joint.linearLimit = new SoftJointLimit() { limit = LinearLimit, contactDistance = 0.1f };
        m_Joint.linearLimitSpring = new SoftJointLimitSpring() { spring = StartSpring, damper = StartDamper };

        var drive = m_Joint.slerpDrive;
        drive.positionSpring = DriverSpring;
        drive.positionDamper = DriverDamper;
        m_Joint.slerpDrive = drive;


        Target.detectCollisions = true;

        m_GrabTimer = 0;


        // TODO ignore hand blocker and hold item
        //m_HandBlocker.attachedRigidbody.
        //Physics.IgnoreCollision()

        if (m_Attributes)
        {
            m_WalkSpeedModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = 1 };
            m_Attributes.AddAttributeModifier(m_WalkSpeedAttribute, m_WalkSpeedModifier);

            m_RotationRateModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = 1 };
            m_Attributes.AddAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
        }

        m_GrabStarted.Invoke();
    }

    public void EstablishGrab()
    {
        m_Joint.enableCollision = m_Target_GrabPrefs.CollisionEnabled;

        m_Joint.xMotion = ConfigurableJointMotion.Limited;
        m_Joint.yMotion = ConfigurableJointMotion.Limited;
        m_Joint.zMotion = ConfigurableJointMotion.Limited;

        // Rotation rule
        ConfigurableJointMotion angularMotion = ConfigurableJointMotion.Free;
        switch (m_Target_GrabPrefs.RotationRule)
        {
            case RotationRule.Locked:
                angularMotion = ConfigurableJointMotion.Locked; break;
            case RotationRule.Limited:
                angularMotion = ConfigurableJointMotion.Limited; break;
            case RotationRule.Free:
                angularMotion = ConfigurableJointMotion.Free; break;
        }
        m_Joint.angularXMotion = angularMotion;
        m_Joint.angularYMotion = angularMotion;
        m_Joint.angularZMotion = angularMotion;

        AttachToLocation();

        if (m_Target_GrabPrefs.RotationRule == RotationRule.Limited)
        {
            SoftJointLimit limit;

            limit = m_Joint.highAngularXLimit;
            limit.limit = m_Target_GrabPrefs.RotationLimit;
            m_Joint.highAngularXLimit = limit;

            limit = m_Joint.lowAngularXLimit;
            limit.limit = -m_Target_GrabPrefs.RotationLimit;
            m_Joint.lowAngularXLimit = limit;

            limit = m_Joint.angularYLimit;
            limit.limit = m_Target_GrabPrefs.RotationLimit;
            m_Joint.angularYLimit = limit;

            limit = m_Joint.angularZLimit;
            limit.limit = m_Target_GrabPrefs.RotationLimit;
            m_Joint.angularZLimit = limit;
        }

        if (m_Target_GrabPrefs.UseOrientations)
        {
            m_Joint.angularXDrive = new JointDrive() { useAcceleration = true, positionSpring = 1000000, positionDamper = 50000, maximumForce = 1000000 };
            m_Joint.angularYZDrive = new JointDrive() { useAcceleration = true, positionSpring = 1000000, positionDamper = 50000, maximumForce = 1000000 };
            m_Joint.rotationDriveMode = RotationDriveMode.XYAndZ;

            m_Joint.configuredInWorldSpace = true;
            m_Joint.targetRotation = targetRotation * Quaternion.Inverse(Target.transform.rotation);
        }
    }

    private void AttachToLocation()
    {
        Vector3 handPosWorld = m_Joint.transform.TransformPoint(m_Joint.anchor);
        Vector3 targetHandPos = Target.transform.InverseTransformPoint(Target.ClosestPointOnBounds(handPosWorld));
        switch (m_Target_GrabPrefs.LocationRule)
        {
            case LocationRule.Center:
                m_Joint.connectedAnchor = Vector3.zero;
                break;
            case LocationRule.Bounds:
                m_Joint.connectedAnchor = targetHandPos;
                break;
            case LocationRule.XAxis:
                m_Joint.connectedAnchor = Vector3.Project(targetHandPos, Vector3.right);
                break;
            case LocationRule.YAxis:
                m_Joint.connectedAnchor = Vector3.Project(targetHandPos, Vector3.up);
                break;
            case LocationRule.ZAxis:
                m_Joint.connectedAnchor = Vector3.Project(targetHandPos, Vector3.forward);
                break;
        }
    }

    private void OnDisable()
    {
        if (m_Joint)
        {
            Destroy(m_Joint);
        }
        m_Joint = null;

        if (Target && m_Target_GrabPrefs)
        {
            if (!m_Target_GrabPrefs.AllowOverlapAfterDrop)
            {
                bool cached_detectCollisions = Target.detectCollisions;
                Target.detectCollisions = false;
                Target.detectCollisions = cached_detectCollisions;
            }
        }

        Target = null;
        m_Target_GrabPrefs = null;


        if (m_Attributes)
        {
            m_Attributes.RemoveAttributeModifier(m_WalkSpeedAttribute, m_WalkSpeedModifier);
            m_Attributes.RemoveAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
        }
        m_Attributes = null;

        m_GrabEnded.Invoke();
    }

    public void PreEstablishGrab()
    {
        if (m_Target_GrabPrefs.UseOrientations)
        {
            float bestMatch = float.NegativeInfinity;
            GrabOrientation bestOrientationMatch = null;
            for (int i = 0; i < m_Target_GrabPrefs.Orientations.Length; i++)
            {
                float match = -Quaternion.Angle(transform.rotation, m_Target_GrabPrefs.Orientations[i].transform.rotation) / MMath.Max(MMath.Epsilon, m_Target_GrabPrefs.Orientations[i].Weight);
                if (match < bestMatch)
                {
                    continue;
                }
                bestMatch = match;
                bestOrientationMatch = m_Target_GrabPrefs.Orientations[i];
            }

            if (bestOrientationMatch != null)
            {
                Quaternion deltaQuat = transform.rotation * Quaternion.Inverse(bestOrientationMatch.transform.rotation);
                targetRotation = deltaQuat * Target.rotation;
            }
        }
    }

    private void Update()
    {
        if (m_Joint == null ||
            m_Joint.connectedBody == null)
        {
            enabled = false;
            return;
        }

        DebugHelper.DrawWireSphere(m_Joint.transform.TransformPoint(m_Joint.anchor), 0.1f, Color.green, iterations: 8);
        DebugHelper.DrawWireSphere(m_Joint.connectedBody.transform.TransformPoint(m_Joint.connectedAnchor), 0.1f, Color.red, iterations: 8);
    }
    private void FixedUpdate()
    {
        if (m_Joint == null || !Target.gameObject.activeInHierarchy)
        {
            enabled = false;
            return;
        }

        m_GrabTimer += Time.fixedDeltaTime;



        m_SmoothPullingForce = Vector3.Lerp(m_SmoothPullingForce, m_Joint.currentForce, Time.fixedDeltaTime * 20);

        Debug.DrawLine(m_Joint.transform.TransformPoint(m_Joint.anchor), m_Joint.transform.TransformPoint(m_Joint.anchor) + m_SmoothPullingForce * 0.2f, Color.red, Time.fixedDeltaTime);
        Vector3 forwardDir = transform.forward;

        float moveMult = 1;
        float rotMult = 1;

        // Heavy load
        m_HeavyLoad = MMath.InverseLerpClamped(MinMovementForce, MaxMovementForce, m_SmoothPullingForce.magnitude);
        moveMult = MMath.Lerp(moveMult, OverburdenedMovementMult, m_HeavyLoad);
        rotMult = MMath.Lerp(rotMult, OverburdenedRotationMult, m_HeavyLoad);

        // Pulling load
        m_PullingLoad = MMath.InverseLerpClamped(30, 70, m_SmoothPullingForce.magnitude) * MMath.Pow(MMath.InverseLerpClamped(0.9f, 1, Vector3.Dot(forwardDir, m_SmoothPullingForce)), 2);
        moveMult = MMath.Lerp(moveMult, MMath.RemapClamped(0.5f, 0, 1, 1.5f, Self.velocity.magnitude), m_PullingLoad);
        rotMult = MMath.Lerp(rotMult, 0, m_PullingLoad);

        m_WalkSpeedModifier.Value = moveMult;
        m_RotationRateModifier.Value = rotMult;


        SoftJointLimitSpring linearLimitSpring = m_Joint.linearLimitSpring;
        linearLimitSpring.spring = MMath.RemapClamped(0, GrabTime, StartSpring, EndSpring, m_GrabTimer);
        linearLimitSpring.damper = MMath.RemapClamped(0, GrabTime, StartDamper, EndDamper, m_GrabTimer);
        m_Joint.linearLimitSpring = linearLimitSpring;

        m_Joint.linearLimit = new SoftJointLimit() { limit = 0.002f, contactDistance = 0.01f };


        if (m_GrabTimer > GrabTime)
        {

            // TODO ignore other physics items!

            // Try to raise the rigidbody if its touching the ground, by raising the anchor up
            Target.position = Target.position + Vector3.up * 0.02f;            // Skin offset to prevent sweeps not registering
            LayerMask cachedExcludeLayers = Target.excludeLayers;
            Target.excludeLayers = m_RaisingExcludeLayers;
            float drive = -1;
            if (Target.SweepTest(Vector3.down, out RaycastHit hit))
            {
                DebugHelper.DrawWireSphere(hit.point, 0.05f, Color.red, Time.fixedDeltaTime, false);
                drive = MMath.RemapClamped(0, 0.1f, 1, -1, hit.distance);
            }
            float anchorY = m_Joint.anchor.y;
            anchorY = MMath.Clamp(anchorY + drive * m_RaiseSpeed * Time.fixedDeltaTime, 0, m_MaxRaiseDistance);
            m_Joint.anchor = m_Joint.anchor.FlattenY(anchorY);
            Target.excludeLayers = cachedExcludeLayers;
            Target.position = Target.position - Vector3.up * 0.02f;            // Revert skin offset


            m_Joint.xMotion = ConfigurableJointMotion.Locked;
            m_Joint.yMotion = ConfigurableJointMotion.Limited;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;

            if (!copiedJoint && Target.TryGetComponent(out GrabPreferences grabPrefs) && grabPrefs.UseOrientations)
            {
                // TODO only weld bodies this way if the target rotation has been reached!
                // TODO suuuper hacky but allows us to lock the rotation and have the current orientation persist
                // We want to fuse the hands and the object as much as possible to have them simulated more robustly
                copiedJoint = CopyComponent(m_Joint, gameObject);
                (copiedJoint as ConfigurableJoint).angularXMotion = ConfigurableJointMotion.Locked;
                (copiedJoint as ConfigurableJoint).angularYMotion = ConfigurableJointMotion.Locked;
                (copiedJoint as ConfigurableJoint).angularZMotion = ConfigurableJointMotion.Locked;
                Destroy(m_Joint);
                m_Joint = copiedJoint as ConfigurableJoint;
            }
        }

        if (Target.TryGetComponent(out GrabPreferences prefs))
        {
            UpdateGrab();
        }
    }

    public void UpdateGrab()
    {
        AttachToLocation();

        //if (m_Target_GrabPrefs.UseOrientations)
        //{
        //    Debug.Log(Quaternion.Angle(targetRotation * Quaternion.Inverse(Target.transform.rotation), m_Joint.connectedBody.transform.rotation));
        //}
    }

    Component copiedJoint;
    T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        var dst = destination.AddComponent(type) as T;
        var fields = type.GetFields();
        foreach (var field in fields)
        {
            if (field.IsStatic) continue;
            field.SetValue(dst, field.GetValue(original));
        }
        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
            prop.SetValue(dst, prop.GetValue(original, null), null);
        }
        return dst;
    }



    private void OnGUI()
    {
        MGUI.DrawWorldProgressBar(transform.position, new Rect(10, 10, 50, 9), m_HeavyLoad);
        MGUI.DrawWorldProgressBar(transform.position, new Rect(10, 20, 50, 9), m_PullingLoad);
    }

    public void Throw()
    {
        Debug.Assert(enabled, "Ability is not active!", gameObject);
        Destroy(m_Joint);

        float throwForce = ThrowForce / MMath.Max(Target.mass, 1);
        Vector3 throwDir = transform.right * ThrowDir.x + transform.up * ThrowDir.y + transform.forward * ThrowDir.z;
        Target.velocity += throwDir * throwForce;
        float massMult = MMath.RemapClamped(0.2f, 0.6f, 1, 0.25f, Target.mass);
        Target.AddForceAtPosition(Vector3.up * ThrowRotation * massMult, Target.worldCenterOfMass - transform.forward, ForceMode.VelocityChange);
        Target.AddForceAtPosition(Vector3.down * ThrowRotation * massMult, Target.worldCenterOfMass + transform.forward, ForceMode.VelocityChange);

        enabled = false;
    }
}