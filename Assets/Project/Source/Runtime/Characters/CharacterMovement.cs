using Manatea.GameplaySystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : MonoBehaviour
    {
        [FormerlySerializedAs("GroundMoveForce")]
        [SerializeField]
        private float m_GroundMoveForce = 1;
        [FormerlySerializedAs("AirMoveForce")]
        [SerializeField]
        private float m_AirMoveForce = 1;
        [FormerlySerializedAs("StepHeight")]
        [SerializeField]
        private float m_StepHeight = 0.4f;
        [FormerlySerializedAs("FeetLinearResistance")]
        [SerializeField]
        private float m_FeetLinearResistance = 2;
        [FormerlySerializedAs("FeetAngularResistance")]
        [SerializeField]
        private float m_FeetAngularResistance = 2;
        [SerializeField]
        private float m_FeetLinearAirResistance = 0;
        [SerializeField]
        private float m_FeetAngularAirResistance = 0.5f;
        [FormerlySerializedAs("StationaryFriction")]
        [SerializeField]
        private float m_StationaryFriction = 1;
        [FormerlySerializedAs("MovementFriction")]
        [SerializeField]
        private float m_MovementFriction = 0.1f;
        [FormerlySerializedAs("GroundRotationForce")]
        [SerializeField]
        private float m_GroundRotationForce = 1;
        [FormerlySerializedAs("AirRotationForce")]
        [SerializeField]
        private float m_AirRotationForce = 1;
        [FormerlySerializedAs("PushForce")]
        [SerializeField]
        private float m_PushForce = 1;
        [SerializeField]
        private float m_StandUpForce = 200;

        [Header("Collision Detection")]
        [FormerlySerializedAs("Collider")]
        [SerializeField]
        private Collider m_Collider;
        [FormerlySerializedAs("GroundDetectionDistance")]
        [SerializeField]
        private float m_GroundDetectionDistance = 0.01f;
        [FormerlySerializedAs("MaxSlopeAngle")]
        [SerializeField]
        private float m_MaxSlopeAngle = 46;

        [Header("Attributes & Tags")]
        [FormerlySerializedAs("m_MoveSpeedAttribute")]
        [SerializeField]
        private GameplayAttribute m_MoveSpeedAttribute;
        [FormerlySerializedAs("m_RotationRateAttribute")]
        [SerializeField]
        private GameplayAttribute m_RotationRateAttribute;
        [SerializeField]
        private GameplayAttribute m_StandUpForceMultiplierAttribute;
        [FormerlySerializedAs("m_NoStableGroundTag")]
        [SerializeField]
        private GameplayTag m_NoStableGroundTag;
        [SerializeField]
        private GameplayTagFilter m_BlockGroundMovementFilter;

        [Header("Debug")]
        [FormerlySerializedAs("DebugLocomotion")]
        [SerializeField]
        private bool m_DebugLocomotion = false;
        [FormerlySerializedAs("DebugGroundDetection")]
        [SerializeField]
        private bool m_DebugGroundDetection = false;

        // Public
        public Rigidbody Rigidbody => m_Rigidbody;
        public GameplayAttributeOwner AttributeOwner => m_AttributeOwner;
        public GameplayTagOwner TagOwner => m_TagOwner;
        public Vector3 ScheduledMove => m_Sim.ScheduledMove;
        public Vector3 MoveSpeedMult => m_Sim.ScheduledMove;
        public RaycastHit GroundLowerHit => m_Sim.GroundLowerHit;
        public RaycastHit PreciseGroundLowerHit => m_Sim.PreciseGroundLowerHit;
        public RaycastHit GroundUpperHit => m_Sim.GroundUpperHit;
        public RaycastHit PreciseGroundUpperHit => m_Sim.PreciseGroundUpperHit;


        private Rigidbody m_Rigidbody;
        private GameplayAttributeOwner m_AttributeOwner;
        private GameplayTagOwner m_TagOwner;

        // Simulation
        private MovementSimulationState m_Sim;
        private List<ICharacterMover> m_Movers = new List<ICharacterMover>();
        private List<ICharacterMover> m_MoversToAdd = new List<ICharacterMover>();
        private List<ICharacterMover> m_MoversToRemove = new List<ICharacterMover>();

        // Constants
        /// <summary>
        /// The gap to use when testing as the characters collision
        /// </summary>
        public const float SKIN_THICKNESS = 0.001f;

        private float m_CachedMoveSpeedMult = 1;
        private float m_CachedRotRateMult = 1;
        private float m_StandUpMult = 1;
        private Dictionary<Collider, bool> m_CachedWalkableColliders = new Dictionary<Collider, bool>();

        // TODO The feet and head positions are no longer valid now that we are supporting arbitrary rotations!
        /// <summary>
        /// The caracter feet position in world space
        /// </summary>
        public Vector3 FeetPos => m_Collider.ClosestPoint(transform.position - transform.up * 10000);

        /// <summary>
        /// The caracter head (or top) position in world space
        /// </summary>
        public Vector3 HeadPos => m_Collider.ClosestPoint(transform.position + transform.up * 10000);


        public class MovementSimulationState
        {
            public readonly CharacterMovement Movement;

            // Input
            public Vector3 ScheduledMove;
            public Vector3 ScheduledLookDir;
            public Vector3 TargetLookDir;

            public bool IsStableGrounded;
            public bool IsSliding;
            public bool IsStepping;

            public float AirborneTimer;
            public float GroundTimer;
            public float UprightTimer;

            public Vector3 ContactMove;

            public float SmoothMoveMagnitude;
            public PhysicsMaterial PhysicsMaterial;

            public RaycastHit GroundLowerHit;
            public RaycastHit PreciseGroundLowerHit;
            public RaycastHit GroundUpperHit;
            public RaycastHit PreciseGroundUpperHit;

            public int GroundColliderCount = 0;
            public Collider[] GroundColliders = new Collider[32];

            public RaycastHit[] GroundHits = new RaycastHit[32];

            /// <summary>
            /// In which direction are we leaning
            /// </summary>
            public Vector3 Lean;

            public Vector3 UpVector;


            public MovementSimulationState(CharacterMovement movement)
            {
                Movement = movement;
            }
        }


        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_AttributeOwner = GetComponent<GameplayAttributeOwner>();
            m_TagOwner = GetComponent<GameplayTagOwner>();

            m_Sim = new MovementSimulationState(this);

            m_Sim.PhysicsMaterial = new PhysicsMaterial();
            m_Sim.PhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            m_Collider.material = m_Sim.PhysicsMaterial;
        }

        private void OnEnable()
        {
            m_Sim.ScheduledLookDir = transform.forward;
            m_Sim.TargetLookDir = transform.forward;
        }

        public void RegisterMover(ICharacterMover mover)
        {
            m_MoversToAdd.Add(mover);
        }
        public void UnregisterMover(ICharacterMover mover)
        {
            m_MoversToRemove.Add(mover);
        }

        public void Move(Vector3 moveVector, bool rotateTowardsMove = true)
        {
            m_Sim.ScheduledMove = moveVector;
            if (rotateTowardsMove && m_Sim.ScheduledMove != Vector3.zero)
            {
                m_Sim.ScheduledLookDir = m_Sim.ScheduledMove.normalized;
            }
        }
        public void SetTargetRotation(Vector3 targetRotation)
        {
            if (targetRotation != Vector3.zero)
            {
                m_Sim.ScheduledLookDir = targetRotation.FlattenY().normalized;
                m_Sim.TargetLookDir = m_Sim.ScheduledLookDir;
            }
        }

        private void FixedUpdate()
        {
            Debug.Assert(m_Collider, "No collider setup!", gameObject);
            Debug.Assert(m_Rigidbody, "No rigidbody attached!", gameObject);

            PreMovement();
            PrePhysics();
            UpdatePhysics(Time.fixedDeltaTime);
        }

        protected void PreMovement()
        {
            for (int i = 0; i < m_MoversToAdd.Count; i++)
                m_Movers.Add(m_MoversToAdd[i]);
            m_MoversToAdd.Clear();
            for (int i = 0; i < m_MoversToRemove.Count; i++)
                m_Movers.Remove(m_MoversToRemove[i]);
            m_MoversToRemove.Clear();
        }
        protected void PrePhysics()
        {
            m_CachedWalkableColliders.Clear();
        }

        protected void UpdatePhysics(float dt)
        {
            // Get Attributes
            m_CachedMoveSpeedMult = 1;
            m_CachedRotRateMult = 1;
            if (m_AttributeOwner)
            {
                if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_MoveSpeedAttribute, out float speed))
                {
                    m_CachedMoveSpeedMult = speed;
                }
                if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_RotationRateAttribute, out float rot))
                {
                    m_CachedRotRateMult = rot;
                }
                if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_StandUpForceMultiplierAttribute, out float standupMult))
                {
                    m_StandUpMult = standupMult;
                }
            }
            // Ground detection
            bool groundDetected = DetectGround(out m_Sim.GroundLowerHit, out m_Sim.PreciseGroundLowerHit, out m_Sim.GroundUpperHit, out m_Sim.PreciseGroundUpperHit);
            m_Sim.IsStableGrounded = groundDetected;
            m_Sim.IsSliding = m_Sim.IsStableGrounded && !IsRaycastHitWalkable(m_Sim.PreciseGroundLowerHit);     // TODO  && !m_VaultingActive
            m_Sim.IsStableGrounded &= !m_Sim.IsSliding;
            m_Sim.IsStableGrounded &= IsStandingUpRight();
            m_Sim.IsStableGrounded &= !m_TagOwner.SatisfiesTagFilter(m_BlockGroundMovementFilter, false);
            // Stepping
            if (IsObjectWalkable(m_Sim.PreciseGroundUpperHit.collider) && groundDetected && Vector3.Dot(m_Sim.PreciseGroundUpperHit.normal, m_Sim.GroundLowerHit.normal) < 0.9f && Vector3.Project(m_Sim.PreciseGroundUpperHit.point - FeetPos, transform.up).magnitude < m_StepHeight)     // TODO  && !m_VaultingActive
            {
                m_Sim.IsStepping = true;
                m_Sim.IsStableGrounded = true;
                m_Sim.IsSliding = false;
            }
            // ModifyState
            for (int i = 0; i < m_Movers.Count; i++)
                m_Movers[i].ModifyState(m_Sim, dt);

            // Update timers
            m_Sim.UprightTimer -= dt;
            if (m_Sim.IsStableGrounded)
            {
                m_Sim.GroundTimer += dt;
                m_Sim.AirborneTimer = 0;
                m_Sim.UprightTimer = MMath.Max(m_Sim.UprightTimer, 0.5f);
            }
            else
            {
                m_Sim.GroundTimer = 0;
                m_Sim.AirborneTimer += dt;
            }
            m_Sim.SmoothMoveMagnitude = MMath.Damp(m_Sim.SmoothMoveMagnitude, m_Sim.ScheduledMove.magnitude, 1, dt * 2);
            // UpdateTimers
            for (int i = 0; i < m_Movers.Count; i++)
                m_Movers[i].UpdateTimers(m_Sim, dt);

            m_Sim.ContactMove = m_Sim.ScheduledMove;

            // The direction we want to move in
            if (m_DebugLocomotion)
            {
                Debug.DrawLine(transform.position, transform.position + m_Sim.ScheduledMove, Color.blue, dt, false);
                if (groundDetected)
                    Debug.DrawLine(m_Sim.PreciseGroundLowerHit.point, m_Sim.PreciseGroundLowerHit.point + m_Sim.PreciseGroundLowerHit.normal, Color.black, dt, false);

                if (m_StepHeight > 0)
                {
                    DebugHelper.DrawWireCircle(FeetPos + Vector3.up * m_StepHeight, CalculateFootprintRadius() * 1.2f, Vector3.up, Color.grey);
                }
            }


            // Tensor shit
            m_Rigidbody.automaticInertiaTensor = false;
            m_Rigidbody.inertiaTensor = Vector3.one * 0.3f;

            // PreMovement
            for (int i = 0; i < m_Movers.Count; i++)
                m_Movers[i].PreMovement(m_Sim, dt);

            m_Sim.UpVector = Vector3.up;

            // Contact Movement
            // movement that results from ground or surface contact
            m_Sim.ContactMove *= m_CachedMoveSpeedMult;
            if (m_Sim.ContactMove != Vector3.zero && !m_Sim.IsSliding)
            {
                if (m_Sim.IsStableGrounded)
                {
                    // Wall movement
                    if (m_Sim.IsSliding)
                    {
                        Debug.LogError("This should not be called! investigate");
                        // TODO only do this if we are moving TOWARDS the wall, not if we are moving away from it
                        Vector3 wallNormal = Vector3.ProjectOnPlane(m_Sim.GroundUpperHit.normal, Physics.gravity.normalized).normalized;
                        if (m_DebugGroundDetection)
                            Debug.DrawLine(m_Sim.GroundUpperHit.point + Vector3.one, m_Sim.GroundUpperHit.point + Vector3.one + wallNormal, Color.blue, dt, false);
                        if (Vector3.Dot(wallNormal, m_Sim.ContactMove) > 0)
                        {
                            m_Sim.ContactMove = Vector3.ProjectOnPlane(m_Sim.ContactMove, wallNormal).normalized * m_Sim.ContactMove.magnitude;
                        }
                    }
                    else
                    {
                        // Treat upperGroundHit as wall if it were unwalkable
                        if (!IsRaycastHitWalkable(m_Sim.PreciseGroundUpperHit))
                        {
                            Vector3 wallNormal = Vector3.ProjectOnPlane(m_Sim.PreciseGroundUpperHit.normal, Physics.gravity.normalized).normalized;
                            if (Vector3.Dot(wallNormal, m_Sim.ContactMove) < 0)
                            {
                                if (m_Sim.PreciseGroundUpperHit.collider.attachedRigidbody)
                                {
                                    // TODO push collider
                                    Vector3 pushForce = Vector3.Project(m_Sim.ContactMove, wallNormal);
                                    m_Sim.PreciseGroundUpperHit.collider.attachedRigidbody.AddForce(pushForce.normalized * m_PushForce, ForceMode.Force);
                                }

                                m_Sim.ContactMove = Vector3.ProjectOnPlane(m_Sim.ContactMove, wallNormal) + Vector3.Project(m_Sim.ContactMove, wallNormal) * 0.3f;
                            }
                        }

                        // When we walk perpendicular to a slope we do not expect to move down, we expect the height not to change
                        // So here we cancel out the gravity factor to reduce this effect
                        Vector3 groundNormal = m_Sim.PreciseGroundLowerHit.normal;
                        if (m_Sim.IsStepping)
                        {
                            groundNormal = m_Sim.GroundUpperHit.normal;
                        }
                        m_Sim.ContactMove += -Physics.gravity * dt * (1 + Vector3.Dot(m_Sim.ContactMove, -groundNormal)) * 0.5f;
                        m_Sim.ContactMove = Vector3.ProjectOnPlane(m_Sim.ContactMove, groundNormal);
                    }

                    m_Rigidbody.AddForceAtPosition(m_Sim.ContactMove * m_GroundMoveForce, FeetPos, ForceMode.Acceleration);
                }
                // Air movement
                else
                {
                    // Wall movement
                    if (m_Sim.IsSliding)
                    {
                        Debug.Log("Sliding down slope");
                        // TODO only do this if we are moving TOWARDS the wall, not if we are moving away from it
                        Vector3 wallNormal = Vector3.ProjectOnPlane(m_Sim.GroundUpperHit.normal, Physics.gravity.normalized).normalized;
                        if (m_DebugGroundDetection)
                            Debug.DrawLine(m_Sim.GroundUpperHit.point + Vector3.one, m_Sim.GroundUpperHit.point + Vector3.one + wallNormal, Color.blue, dt, false);
                        if (Vector3.Dot(wallNormal, m_Sim.ContactMove) < 0)
                        {
                            m_Sim.ContactMove = Vector3.ProjectOnPlane(m_Sim.ContactMove, wallNormal).normalized * m_Sim.ContactMove.magnitude;
                        }
                    }

                    float airSpeedMult = MMath.InverseLerpClamped(0.707f, 0f, Vector3.Dot(m_Rigidbody.linearVelocity.normalized, m_Sim.ContactMove.normalized));
                    m_Rigidbody.AddForce(m_Sim.ContactMove * m_AirMoveForce * airSpeedMult, ForceMode.Acceleration);
                }

                // Push at feet
                for (int i = 0; i < m_Sim.GroundColliderCount; i++)
                {
                    if (m_Sim.GroundColliders[i].attachedRigidbody)
                    {
                        m_Sim.GroundColliders[i].attachedRigidbody.AddForceAtPosition(-m_Sim.ScheduledMove * m_PushForce, FeetPos, ForceMode.Force);
                    }
                }
            }


            // TODO adding 90 deg to the character rotation works out, it might be a hack tho and is not tested in every scenario, could break
            float targetRotationRate = 20;
            targetRotationRate *= MMath.RemapClamped(0, 0.08f, 1, 0.1f, m_Sim.AirborneTimer);
            m_Sim.TargetLookDir = Vector3.Slerp(m_Sim.TargetLookDir, Vector3.Slerp(m_Sim.TargetLookDir, Quaternion.Euler(1, 1, 1) * m_Sim.ScheduledLookDir, targetRotationRate * dt), m_Sim.ScheduledMove.magnitude.Clamp01());

            if (m_Sim.IsSliding)
            {
                m_Sim.TargetLookDir = m_Sim.GroundUpperHit.normal;
            }
            Vector3 targetForward = Vector3.Cross(Vector3.Cross(m_Sim.UpVector, m_Sim.TargetLookDir), m_Sim.UpVector);
            Quaternion lookRotation = Quaternion.FromToRotation(m_Rigidbody.rotation * Vector3.forward, targetForward);
            Vector3 targetRotationTorque = lookRotation.eulerAngles;
            targetRotationTorque = new Vector3(MMath.RepeatInInterval(targetRotationTorque.x, -180, 180), MMath.RepeatInInterval(targetRotationTorque.y, -180, 180), MMath.RepeatInInterval(targetRotationTorque.z, -180, 180));
            targetRotationTorque = Vector3.Scale(targetRotationTorque, m_Sim.UpVector);
            targetRotationTorque = targetRotationTorque.Clamp(Vector3.one * -45, Vector3.one * 45) * (360 / 45);
            //targetRotationTorque *= MMath.RemapClamped(-0.5f, 0.25f, 2, 1, Vector3.Dot(m_Sim.TargetLookDir, Rigidbody.rotation * Vector3.forward));
            //float targetRotationTorque = MMath.DeltaAngle((m_Rigidbody.rotation.eulerAngles.y + 90) * MMath.Deg2Rad, MMath.Atan2(m_Sim.TargetLookDir.z, -m_Sim.TargetLookDir.x)) * MMath.Rad2Deg;
            if (!MMath.IsZero(Rigidbody.linearVelocity))
            {
                targetRotationTorque *= MMath.RemapClamped(-0.5f, 0, 0.1f, 1, Vector3.Dot(m_Sim.ContactMove.normalized, Rigidbody.linearVelocity.normalized));
            }
            if (m_Sim.IsStableGrounded)
            {
                targetRotationTorque *= m_GroundRotationForce;
            }
            else
            {
                targetRotationTorque *= m_AirRotationForce;
            }
            if (m_Sim.IsSliding)
            {
                targetRotationTorque *= 0;
            }
            targetRotationTorque *= m_CachedRotRateMult;
            //rotMult = MMath.RemapClamped(0.5f, 1, 1, 0, m_RotationRelaxation);
            //rotMult *= MMath.RemapClamped(180, 90, 0.1f, 1f, MMath.Abs(targetRotationTorque));
            //rotMult *= MMath.RemapClamped(1, 3, 0.01f, 1f, MMath.Abs(m_RigidBody.angularVelocity.y));
            //rotMult *= MMath.RemapClamped(2, 4, 0.05f, 1f, m_RigidBody.velocity.magnitude);
            //Debug.Log(rotMult);
            m_Rigidbody.AddRelativeTorque(0, targetRotationTorque.y, 0, ForceMode.Force);


            // TODO maybe try rotating by applying a torque impulse instead of a force
            // when target rotation and *last* target rotation differ, a force is applied
            // that could help with the rotation relaxment problem
            // maybe even lerp the last target roation with the current one and apply force based on the diff
            // so if you want to go in the new direction for longer, the vectors have lerpt on top of each other
            // and you should either have rotated to target rot or you are stuck and get rotated less.

            // TODO relax the rotation amount if we realize that we can not rotate under the current load we have
            //m_RotationRelaxation -= Vector3.Dot(m_TargetRotation, transform.forward) * dt;
            //m_RotationRelaxation = MMath.Clamp(m_RotationRelaxation, 0, 1);

            // Tumbler stand-up effect
            //if (false)
            {
                Vector3 targetLean = Vector3.zero;
                if (m_Sim.IsStableGrounded && m_Sim.ContactMove != Vector3.zero && m_Rigidbody.linearVelocity != Vector3.zero)
                {
                    // Lean in direction of movement
                    targetLean += m_Sim.ContactMove.normalized
                        // Lean more if linearVelocity has not caught up to contactMove
                        * MMath.RemapClamped(0.5f, 0.8f, 0.2f, 0, Vector3.Dot(m_Sim.ContactMove.ClampMagnitude(0, 1), (m_Rigidbody.linearVelocity / 4).ClampMagnitude(0, 1)))
                        // Lean more if forward axis does not match wanted contactMove (we are dragging something) 
                        * MMath.RemapClamped(-1, 1, 1, 0, Vector3.Dot(m_Rigidbody.rotation * Vector3.forward, m_Sim.ContactMove.normalized));

                    targetLean += m_Sim.ContactMove.normalized * 0.03f;
                }
                m_Sim.Lean = Vector3.Slerp(m_Sim.Lean, targetLean, 4 * dt);


                Vector3 gravityLeanVector = Vector3.Scale(m_Rigidbody.rotation * Vector3.up, new Vector3(-1, 1, -1));
                Quaternion upRotation = Quaternion.FromToRotation(m_Rigidbody.rotation * Vector3.up, (Vector3.up + gravityLeanVector + m_Sim.Lean).normalized);
                Vector3 rawTorque = Vector3.ClampMagnitude(new Vector3(upRotation.x, upRotation.y, upRotation.z) * upRotation.w, 0.2f) / 0.2f;
                float torqueMult = 1;
                torqueMult *= MMath.Lerp(0.0f, 1, MMath.Pow(MMath.InverseLerpClamped(0.4f, 0.1f, m_Sim.AirborneTimer), 4));
                torqueMult *= MMath.RemapClamped(0, 0.5f, 0, 1, m_Sim.UprightTimer);
                //torque *= MMath.Lerp(0.4f, 1, MMath.Pow(MMath.InverseLerpClamped(0.707f, 0.9f, Vector3.Dot(m_Sim.UpVector, m_Rigidbody.rotation * Vector3.up)), 2));
                torqueMult *= MMath.RemapClamped(0, 0.5f, 0.2f, 1, m_Sim.GroundTimer);
                if (!m_Sim.IsSliding && m_Sim.GroundColliderCount > 0)
                {
                    torqueMult = MMath.Max(0.1f, torqueMult);
                }
                if (m_Sim.IsSliding)
                {
                    torqueMult *= 0;
                }
                torqueMult *= m_StandUpMult;
                torqueMult *= m_StandUpForce;
                //Debug.Log("Stand up torque: " + torqueMult);
                m_Rigidbody.AddTorque(rawTorque * torqueMult, ForceMode.Acceleration);
            }

            // Face upwards when sliding
            if (m_Sim.IsSliding)
            {
                Vector3 slopeNormal = m_Sim.GroundUpperHit.normal;
                Vector3 slideUp = Vector3.Cross(Vector3.Cross(Vector3.up, slopeNormal), slopeNormal);
                slideUp = slideUp * MMath.Sign(Vector3.Dot(slideUp, Vector3.up));
            
                float mult = 10;
                mult *= MMath.InverseLerpClamped(0.95f, 0.707f, Vector3.Dot(m_Rigidbody.rotation * Vector3.up, Vector3.up));
                mult *= MMath.InverseLerpClamped(0.2f, 0.7f, m_Sim.AirborneTimer);
            
                // Adjust forward axis
                var rot = Quaternion.FromToRotation(m_Rigidbody.rotation * Vector3.up, slideUp);
                m_Rigidbody.AddTorque((new Vector3(rot.x, rot.y, rot.z) * mult).Clamp(Vector3.one * -200, Vector3.one * 200), ForceMode.Force);
            
                // Adjust up axis
                Quaternion upRotation = Quaternion.FromToRotation(m_Rigidbody.rotation * Vector3.forward, slopeNormal);
                m_Rigidbody.AddTorque(new Vector3(upRotation.x, upRotation.y, upRotation.z) * mult, ForceMode.Force);
            }


            // Feet drag
            if (!m_Sim.IsSliding)
            {
                /* TODO find better approach for this...
                 * When applying an upwards force to a character the feet dampening kicks in and limits the upwards motion
                 * However, when simply not dampening the y component of the velocity, walking up slopes causes issues
                 * In other words: Correctly walking up slopes is only possible because of the dampening applied here.
                 * That is weird and should not be the case, so walking up slopes needs a fix!
                */

                float lerpFactor = MMath.InverseLerpClamped(4f, 6f, MMath.Abs(m_Rigidbody.linearVelocity.y));
                float linearDrag = MMath.Lerp(m_FeetLinearResistance, m_FeetLinearAirResistance, m_Sim.IsStableGrounded ? 0 : 1);
                float feetLinearResistance = Mathf.Clamp01(1 - linearDrag * dt);
                m_Rigidbody.linearVelocity = Vector3.Scale(m_Rigidbody.linearVelocity, Vector3.Lerp(Vector3.one * feetLinearResistance, new Vector3(feetLinearResistance, 1, feetLinearResistance), lerpFactor));
                
                float angularDrag = MMath.Lerp(m_FeetAngularResistance, m_FeetAngularAirResistance, m_Sim.IsStableGrounded ? 0 : 1);
                m_Rigidbody.angularVelocity *= MMath.Clamp01(1 - angularDrag * dt);
                //Vector3 accVel = (m_RigidBody.GetAccumulatedForce() * Mathf.Clamp01(1 - FeetLinearResistance * dt)) - m_RigidBody.GetAccumulatedForce();
                //m_RigidBody.AddForceAtPosition(accVel, feetPos, ForceMode.Force);
                Vector3 accTorque = -m_Rigidbody.GetAccumulatedTorque() + (m_Rigidbody.GetAccumulatedTorque() * Mathf.Clamp01(1 - angularDrag * dt));
                m_Rigidbody.AddTorque(accTorque, ForceMode.Force);
            }

            float frictionTarget = MMath.LerpClamped(m_StationaryFriction, m_MovementFriction, m_Sim.ContactMove.magnitude); ;
            if (!m_Sim.IsStableGrounded || m_Sim.IsSliding)
                frictionTarget = 0;
            m_Sim.PhysicsMaterial.staticFriction = frictionTarget;
            m_Sim.PhysicsMaterial.dynamicFriction = frictionTarget;

            if (m_DebugLocomotion)
                Debug.DrawLine(transform.position, transform.position + m_Sim.ContactMove, Color.green, dt, false);
        }


        public bool IsRaycastHitWalkable(RaycastHit hit)
        {
            return IsSlopeWalkable(hit.normal) && IsObjectWalkable(hit.collider);
        }
        public bool IsSlopeWalkable(Vector3 normal)
        {
            return MMath.Acos(MMath.ClampNeg1to1(Vector3.Dot(normal, -Physics.gravity.normalized))) * MMath.Rad2Deg <= m_MaxSlopeAngle;
        }
        public bool IsObjectWalkable(Collider collider)
        {
            if (!m_NoStableGroundTag)
            {
                return true;
            }
            if (!collider)
            {
                return false;
            }

            // Test colliders walkable tag
            if (!m_CachedWalkableColliders.ContainsKey(collider))
            {
                GameplayTagOwner tagOwner = collider.GetComponentInParent<GameplayTagOwner>();
                bool walkable = true;
                if (tagOwner && tagOwner.HasTag(m_NoStableGroundTag))
                {
                    walkable = false;
                }
                m_CachedWalkableColliders.Add(collider, walkable);
            }

            return m_CachedWalkableColliders[collider];
        }
        public bool IsStandingUpRight()
        {
            return Vector3.Dot(Rigidbody.rotation * Vector3.up, m_Sim.UpVector) > 0.707f;
        }

        public Vector3 CalculateLocalUpVector()
        {
            if (m_Collider is CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = (CapsuleCollider)m_Collider;
                switch (capsuleCollider.direction)
                {
                    case 0: return Vector3.right;
                    case 1: return Vector3.up;
                    case 2: return Vector3.forward;
                }
            }
            else if (m_Collider is SphereCollider)
            {
                return Vector3.up;
            }
            else
                Debug.Assert(false, "Collider type is not supported!", gameObject);
            return Vector3.zero;
        }
        public float CalculateBodyHeight()
        {
            float height = 0;
            if (m_Collider is CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = (CapsuleCollider)m_Collider;
                height = MMath.Max(capsuleCollider.height, capsuleCollider.radius * 2) * (capsuleCollider.direction == 0 ? transform.localScale.x : (capsuleCollider.direction == 1 ? transform.localScale.y : transform.localScale.z));
            }
            else if (m_Collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)m_Collider;
                height = sphereCollider.radius * MMath.Max(transform.localScale) * 2;
            }
            else
                Debug.Assert(false, "Collider type is not supported!", gameObject);
            return height;
        }
        public float CalculateFootprintRadius()
        {
            float radius = 0;
            if (m_Collider is CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = (CapsuleCollider)m_Collider;
                float scaledRadius = capsuleCollider.radius * MMath.Max(Vector3.ProjectOnPlane(transform.localScale, capsuleCollider.direction == 0 ? Vector3.right : (capsuleCollider.direction == 1 ? Vector3.up : Vector3.forward)));
                float scaledHeight = MMath.Max(capsuleCollider.height, capsuleCollider.radius * 2) * (capsuleCollider.direction == 0 ? transform.localScale.x : (capsuleCollider.direction == 1 ? transform.localScale.y : transform.localScale.z));

                float capsuleHalfHeightWithoutHemisphereScaled = scaledHeight / 2 - scaledRadius;
                radius = scaledRadius;
            }
            else if (m_Collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)m_Collider;
                Ray ray = new Ray();
                ray.origin = transform.TransformPoint(sphereCollider.center);
                ray.direction = Vector3.down;
                float scaledRadius = sphereCollider.radius * MMath.Max(transform.localScale);
                radius = scaledRadius;
            }
            else
                Debug.Assert(false, "Collider type is not supported!", gameObject);
            return radius;
        }


        private bool DetectGround(out RaycastHit groundLowerHit, out RaycastHit preciseGroundLowerHit, out RaycastHit groundUpperHit, out RaycastHit preciseGroundUpperHit)
        {
            groundLowerHit = new RaycastHit();
            preciseGroundLowerHit = new RaycastHit();
            groundUpperHit = new RaycastHit();
            preciseGroundUpperHit = new RaycastHit();

            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);
            float distance = m_GroundDetectionDistance + SKIN_THICKNESS;

            Ray detectionRay = new Ray();
            float radius = 0;

            int hits = 0;
            if (m_Collider is CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = (CapsuleCollider)m_Collider;
                float scaledRadius = capsuleCollider.radius * MMath.Max(Vector3.ProjectOnPlane(transform.localScale, capsuleCollider.direction == 0 ? Vector3.right : (capsuleCollider.direction == 1 ? Vector3.up : Vector3.forward)));
                float scaledHeight = MMath.Max(capsuleCollider.height, capsuleCollider.radius * 2) * (capsuleCollider.direction == 0 ? transform.localScale.x : (capsuleCollider.direction == 1 ? transform.localScale.y : transform.localScale.z));

                float capsuleHalfHeightWithoutHemisphereScaled = scaledHeight / 2 - scaledRadius;
                detectionRay.origin = transform.TransformPoint(capsuleCollider.center) - transform.TransformDirection(Vector2.up) * capsuleHalfHeightWithoutHemisphereScaled;
                detectionRay.direction = Vector3.down;
                radius = scaledRadius - SKIN_THICKNESS;
                hits = Physics.SphereCastNonAlloc(detectionRay, radius, m_Sim.GroundHits, distance, layerMask, QueryTriggerInteraction.Ignore);
            }
            else if (m_Collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)m_Collider;
                detectionRay.origin = transform.TransformPoint(sphereCollider.center);
                detectionRay.direction = Vector3.down;
                float scaledRadius = sphereCollider.radius * MMath.Max(transform.localScale);
                radius = scaledRadius - SKIN_THICKNESS;
                hits = Physics.SphereCastNonAlloc(detectionRay, radius, m_Sim.GroundHits, distance, layerMask, QueryTriggerInteraction.Ignore);

            }
            else
                Debug.Assert(false, "Collider type is not supported!", gameObject);

            if (hits == 0)
                return false;
            
            groundLowerHit.point = new Vector3(0, float.PositiveInfinity, 0);
            groundUpperHit.point = new Vector3(0, float.NegativeInfinity, 0);
            m_Sim.GroundColliderCount = 0;
            for (int i = 0; i < hits; i++)
            {
                // Discard overlaps
                if (m_Sim.GroundHits[i].distance == 0)
                    continue;
                // Discard self collisions
                if (m_Sim.GroundHits[i].collider.transform == Rigidbody.transform)
                    continue;
                if (m_Sim.GroundHits[i].collider.transform.IsChildOf(Rigidbody.transform))
                    continue;

                m_Sim.GroundColliders[m_Sim.GroundColliderCount] = m_Sim.GroundHits[i].collider;
                m_Sim.GroundColliderCount++;

                if (m_Sim.GroundHits[i].point.y < groundLowerHit.point.y)
                {
                    groundLowerHit = m_Sim.GroundHits[i];
                }
                if (m_Sim.GroundHits[i].point.y > groundUpperHit.point.y)
                {
                    groundUpperHit = m_Sim.GroundHits[i];
                }
                // Choose better ground if two colliders are similar
                if (MMath.Approximately(m_Sim.GroundHits[i].point.y, groundUpperHit.point.y))
                {
                    groundUpperHit = m_Sim.GroundHits[i].normal.y > groundUpperHit.normal.y ? m_Sim.GroundHits[i] : groundUpperHit;
                }
                if (MMath.Approximately(m_Sim.GroundHits[i].point.y, groundLowerHit.point.y))
                {
                    groundLowerHit = m_Sim.GroundHits[i].normal.y > groundLowerHit.normal.y ? m_Sim.GroundHits[i] : groundLowerHit;
                }
            }
            // No valid ground found
            if (groundLowerHit.collider == null)
                return false;

            if (m_DebugGroundDetection)
            {
                DebugHelper.DrawWireCapsule(detectionRay.origin, detectionRay.GetPoint(distance), radius, Color.grey);
                DebugHelper.DrawWireCapsule(detectionRay.origin, detectionRay.GetPoint(groundLowerHit.distance), radius, Color.red);
            }

            // TODO these precise hit raycasts should propably be global and not only hit the ground collider
            if (groundLowerHit.collider)
            {
                Physics.Raycast(new Ray(groundLowerHit.point + Vector3.up * 0.001f + Vector3.Cross(Vector3.Cross(groundLowerHit.normal, Vector3.up), groundLowerHit.normal) * 0.001f, Vector3.down), out preciseGroundLowerHit, 0.002f, layerMask, QueryTriggerInteraction.Ignore);
                if (groundLowerHit.point == groundUpperHit.point && groundLowerHit.normal == groundUpperHit.normal && groundLowerHit.collider == groundUpperHit.collider)
                    preciseGroundUpperHit = preciseGroundLowerHit;
                else
                    Physics.Raycast(new Ray(groundUpperHit.point + Vector3.up * 0.001f + Vector3.Cross(Vector3.Cross(groundUpperHit.normal, Vector3.up), groundUpperHit.normal) * 0.001f, Vector3.down), out preciseGroundUpperHit, 0.002f, layerMask, QueryTriggerInteraction.Ignore);
            }

            return true;
        }


        private Vector3 GetInertiaTensor()
        {
            bool cachedAutoTensor = m_Rigidbody.automaticInertiaTensor;
            Vector3 cachedInertiaTensor = m_Rigidbody.inertiaTensor;
            Quaternion cachedInertiaTensorRotation = m_Rigidbody.inertiaTensorRotation;

            m_Rigidbody.automaticInertiaTensor = true;
            Vector3 autoTensor = m_Rigidbody.inertiaTensor;

            m_Rigidbody.automaticInertiaTensor = cachedAutoTensor;
            m_Rigidbody.inertiaTensor = cachedInertiaTensor;
            m_Rigidbody.inertiaTensorRotation = cachedInertiaTensorRotation;

            return autoTensor;
        }
        private Vector3 ApproximateInertiaTensor()
        {
            float ComputeSphereRatio(float radius)
            {
                return (4.0f / 3.0f) * MMath.PI * radius * radius * radius;
            };
            float ComputeCylinderRatio(float radius, float halfLength)
            {
                return MMath.PI * radius * radius * (2.0f * halfLength);
            };
            float ComputeCapsuleRatio(float radius, float cylinderHeight)
            {
                return ComputeSphereRatio(radius) + ComputeCylinderRatio(radius, cylinderHeight);
            };
            Vector3 ComputeCapsuleInertiaTensor(float radius, float cylinderHeight, Vector3 upVector)
            {
                float mass = ComputeCapsuleRatio(radius, cylinderHeight);

                float t = MMath.PI * radius * radius;
                float i1 = t * ((radius * radius * radius * 8.0f / 15.0f) + (cylinderHeight * radius * radius));
                float i2 = t * ((radius * radius * radius * 8.0f / 15.0f) + (cylinderHeight * radius * radius * 3.0f / 2.0f) + (cylinderHeight * cylinderHeight * radius * 4.0f / 3.0f) + (cylinderHeight * cylinderHeight * cylinderHeight * 2.0f / 3.0f));

                return MMath.Lerp(Vector3.one * i2 / mass, Vector3.one * i1 / mass, upVector);
            }

            float height = CalculateBodyHeight();
            float radius = CalculateFootprintRadius();
            Vector3 up = CalculateLocalUpVector();

            return ComputeCapsuleInertiaTensor(radius, height / 2 - radius, up) * m_Rigidbody.mass;
        }
    }


    public interface ICharacterMover
    {
        public void UpdateTimers(CharacterMovement.MovementSimulationState sim, float dt) { }
        public void PreMovement(CharacterMovement.MovementSimulationState sim, float dt) { }
        public void ModifyState(CharacterMovement.MovementSimulationState sim, float dt) { }
    }
}
