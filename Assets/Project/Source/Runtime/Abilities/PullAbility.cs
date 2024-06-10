using UnityEngine;
using Manatea;
using Manatea.GameplaySystem;
using UnityEngine.Events;
using System.Collections;
using System;

// TODO cleanup this file

public class PullAbility : MonoBehaviour
{
    public Rigidbody Self;
    public bool m_UseTargetPosition;
    public bool m_UseDrivers;
    public float StartSpring = 10;
    public float EndSpring = 500;
    public float StartDamper = 50;
    public float EndDamper = 2;
    public float SnapPositionThreshold = 0.15f;
    public float SnapRotationThreshold = 10f;
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

    public LayerMask m_RaisingExcludeLayers;
    public LayerMask m_HandExcludeLayers;

    public float GrabTime = 0.25f;
    public float GrabTimeLeeway = 0.2f;

    public Collider m_HandBlocker;

    public GameplayAttribute m_WalkSpeedAttribute;
    public GameplayAttribute m_RotationRateAttribute;
    public GameplayAttribute m_ForceDetectionMultiplierAttribute;
    public float m_ForceDetectionMultiplier = 0.3f;

    public UnityEvent m_GrabStarted;
    public UnityEvent m_GrabEnded;

    public GrabState CurrentGrabState => m_GrabState;

    public bool m_DisableHandVerticalState;
    public bool m_DisableHandRaise;


    private Rigidbody m_Target;
    public Rigidbody Target
    {
        get => m_Target;
        set
        {
            if (enabled)
            {
                Debug.Assert(false, "Target can only be set if the ability is disabled!");
                return;
            }
            m_Target = value;
        }
    }


    private GrabState m_GrabState;

    private float m_HandVerticalState = 0;

    private GameplayAttributeOwner m_Attributes;
    private ConfigurableJoint m_Joint;
    private GrabPreferences m_Target_GrabPrefs;

    private Quaternion startRotation;
    private Quaternion targetRotation;
    private Vector3 targetLocalPosition;
    private float m_GrabTimer;

    private GameplayAttributeModifier m_WalkSpeedModifier;
    private GameplayAttributeModifier m_RotationRateModifier;
    private GameplayAttributeModifier m_ForceDetectionMultiplierModifier;

    private Vector3 m_SmoothPullingForce;

    private float m_HeavyLoad;
    private float m_PullingLoad;


    public enum GrabState
    {
        Idle = -1,
        Initializing = 0,
        EstablishGrab = 1,
        GrabEstablished = 2,
    }



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

        m_GrabState = GrabState.Initializing;

        TryGetComponent(out m_Attributes);


        // HACK solves an issue with "enableCollision" property of the joint
        Target.detectCollisions = false;



        targetLocalPosition = Vector3.zero;
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
                targetLocalPosition = Target.transform.InverseTransformPoint(bestOrientationMatch.transform.position);
                Debug.Log(targetLocalPosition);
                startRotation = transform.rotation;
                Quaternion deltaQuat = startRotation * Quaternion.Inverse(bestOrientationMatch.transform.rotation);
                targetRotation = deltaQuat * Target.rotation;
            }
        }



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



        if (!m_UseDrivers)
        {
            m_Joint.linearLimit = new SoftJointLimit() { limit = LinearLimit, contactDistance = 0.1f };
            m_Joint.linearLimitSpring = new SoftJointLimitSpring() { spring = StartSpring, damper = StartDamper };
        }
        else
        {
            m_Joint.linearLimit = new SoftJointLimit() { limit = 2.0f, contactDistance = 0.01f };

            m_Joint.xDrive = new JointDrive()
            {
                positionSpring = StartSpring,
                positionDamper = StartDamper,
                useAcceleration = true,
                maximumForce = float.MaxValue,
            };
            m_Joint.yDrive = m_Joint.xDrive;
            m_Joint.zDrive = m_Joint.xDrive;
        }

        var drive = m_Joint.slerpDrive;
        drive.positionSpring = DriverSpring;
        drive.positionDamper = DriverDamper;
        m_Joint.slerpDrive = drive;


        Target.detectCollisions = true;

        m_GrabTimer = 0;


        // TODO ignore hand blocker and hold item
        //m_Joint.connectedBody.excludeLayers |= LayerMask.NameToLayer("HandBlocker");
        //Physics.IgnoreCollision()

        m_SmoothPullingForce = m_Joint.currentForce;

        if (m_Attributes)
        {
            m_WalkSpeedModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = 1 };
            m_Attributes.AddAttributeModifier(m_WalkSpeedAttribute, m_WalkSpeedModifier);

            m_RotationRateModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = 1 };
            m_Attributes.AddAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
        }

        if (Target.TryGetComponent(out GameplayAttributeOwner targetAttOwner))
        {
            m_ForceDetectionMultiplierModifier = new GameplayAttributeModifier() { Type = GameplayAttributeModifierType.Multiplicative, Value = m_ForceDetectionMultiplier };
            targetAttOwner.AddAttributeModifier(m_ForceDetectionMultiplierAttribute, m_ForceDetectionMultiplierModifier);
        }

        m_HandBlocker.gameObject.SetActive(true);
        Target.excludeLayers |= m_HandExcludeLayers;

        m_GrabStarted.Invoke();
        m_GrabState = GrabState.EstablishGrab;
    }

    private void AttachToLocation()
    {
        Vector3 handPosWorld = m_Joint.transform.TransformPoint(m_Joint.anchor);
        if (m_Target_GrabPrefs.UpdateGrabLocation)
        {
            handPosWorld += Self.velocity * 0.01f;
        }
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
        m_Joint.connectedAnchor += targetLocalPosition;
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

            Target.excludeLayers = new LayerMask();

            StartCoroutine(CO_RemoveAttributesDelayed(m_Target));
        }

        m_Target = null;
        m_Target_GrabPrefs = null;

        m_HandBlocker.gameObject.SetActive(false);

        if (m_Attributes)
        {
            m_Attributes.RemoveAttributeModifier(m_WalkSpeedAttribute, m_WalkSpeedModifier);
            m_Attributes.RemoveAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
            m_Attributes.RemoveAttributeModifier(m_RotationRateAttribute, m_RotationRateModifier);
        }
        m_Attributes = null;

        m_GrabEnded.Invoke();
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

        if (!m_Target_GrabPrefs || !m_Target_GrabPrefs.enabled)
        {
            enabled = false;
            return;
        }

        m_GrabTimer += Time.fixedDeltaTime;

        m_SmoothPullingForce = Vector3.Lerp(m_SmoothPullingForce, m_Joint.currentForce, Time.fixedDeltaTime * 15);
        //Debug.DrawLine(m_Joint.transform.TransformPoint(m_Joint.anchor), m_Joint.transform.TransformPoint(m_Joint.anchor) + m_SmoothPullingForce * 0.2f, Color.red, Time.fixedDeltaTime);

        switch (CurrentGrabState)
        {
            case GrabState.Idle:
                Debug.LogError("Grab should be disabled, yet we are FixedUpdating!", gameObject);
                break;
            case GrabState.Initializing:
                Debug.LogError("Grab is still initializing, yet we are FixedUpdating!", gameObject);
                break;
            case GrabState.EstablishGrab:
                UpdateEstablishGrab();
                break;
            case GrabState.GrabEstablished:
                UpdateGrabEstablished();
                break;
        }

        // Check joint again, as it could have been destroyed in update steps
        if (m_Joint == null)
        {
            enabled = false;
            return;
        }

        // Orient hand blocker to hand anchor
        m_HandBlocker.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(m_Joint.anchor.normalized));
        m_HandBlocker.transform.localScale = new Vector3(1, 1, m_Joint.anchor.magnitude);
    }

    private void UpdateEstablishGrab()
    {
        if (!m_UseDrivers)
        {
            SoftJointLimitSpring linearLimitSpring = m_Joint.linearLimitSpring;
            linearLimitSpring.spring = MMath.LerpClamped(StartSpring, EndSpring, MMath.Pow(m_GrabTimer / GrabTime, 2));
            linearLimitSpring.damper = MMath.LerpClamped(StartDamper, EndDamper, MMath.Pow(m_GrabTimer / GrabTime, 2));
            m_Joint.linearLimitSpring = linearLimitSpring;
        }
        else
        {
            m_Joint.linearLimit = new SoftJointLimit() { limit = 10.002f, contactDistance = 0.01f };

            m_Joint.xDrive = new JointDrive()
            {
                positionSpring = MMath.LerpClamped(StartSpring, EndSpring, MMath.Pow(m_GrabTimer / GrabTime, 2)),
                positionDamper = MMath.LerpClamped(StartDamper, EndDamper, MMath.Pow(m_GrabTimer / GrabTime, 2)),
                useAcceleration = true,
                maximumForce = float.MaxValue,
            };
            m_Joint.yDrive = m_Joint.xDrive;
            m_Joint.zDrive = m_Joint.xDrive;
        }


        // stop grab attempt if too much time passed
        if (m_GrabTimer > GrabTime + GrabTimeLeeway)
		{
			enabled = false;
			return;
		}

#pragma warning disable
        // debug target orientation
        if (false)
        {
            Quaternion currentRot = Target.transform.rotation * Quaternion.Inverse(targetRotation) * startRotation;
            Quaternion targetRot = transform.rotation;
            Vector3 pos = m_Joint.transform.TransformPoint(m_Joint.anchor);
            DebugHelper.DrawQuaternion(pos, targetRot, 1, Time.fixedDeltaTime, false);
            DebugHelper.DrawQuaternion(pos, currentRot, 0.5f, Time.fixedDeltaTime, false);
        }
#pragma warning restore

        // check if target is in correct anchor position and orientation
        Vector3 posDelta = m_Joint.transform.TransformPoint(m_Joint.anchor) - m_Joint.connectedBody.transform.TransformPoint(m_Joint.connectedAnchor);
        bool anchorsOverlap = posDelta.magnitude < SnapPositionThreshold;
        bool driveRotationMatches = !m_Target_GrabPrefs.UseOrientations || Quaternion.Angle(transform.rotation, Target.transform.rotation * Quaternion.Inverse(targetRotation) * startRotation) < SnapRotationThreshold;
        if (anchorsOverlap && driveRotationMatches)
        {
            // line up grabbed object with it's target position and rotation
            m_Joint.connectedBody.position = (m_Joint.transform.TransformPoint(m_Joint.anchor));
            if (m_Target_GrabPrefs.UseOrientations)
            {
                m_Joint.connectedBody.rotation = transform.rotation * Quaternion.Inverse(Quaternion.Inverse(targetRotation) * startRotation);
            }
            // publish transform to copy component can use the correct transforms for attachment
            m_Joint.connectedBody.PublishTransform();

            // TODO only weld bodies this way if the target rotation has been reached!
            // TODO suuuper hacky but allows us to lock the rotation and have the current orientation persist
            // We want to fuse the hands and the object (by locking the joint instead of limiting it) to have them simulated more robustly
            Component copiedJoint = m_Joint.CopyComponent(gameObject);
            Destroy(m_Joint);
            m_Joint = copiedJoint as ConfigurableJoint;
            
            m_GrabState = GrabState.GrabEstablished;
        }
    }

    private float m_GroundCloseTimer = 0;
    private float m_RaiseAmount = 0;

    private void UpdateGrabEstablished()
    {
        m_GrabState = GrabState.GrabEstablished;

        // TODO ignore other physics items!

        // Try to raise the rigidbody if its touching the ground, by raising the anchor up
        Target.position = Target.position + Vector3.up * 0.02f;            // Skin offset to prevent sweeps not registering
        bool sweepTest = Target.SweepTest(Vector3.down, out RaycastHit hit, 0.22f);
        Target.position = Target.position - Vector3.up * 0.02f;            // Revert skin offset

        int raiseOperation = 0;
        // TODO this might cause problems
        if (sweepTest && (!hit.collider.attachedRigidbody || hit.collider.attachedRigidbody.isKinematic))
        {
            if (hit.distance > 0.2f)
            {
                raiseOperation--;
            }
            if (hit.distance < 0.1f)
            {
                raiseOperation++;
            }
            if (hit.distance < 0.02f)
            {
                raiseOperation += 10;
                m_GroundCloseTimer = float.PositiveInfinity;
            }
        }
        else
        {
            raiseOperation--;
        }
        if (raiseOperation != 0)
        {
            m_GroundCloseTimer += Time.fixedDeltaTime;
            if (m_GroundCloseTimer >= 0.02f)
            {
                m_RaiseAmount += 0.1f * raiseOperation;
                m_GroundCloseTimer = 0;
            }
        }
        else
        {
            m_GroundCloseTimer = 0;
        }
        m_RaiseAmount = MMath.Clamp(m_RaiseAmount, 0, 0.4f);


        Vector3 handRestPos = transform.InverseTransformPoint(HandTransform.position);
        Vector3 targetAnchor = m_Joint.anchor;
        targetAnchor += m_SmoothPullingForce * 20.0f;

        bool isAnchorAtRest = MMath.Approximately(m_Joint.anchor, handRestPos, 0.1f);
        // Integer for hand position (-1, 0, 1) indicating if the hand is currently up, down or neutral. Then switch between these states with some smoothing and delay.
        if (m_SmoothPullingForce.y > 0)
        {
            if ((m_SmoothPullingForce.y > 25 && isAnchorAtRest) || (m_SmoothPullingForce.y > 10 && !isAnchorAtRest))
            {
                m_HandVerticalState += 3.0f * Time.deltaTime;
                if (m_HandVerticalState > 0.5f)
                {
                    m_HandVerticalState = 1;
                }
            }
            else
            {
                m_HandVerticalState -= 3.0f * Time.deltaTime;
                if (m_HandVerticalState < 0.5f)
                {
                    m_HandVerticalState = 0;
                }
            }
        }
        else
        {
            if ((m_SmoothPullingForce.y < -70 && isAnchorAtRest) || (m_SmoothPullingForce.y < -40 && !isAnchorAtRest))
            {
                m_HandVerticalState -= 3.0f * Time.deltaTime;
                if (m_HandVerticalState < -0.5f)
                {
                    m_HandVerticalState = -1;
                }
            }
            else
            {
                m_HandVerticalState += 3.0f * Time.deltaTime;
                if (m_HandVerticalState > -0.5f)
                {
                    m_HandVerticalState = 0;
                }
            }
        }

        // TODO this might cause problems
        // Moving hand up due to different means might interfere with each other
        // Only allow one way of raising the hand. Prioritize vertical state (like hanging from the item)
        if (m_HandVerticalState != 0)
        {
            m_RaiseAmount = 0;
        }
        if (m_RaiseAmount != 0)
        {
            m_HandVerticalState = 0;
        }

        // Debug
        if (m_DisableHandVerticalState)
        {
            m_HandVerticalState = 0;
        }
        if (m_DisableHandRaise)
        {
            m_RaiseAmount = 0;
        }

        // Move hand anchor
        switch (MMath.RoundToInt(m_HandVerticalState))
        {
            case -1:
                m_Joint.anchor = Vector3.Lerp(m_Joint.anchor, new Vector3(0, -0.3f, 0.5f), 3 * Time.fixedDeltaTime);
                break;
            case 0:
                m_Joint.anchor = Vector3.Lerp(m_Joint.anchor, handRestPos + (sweepTest ? new Vector3(0, m_RaiseAmount, 0.0f) : Vector3.zero), 10 * Time.fixedDeltaTime);
                break;
            case 1:
                m_Joint.anchor = Vector3.Lerp(m_Joint.anchor, new Vector3(0, 0.7f, 0.3f), 3 * Time.fixedDeltaTime);
                break;
        }


        m_Joint.xMotion = ConfigurableJointMotion.Locked;
        m_Joint.yMotion = ConfigurableJointMotion.Locked;
        m_Joint.zMotion = ConfigurableJointMotion.Locked;

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
        if (m_Target_GrabPrefs.UseOrientations)
        {
            angularMotion = ConfigurableJointMotion.Locked;
        }
        m_Joint.angularXMotion = angularMotion;
        m_Joint.angularYMotion = angularMotion;
        m_Joint.angularZMotion = angularMotion;



        if (m_Target_GrabPrefs.UpdateGrabLocation)
        {
            AttachToLocation();
        }


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
    }


    private void OnGUI()
    {
        MGUI.DrawWorldProgressBar(transform.position, new Rect(10, 10, 50, 9), m_HeavyLoad);
        MGUI.DrawWorldProgressBar(transform.position, new Rect(10, 20, 50, 9), m_PullingLoad);
    }


    public void Throw()
    {
        Debug.Assert(enabled, "Ability is not active!", gameObject);
    
        float throwForce = ThrowForce / MMath.Max(Target.mass, 1);
        float throwRotation = ThrowRotation;
        if (m_Target_GrabPrefs.UseCustomThrow)
        {
            throwRotation = m_Target_GrabPrefs.ThrowRotation;
        }
        // Limit throw if we are still not fully grabbing on to the target
        if (m_GrabState != GrabState.GrabEstablished)
        {
            throwForce *= 0.2f;
        }

        ThrowInternal(ThrowDir * throwForce, throwRotation);
    
        enabled = false;
    }
    public void Drop()
    {
        // Initiate drop throw
        if (m_Target_GrabPrefs.UseDropThrow)
        {
            ThrowInternal(m_Target_GrabPrefs.DropForce, m_Target_GrabPrefs.DropTorque);
        } 
    
        enabled = false;
    }

    private void ThrowInternal(Vector3 velocity, float torque)
    {
        Vector3 v = transform.right * velocity.x + transform.up * velocity.y + transform.forward * velocity.z;
        StartCoroutine(CO_Throw(4, Target, v, torque));
    }

    private IEnumerator CO_Throw(int sampleCount, Rigidbody rb, Vector3 velocity, float torque)
    {
        for (int i = 0; i < sampleCount; i++)
        {
            if (!rb)
            {
                break;
            }

            float massLocationMult = MMath.RemapClamped(0.5f, 1.0f, 1, 0.6f, rb.mass);
            rb.velocity += velocity / sampleCount * massLocationMult;

            float massRotationMult = MMath.RemapClamped(0.5f, 1.0f, 1, 0.25f, rb.mass);
            Vector3 forward = velocity.normalized;
            // TODO rotate towards throwing direction
            Vector3 up = Vector3.Cross(Vector3.Cross(forward != Vector3.up ? Vector3.up : Vector3.forward, forward), forward);
            rb.AddForceAtPosition(up * torque / sampleCount * massRotationMult, rb.worldCenterOfMass + forward, ForceMode.VelocityChange);
            rb.AddForceAtPosition(-up * torque / sampleCount * massRotationMult, rb.worldCenterOfMass - forward, ForceMode.VelocityChange);

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator CO_RemoveAttributesDelayed(Rigidbody target)
    {
        yield return new WaitForSeconds(0.1f);

        if (target && target.TryGetComponent(out GameplayAttributeOwner targetAttOwner))
        {
            targetAttOwner.RemoveAttributeModifier(m_ForceDetectionMultiplierAttribute, m_ForceDetectionMultiplierModifier);
        }
    }
}