using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using Manatea.AdventureRoots;
using static UnityEngine.GraphicsBuffer;
using Manatea.GameplaySystem;
using System.Runtime.CompilerServices;
using static UnityEngine.UI.Image;

public class PullAbility : MonoBehaviour
{
    public Rigidbody Target;
    public float StartSpring = 10;
    public float EndSpring = 500;
    public float StartDamper = 50;
    public float EndDamper = 2;
    public float StartDriveSpring = 10;
    public float EndDriveSpring = 500;
    public float StartDriveDamper = 50;
    public float EndDriveDamper = 2;
    public float DamperSpeed = 2;
    public Vector3 HandPosition = new Vector3(0, .8f, .8f);
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

    public float GrabTime = 0.25f;

    public GameplayAttribute m_WalkSpeedAttribute;
    public GameplayAttribute m_RotationRateAttribute;

    private Joint m_Joint;
    private Quaternion startRotation;
    private float m_GrabTimer;

    private GameplayAttributeModifier m_WalkSpeedModifier;
    private GameplayAttributeModifier m_RotationRateModifier;

    private Vector3 m_SmoothPullingForce;

    private float m_HeavyLoad;
    private float m_PullingLoad;


    private void OnEnable()
    {
        if (!Target.TryGetComponent(out GrabPreferences grabPrefs))
        {
            enabled = false;
            return;
        }

        Quaternion startRotation = Target.rotation;

        // HACK solves an issue with "enableCollision" property of the joint
        Target.detectCollisions = false;

        grabPrefs.PreEstablishGrab(GetComponent<Rigidbody>());

        m_Joint = gameObject.AddComponent<ConfigurableJoint>();
        m_Joint.connectedBody = Target;
        m_Joint.autoConfigureConnectedAnchor = false;
        //m_Joint.enableCollision = EnableCollision;
        m_Joint.breakForce = BreakForce;
        m_Joint.breakTorque = BreakTorque;

        // Find closest point
        var colliders = Target.GetComponents<Collider>();
        Vector3 closestPoint = Vector3.one * 10000;
        for (int i  = 0; i < colliders.Length; i++)
        {
            Vector3 newPoint = colliders[i].ClosestPoint(transform.position);
            if (Vector3.Distance(transform.position, newPoint) < Vector3.Distance(transform.position, closestPoint))
                closestPoint = newPoint;
        }
        closestPoint = Target.transform.InverseTransformPoint(closestPoint);

        m_Joint.anchor = HandPosition;
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
                        m_Joint.connectedAnchor = Target.transform.InverseTransformPoint(Target.ClosestPointOnBounds(transform.TransformPoint(HandPosition)));
                        break;
                }
            }
        }
        else
        {
            m_Joint.connectedAnchor = TargetPosition;
        }
        //m_Joint.anchor = transform.InverseTransformPoint(Target.transform.TransformPoint(m_Joint.connectedAnchor));

        if (m_Joint is SpringJoint)
        {
            (m_Joint as SpringJoint).spring = StartSpring;
            (m_Joint as SpringJoint).damper = StartDamper;
        }
        if (m_Joint is ConfigurableJoint)
        {
            ConfigurableJoint configurableJoint = (ConfigurableJoint)m_Joint;

            if (Target.TryGetComponent(out GrabPreferences grabPrefss))
            {
                grabPrefss.EstablishGrab(configurableJoint);
            }

            configurableJoint.linearLimit = new SoftJointLimit() { limit = LinearLimit, contactDistance = 0.1f };
            configurableJoint.linearLimitSpring = new SoftJointLimitSpring() { spring = StartSpring, damper = StartDamper };

            var drive = configurableJoint.slerpDrive;
            drive.positionSpring = DriverSpring;
            drive.positionDamper = DriverDamper;
            configurableJoint.slerpDrive = drive;


            //configurableJoint.targetRotation = Quaternion.Inverse(startRotation);// Quaternion.Inverse(startRotation)
            //startRotation = Target.transform.rotation;

            //configurableJoint.SetTargetRotationLocal(targetRotation, startRotation);
            //configurableJoint.targetRotation = (startRotation);
        }

        //Target.rotation = startRotation;
        //Target.PublishTransform();
        Target.detectCollisions = true;

        m_GrabTimer = 0;

        if (TryGetComponent(out GameplayAttributeOwner attributes))
        {
            m_WalkSpeedModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = 1 };
            attributes.AddAttributeModifier(m_WalkSpeedAttribute, m_WalkSpeedModifier);

            m_RotationRateModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = 1 };
            attributes.AddAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
        }
    }

    private Quaternion targetRotation => Quaternion.Euler(rotationAngle);

    private void OnDisable()
    {
        if (m_Joint != null)
        {
            Destroy(m_Joint);
        }
        m_Joint = null;

        if (Target.TryGetComponent(out GrabPreferences grabPrefs) && !grabPrefs.AllowOverlapAfterDrop)
        {
            bool cached_detectCollisions = Target.detectCollisions;
            Target.detectCollisions = false;
            Target.detectCollisions = cached_detectCollisions;
        }

        Target = null;

        if (TryGetComponent(out GameplayAttributeOwner attributes))
        {
            attributes.RemoveAttributeModifier(m_WalkSpeedAttribute, m_WalkSpeedModifier);
            attributes.RemoveAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
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
        if (m_Joint == null)
        {
            enabled = false;
            return;
        }

        m_GrabTimer += Time.fixedDeltaTime;

        if (TryGetComponent(out CharacterMovement movement))
        {
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
            moveMult = MMath.Lerp(moveMult, MMath.RemapClamped(0.5f, 0, 1, 1.5f, movement.Rigidbody.velocity.magnitude), m_PullingLoad);
            rotMult = MMath.Lerp(rotMult, 0, m_PullingLoad);

            m_WalkSpeedModifier.Value = moveMult;
            m_RotationRateModifier.Value = rotMult;
        }

        if (m_Joint is ConfigurableJoint)
        {
            ConfigurableJoint configurableJoint = (ConfigurableJoint)m_Joint;

            SoftJointLimitSpring linearLimitSpring = configurableJoint.linearLimitSpring;
            linearLimitSpring.spring = MMath.RemapClamped(0, GrabTime, StartSpring, EndSpring, m_GrabTimer);
            linearLimitSpring.damper = MMath.RemapClamped(0, GrabTime, StartDamper, EndDamper, m_GrabTimer);
            configurableJoint.linearLimitSpring = linearLimitSpring;
            if (m_GrabTimer > GrabTime)
            {
                configurableJoint.linearLimit = new SoftJointLimit() { limit = 0.0f, contactDistance = 0.1f };

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
        }

        if (Target.TryGetComponent(out GrabPreferences prefs))
        {
            prefs.UpdateGrab((ConfigurableJoint)m_Joint);
        }
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
        return dst as T;
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

public static class ConfigurableJointExtensions
{
    /// <summary>
    /// Sets a joint's targetRotation to match a given local rotation.
    /// The joint transform's local rotation must be cached on Start and passed into this method.
    /// </summary>
    public static void SetTargetRotationLocal(this ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
    {
        if (joint.configuredInWorldSpace)
        {
            Debug.LogError("SetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
        }
        SetTargetRotationInternal(joint, targetLocalRotation, startLocalRotation, Space.Self);
    }

    /// <summary>
    /// Sets a joint's targetRotation to match a given world rotation.
    /// The joint transform's world rotation must be cached on Start and passed into this method.
    /// </summary>
    public static void SetTargetRotation(this ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
    {
        if (!joint.configuredInWorldSpace)
        {
            Debug.LogError("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
        }
        SetTargetRotationInternal(joint, targetWorldRotation, startWorldRotation, Space.World);
    }

    static void SetTargetRotationInternal(this ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
    {
        // Calculate the rotation expressed by the joint's axis and secondary axis
        var right = joint.axis;
        var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
        var up = Vector3.Cross(forward, right).normalized;
        Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);

        // Transform into world space
        Quaternion resultRotation = Quaternion.Inverse(worldToJointSpace);

        // Counter-rotate and apply the new local rotation.
        // Joint space is the inverse of world space, so we need to invert our value
        if (space == Space.World)
        {
            resultRotation *= startRotation * Quaternion.Inverse(targetRotation);
        }
        else
        {
            resultRotation *= Quaternion.Inverse(targetRotation) * startRotation;
        }

        // Transform back into joint space
        resultRotation *= worldToJointSpace;

        // Set target rotation to our newly calculated rotation
        joint.targetRotation = resultRotation;
    }
}
