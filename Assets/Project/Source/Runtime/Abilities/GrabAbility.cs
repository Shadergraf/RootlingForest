using UnityEngine;
using Manatea;
using Manatea.GameplaySystem;
using UnityEngine.Events;
using System.Collections;
using System;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    // TODO cleanup this file
    public class GrabAbility : MonoBehaviour
    {
        [FormerlySerializedAs("Self")]
        [SerializeField]
        public Rigidbody m_Self;
        [SerializeField]
        public bool m_UseTargetPosition;
        [SerializeField]
        public bool m_UseDrivers;
        [FormerlySerializedAs("StartSpring")]
        [SerializeField]
        public float m_StartSpring = 10;
        [FormerlySerializedAs("EndSpring")]
        [SerializeField]
        public float m_EndSpring = 500;
        [FormerlySerializedAs("StartDamper")]
        [SerializeField]
        public float m_StartDamper = 50;
        [FormerlySerializedAs("EndDamper")]
        [SerializeField]
        public float m_EndDamper = 2;
        [FormerlySerializedAs("SnapPositionThreshold")]
        [SerializeField]
        public float m_SnapPositionThreshold = 0.15f;
        [FormerlySerializedAs("SnapRotationThreshold")]
        [SerializeField]
        public float m_SnapRotationThreshold = 10f;
        [FormerlySerializedAs("StartDriveSpring")]
        [SerializeField]
        public float m_StartDriveSpring = 10;
        [FormerlySerializedAs("EndDriveSpring")]
        [SerializeField]
        public float m_EndDriveSpring = 500;
        [FormerlySerializedAs("StartDriveDamper")]
        [SerializeField]
        public float m_StartDriveDamper = 50;
        [FormerlySerializedAs("EndDriveDamper")]
        [SerializeField]
        public float m_EndDriveDamper = 2;
        [FormerlySerializedAs("DamperSpeed")]
        [SerializeField]
        public float m_DamperSpeed = 2;
        [FormerlySerializedAs("HandTransform")]
        [SerializeField]
        public Transform m_HandTransform;
        [FormerlySerializedAs("TargetPosition")]
        [SerializeField]
        public Vector3 m_TargetPosition = new Vector3(0, 0, 0);
        [FormerlySerializedAs("AutoTargetPosition")]
        [SerializeField]
        public bool m_AutoTargetPosition;
        [FormerlySerializedAs("ThrowForce")]
        [SerializeField]
        public float m_ThrowForce = 5;
        [FormerlySerializedAs("ThrowRotation")]
        [SerializeField]
        public float m_ThrowRotation = 5;
        [FormerlySerializedAs("ThrowDir")]
        [SerializeField]
        public Vector3 m_ThrowDir = new Vector3(0, 1, 1);
        [FormerlySerializedAs("BreakForce")]
        [SerializeField]
        public float m_BreakForce = 10;
        [FormerlySerializedAs("BreakTorque")]
        [SerializeField]
        public float m_BreakTorque = 10;
        [FormerlySerializedAs("LinearLimit")]
        [SerializeField]
        public float m_LinearLimit = 0;
        [FormerlySerializedAs("EnableCollision")]
        [SerializeField]
        public bool m_EnableCollision = true;
        [FormerlySerializedAs("rotationAngle")]
        [SerializeField]
        public Vector3 m_RotationAngle;
        [FormerlySerializedAs("DriverSpring")]
        [SerializeField]
        public float m_DriverSpring = 1000;
        [FormerlySerializedAs("DriverDamper")]
        [SerializeField]
        public float m_DriverDamper = 10;

        [FormerlySerializedAs("MinMovementForce")]
        [FormerlySerializedAs("OverburdenedThresholdMin")]
        [SerializeField]
        public float m_OverburdenedThresholdMin = 10;
        [FormerlySerializedAs("MaxMovementForce")]
        [FormerlySerializedAs("OverburdenedThresholdMax")]
        [SerializeField]
        public float m_OverburdenedThresholdMax = 20;
        [FormerlySerializedAs("OverburdenedMovementMult")]
        [SerializeField]
        public float m_OverburdenedMovementMult = 0.5f;
        [FormerlySerializedAs("OverburdenedRotationMult")]
        [SerializeField]
        public float m_OverburdenedRotationMult = 0.5f;

        [SerializeField]
        public LayerMask m_RaisingExcludeLayers;
        [SerializeField]
        public LayerMask m_HandExcludeLayers;

        [FormerlySerializedAs("GrabTime")]
        [SerializeField]
        public float m_GrabTime = 0.25f;
        [FormerlySerializedAs("GrabTimeLeeway")]
        [SerializeField]
        public float m_GrabTimeLeeway = 0.2f;

        [SerializeField]
        public Collider m_HandBlocker;

        [SerializeField]
        public GameplayAttribute m_WalkSpeedAttribute;
        [SerializeField]
        public GameplayAttribute m_RotationRateAttribute;
        [SerializeField]
        public GameplayAttribute m_ForceDetectionMultiplierAttribute;
        [SerializeField]
        public float m_ForceDetectionMultiplier = 0.3f;

        [SerializeField]
        public UnityEvent m_GrabStarted;
        [SerializeField]
        public UnityEvent m_GrabEnded;

        [SerializeField]
        public bool m_DisableHandVerticalState;
        [SerializeField]
        public bool m_DisableHandRaise;

        public GrabState CurrentGrabState => m_GrabState;
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


        private Rigidbody m_Target;


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
            if (Target == m_Self || Target.transform.IsChildOf(m_Self.transform))
            {
                enabled = false;
                return;
            }

            m_GrabState = GrabState.Initializing;

            m_Attributes = GetComponentInParent<GameplayAttributeOwner>();


            // HACK solves an issue with "enableCollision" property of the joint
            Target.detectCollisions = false;



            targetLocalPosition = Vector3.zero;
            if (m_Target_GrabPrefs.UseOrientations)
            {
                float bestMatch = float.NegativeInfinity;
                GrabOrientation bestOrientationMatch = null;
                for (int i = 0; i < m_Target_GrabPrefs.Orientations.Length; i++)
                {
                    float match = -Quaternion.Angle(m_HandTransform.rotation, m_Target_GrabPrefs.Orientations[i].transform.rotation);
                    match -= Vector3.Distance(m_HandTransform.position, m_Target_GrabPrefs.Orientations[i].transform.position) * 44;
                    match /= MMath.Max(MMath.Epsilon, m_Target_GrabPrefs.Orientations[i].Weight);
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
                    startRotation = transform.rotation;
                    Quaternion deltaQuat = startRotation * Quaternion.Inverse(bestOrientationMatch.transform.rotation);
                    targetRotation = deltaQuat * Target.rotation;
                }
            }



            m_Joint = m_Self.gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.connectedBody = Target;
            m_Joint.autoConfigureConnectedAnchor = false;
            //m_Joint.enableCollision = EnableCollision;
            m_Joint.breakForce = m_BreakForce;
            m_Joint.breakTorque = m_BreakTorque;

            m_Joint.anchor = transform.InverseTransformPoint(m_HandTransform.position);
            if (m_AutoTargetPosition)
            {
                // TODO ClosestPointOnBounds uses the bounding box instead of the actual collider. This is imprecise
            }
            else
            {
                m_Joint.connectedAnchor = m_TargetPosition;
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
                m_Joint.linearLimit = new SoftJointLimit() { limit = m_LinearLimit, contactDistance = 0.1f };
                m_Joint.linearLimitSpring = new SoftJointLimitSpring() { spring = m_StartSpring, damper = m_StartDamper };
            }
            else
            {
                m_Joint.linearLimit = new SoftJointLimit() { limit = 2.0f, contactDistance = 0.01f };

                m_Joint.xDrive = new JointDrive()
                {
                    positionSpring = m_StartSpring,
                    positionDamper = m_StartDamper,
                    useAcceleration = true,
                    maximumForce = float.MaxValue,
                };
                m_Joint.yDrive = m_Joint.xDrive;
                m_Joint.zDrive = m_Joint.xDrive;
            }

            var drive = m_Joint.slerpDrive;
            drive.positionSpring = m_DriverSpring;
            drive.positionDamper = m_DriverDamper;
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
                handPosWorld += m_Self.velocity * 0.01f;
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

            m_SmoothPullingForce = MMath.Damp(m_SmoothPullingForce, m_Joint.currentForce, 10, Time.fixedDeltaTime);
            Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + m_Joint.currentForce * 0.01f, Color.red);
            Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + m_SmoothPullingForce * 0.01f, Color.green);
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
                linearLimitSpring.spring = MMath.LerpClamped(m_StartSpring, m_EndSpring, MMath.Pow(m_GrabTimer / m_GrabTime, 2));
                linearLimitSpring.damper = MMath.LerpClamped(m_StartDamper, m_EndDamper, MMath.Pow(m_GrabTimer / m_GrabTime, 2));
                m_Joint.linearLimitSpring = linearLimitSpring;
            }
            else
            {
                m_Joint.linearLimit = new SoftJointLimit() { limit = 10.002f, contactDistance = 0.01f };

                m_Joint.xDrive = new JointDrive()
                {
                    positionSpring = MMath.LerpClamped(m_StartSpring, m_EndSpring, MMath.Pow(m_GrabTimer / m_GrabTime, 2)),
                    positionDamper = MMath.LerpClamped(m_StartDamper, m_EndDamper, MMath.Pow(m_GrabTimer / m_GrabTime, 2)),
                    useAcceleration = true,
                    maximumForce = float.MaxValue,
                };
                m_Joint.yDrive = m_Joint.xDrive;
                m_Joint.zDrive = m_Joint.xDrive;
            }


            // stop grab attempt if too much time passed
            if (m_GrabTimer > m_GrabTime + m_GrabTimeLeeway)
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
            bool anchorsOverlap = posDelta.magnitude < m_SnapPositionThreshold;
            bool driveRotationMatches = !m_Target_GrabPrefs.UseOrientations || Quaternion.Angle(transform.rotation, Target.transform.rotation * Quaternion.Inverse(targetRotation) * startRotation) < m_SnapRotationThreshold;
            if (anchorsOverlap && driveRotationMatches)
            {
                // line up grabbed object with it's target position and rotation
                //m_Joint.connectedBody.position = m_Joint.transform.TransformPoint(m_Joint.anchor);
                m_Joint.connectedBody.position += posDelta;
                if (m_Target_GrabPrefs.UseOrientations)
                {
                    m_Joint.connectedBody.rotation = transform.rotation * Quaternion.Inverse(Quaternion.Inverse(targetRotation) * startRotation);
                }
                // publish transform to copy component can use the correct transforms for attachment
                m_Joint.connectedBody.PublishTransform();

                // TODO only weld bodies this way if the target rotation has been reached!
                // TODO suuuper hacky but allows us to lock the rotation and have the current orientation persist
                // We want to fuse the hands and the object (by locking the joint instead of limiting it) to have them simulated more robustly
                Component copiedJoint = m_Joint.CopyComponent(m_Self.gameObject);
                Destroy(m_Joint);
                m_Joint = copiedJoint as ConfigurableJoint;

                m_GrabState = GrabState.GrabEstablished;
            }

            // Lerp to final grab walk/rotation modifiers
            if (m_Attributes)
            {
                m_WalkSpeedModifier.Value = MMath.Damp(m_WalkSpeedModifier.Value, 0.4f, 20, Time.fixedDeltaTime);
                m_RotationRateModifier.Value = MMath.Damp(m_RotationRateModifier.Value, 0.4f, 20, Time.fixedDeltaTime);
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


            Vector3 handRestPos = transform.InverseTransformPoint(m_HandTransform.position);
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
            m_HeavyLoad = MMath.InverseLerpClamped(m_OverburdenedThresholdMin, m_OverburdenedThresholdMax, m_SmoothPullingForce.magnitude);
            moveMult = MMath.Lerp(moveMult, m_OverburdenedMovementMult, m_HeavyLoad);
            rotMult = MMath.Lerp(rotMult, m_OverburdenedRotationMult, m_HeavyLoad);

            // Pulling load
            float totalPullingForce = MMath.InverseLerpClamped(30, 70, m_SmoothPullingForce.magnitude);
            float linearPullingForce = MMath.Pow(MMath.InverseLerpClamped(0.9f, 1, Vector3.Dot(forwardDir, m_SmoothPullingForce)), 2) * 0 + 1;
            m_PullingLoad = totalPullingForce * linearPullingForce;
            moveMult = MMath.Lerp(moveMult, MMath.RemapClamped(0.5f, 0, 1, 1.5f, m_Self.velocity.magnitude), m_PullingLoad);
            rotMult = MMath.Lerp(rotMult, 0, m_PullingLoad);

            // TODO rotMult for pulling load is wrong!

            // Lerp to final grab walk/rotation modifiers
            if (m_Attributes)
            {
                m_WalkSpeedModifier.Value = MMath.Damp(m_WalkSpeedModifier.Value, moveMult, 10, Time.fixedDeltaTime);
                m_RotationRateModifier.Value = MMath.Damp(m_RotationRateModifier.Value, rotMult, 10, Time.fixedDeltaTime);
            }
        }


        private void OnGUI()
        {
            MGUI.DrawWorldProgressBar(transform.position, new Rect(10, 10, 50, 9), m_HeavyLoad);
            MGUI.DrawWorldProgressBar(transform.position, new Rect(10, 20, 50, 9), m_PullingLoad);
        }


        public void Throw()
        {
            Debug.Assert(enabled, "Ability is not active!", gameObject);

            float throwForce = m_ThrowForce / MMath.Max(Target.mass, 1);
            float throwRotation = m_ThrowRotation;
            if (m_Target_GrabPrefs.UseCustomThrow)
            {
                throwRotation = m_Target_GrabPrefs.ThrowRotation;
            }
            // Limit throw if we are still not fully grabbing on to the target
            if (m_GrabState != GrabState.GrabEstablished)
            {
                throwForce *= 0.2f;
            }

            ThrowInternal(m_ThrowDir * throwForce, throwRotation);

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
}