using Manatea.GameplaySystem;
using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        [FormerlySerializedAs("StationaryFriction")] 
        [SerializeField]
        private float m_StationaryFriction = 1;
        [FormerlySerializedAs("MovementFriction")] 
        [SerializeField]
        private float m_MovementFriction = 0.1f;
        [FormerlySerializedAs("GroundRotationRate")] 
        [SerializeField]
        private float m_GroundRotationRate = 10;
        [FormerlySerializedAs("AirRotationRate")] 
        [SerializeField]
        private float m_AirRotationRate = 0.05f;
        [FormerlySerializedAs("GroundRotationForce")] 
        [SerializeField]
        private float m_GroundRotationForce = 1;
        [FormerlySerializedAs("AirRotationForce")] 
        [SerializeField]
        private float m_AirRotationForce = 1;
        [FormerlySerializedAs("PushForce")] 
        [SerializeField]
        private float m_PushForce = 1;

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
        [FormerlySerializedAs("m_NoStableGroundTag")]
        [SerializeField]
        private GameplayTag m_NoStableGroundTag;

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

        // Constants
        /// <summary>
        /// The gap to use when testing as the characters collision
        /// </summary>
        public const float SKIN_THICKNESS = 0.001f;

        private float m_CachedMoveSpeedMult = 1;
        private float m_CachedRotRateMult = 1;
        private Dictionary<Collider, bool> m_CachedWalkableColliders = new Dictionary<Collider, bool>();

        /// <summary>
        /// The caracter feet position in world space
        /// </summary>
        public Vector3 FeetPos => m_Collider.ClosestPoint(transform.position + Physics.gravity * 10000);


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

            public Vector3 ContactMove;

            public float SmoothMoveMagnitude;
            public PhysicMaterial PhysicsMaterial;

            public RaycastHit GroundLowerHit;
            public RaycastHit PreciseGroundLowerHit;
            public RaycastHit GroundUpperHit;
            public RaycastHit PreciseGroundUpperHit;

            public int GroundColliderCount = 0;
            public Collider[] GroundColliders = new Collider[32];

            public RaycastHit[] GroundHits = new RaycastHit[32];


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

            m_Sim.PhysicsMaterial = new PhysicMaterial();
            m_Sim.PhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            m_Collider.material = m_Sim.PhysicsMaterial;
        }

        private void OnEnable()
        {
            m_Sim.ScheduledLookDir = transform.forward;
        }

        public void RegisterMover(ICharacterMover mover)
        {
            m_Movers.Add(mover);
        }
        public void UnregisterMover(ICharacterMover mover)
        {
            m_Movers.Remove(mover);
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
            }
        }

        private void FixedUpdate()
        {
            Debug.Assert(m_Collider, "No collider setup!", gameObject);
            Debug.Assert(m_Rigidbody, "No rigidbody attached!", gameObject);

            PrePhysics();
            UpdatePhysics(Time.fixedDeltaTime);
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
            }

            // Ground detection
            bool groundDetected = DetectGround(out m_Sim.GroundLowerHit, out m_Sim.PreciseGroundLowerHit, out m_Sim.GroundUpperHit, out m_Sim.PreciseGroundUpperHit);
            m_Sim.IsStableGrounded = groundDetected;
            m_Sim.IsSliding = m_Sim.IsStableGrounded && !IsRaycastHitWalkable(m_Sim.PreciseGroundLowerHit);     // TODO  && !m_VaultingActive
            m_Sim.IsStableGrounded &= !m_Sim.IsSliding;
            // Stepping
            if (IsObjectWalkable(m_Sim.PreciseGroundUpperHit.collider) && groundDetected && Vector3.Dot(m_Sim.PreciseGroundUpperHit.normal, m_Sim.GroundLowerHit.normal) < 0.9f && Vector3.Project(m_Sim.PreciseGroundUpperHit.point - FeetPos, transform.up).magnitude < m_StepHeight)     // TODO  && !m_VaultingActive
            {
                m_Sim.IsStepping = true;
                m_Sim.IsStableGrounded = true;
                m_Sim.IsSliding = false;
            }
            // ModifyState
            for (int i = 0; i < m_Movers.Count; i++)
                m_Movers[i].ModifyState(m_Sim);

            // Update timers
            if (m_Sim.IsStableGrounded)
            {
                m_Sim.GroundTimer += dt;
                m_Sim.AirborneTimer = 0;
            }
            else
            {
                m_Sim.GroundTimer = 0;
                m_Sim.AirborneTimer += dt;
            }
            m_Sim.SmoothMoveMagnitude = MMath.Damp(m_Sim.SmoothMoveMagnitude, m_Sim.ScheduledMove.magnitude, 1, Time.fixedDeltaTime * 2);
            // UpdateTimers
            for (int i = 0; i < m_Movers.Count; i++)
                m_Movers[i].UpdateTimers(m_Sim);

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


            // PreMovement
            for (int i = 0; i < m_Movers.Count; i++)
                m_Movers[i].PreMovement(m_Sim);


            // Contact Movement
            // movement that results from ground or surface contact
            m_Sim.ContactMove *= m_CachedMoveSpeedMult;
            if (m_Sim.ContactMove != Vector3.zero)
            {
                if (m_Sim.IsStableGrounded)
                {
                    // Wall movement
                    if (m_Sim.IsSliding)
                    {
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
                    float airSpeedMult = MMath.InverseLerpClamped(0.707f, 0f, Vector3.Dot(m_Rigidbody.velocity.normalized, m_Sim.ContactMove.normalized));
                    m_Rigidbody.AddForceAtPosition(m_Sim.ContactMove * m_AirMoveForce * airSpeedMult, FeetPos, ForceMode.Acceleration);
                }

                // Push at feet
                for (int i = 0; i < m_Sim.GroundColliderCount; i++)
                {
                    if (m_Sim.GroundColliders[i].attachedRigidbody)
                    {
                        m_Sim.GroundColliders[i].attachedRigidbody.AddForceAtPosition(-m_Sim.ScheduledMove * m_PushForce, FeetPos, ForceMode.Force);
                    }
                }

                // TODO adding 90 deg to the character rotation works out, it might be a hack tho and is not tested in every scenario, could break
                float targetRotationRate = 1;
                if (m_Sim.IsStableGrounded && !m_Sim.IsSliding)
                {
                    targetRotationRate *= m_GroundRotationRate;
                }
                else
                {
                    targetRotationRate *= m_AirRotationRate;
                }
                m_Sim.TargetLookDir = Vector3.RotateTowards(m_Sim.TargetLookDir, m_Sim.ScheduledLookDir, targetRotationRate * MMath.Deg2Rad * Time.fixedDeltaTime, 1);
                float targetRotationTorque = MMath.DeltaAngle((m_Rigidbody.rotation.eulerAngles.y + 90) * MMath.Deg2Rad, MMath.Atan2(m_Sim.TargetLookDir.z, -m_Sim.TargetLookDir.x)) * MMath.Rad2Deg;
                if (!MMath.IsZero(Rigidbody.velocity))
                {
                    targetRotationTorque *= MMath.RemapClamped(-0.5f, 0, 0.1f, 1, Vector3.Dot(m_Sim.ContactMove.normalized, Rigidbody.velocity.normalized));
                }
                targetRotationTorque *= m_CachedRotRateMult;
                if (m_Sim.IsStableGrounded && !m_Sim.IsSliding)
                {
                    targetRotationTorque *= m_GroundRotationForce;
                }
                else
                {
                    targetRotationTorque *= m_AirRotationForce;
                }
                //rotMult = MMath.RemapClamped(0.5f, 1, 1, 0, m_RotationRelaxation);
                //rotMult *= MMath.RemapClamped(180, 90, 0.1f, 1f, MMath.Abs(targetRotationTorque));
                //rotMult *= MMath.RemapClamped(1, 3, 0.01f, 1f, MMath.Abs(m_RigidBody.angularVelocity.y));
                //rotMult *= MMath.RemapClamped(2, 4, 0.05f, 1f, m_RigidBody.velocity.magnitude);
                //Debug.Log(rotMult);
                m_Rigidbody.AddTorque(0, targetRotationTorque, 0, ForceMode.Force);
            }


            // TODO maybe try rotating by applying a torque impulse instead of a force
            // when target rotation and *last* target rotation differ, a force is applied
            // that could help with the rotation relaxment problem
            // maybe even lerp the last target roation with the current one and apply force based on the diff
            // so if you want to go in the new direction for longer, the vectors have lerpt on top of each other
            // and you should either have rotated to target rot or you are stuck and get rotated less.

            // TODO relax the rotation amount if we realize that we can not rotate under the current load we have
            //m_RotationRelaxation -= Vector3.Dot(m_TargetRotation, transform.forward) * dt;
            //m_RotationRelaxation = MMath.Clamp(m_RotationRelaxation, 0, 1);

            // Feet drag
            if (m_Sim.IsStableGrounded && !m_Sim.IsSliding)
            {
                /* TODO find better approach for this...
                 * When applying an upwards force to a character the feet dampening kicks in and limits the upwards motion
                 * However, when simply not dampening the y component of the velocity, walking up slopes causes issues
                 * In other words: Correctly walking up slopes is only possible because of the dampening applied here.
                 * That is weird and should not be the case, so walking up slopes needs a fix!
                */
                float lerpFactor = MMath.InverseLerpClamped(4f, 6f, MMath.Abs(m_Rigidbody.velocity.y));

                float feetLinearResistance = Mathf.Clamp01(1 - m_FeetLinearResistance * dt);
                m_Rigidbody.velocity = Vector3.Scale(m_Rigidbody.velocity, Vector3.Lerp(Vector3.one * feetLinearResistance, new Vector3(feetLinearResistance, 1, feetLinearResistance), lerpFactor));

                float rotationDragMult = MMath.Clamp01(m_CachedRotRateMult);
                m_Rigidbody.angularVelocity *= MMath.Clamp01(1 - m_FeetAngularResistance * rotationDragMult * dt);
                //Vector3 accVel = (m_RigidBody.GetAccumulatedForce() * Mathf.Clamp01(1 - FeetLinearResistance * dt)) - m_RigidBody.GetAccumulatedForce();
                //m_RigidBody.AddForceAtPosition(accVel, feetPos, ForceMode.Force);
                //Vector3 accTorque = m_RigidBody.GetAccumulatedTorque() - (m_RigidBody.GetAccumulatedTorque() * Mathf.Clamp01(1 - FeetAngularResistance * dt));
                //m_RigidBody.AddTorque(accTorque, ForceMode.Force);
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
                if (tagOwner && tagOwner.Tags.Contains(m_NoStableGroundTag))
                {
                    walkable = false;
                }
                m_CachedWalkableColliders.Add(collider, walkable);
            }

            return m_CachedWalkableColliders[collider];
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
                //if (m_Sim.GroundHits[i].rigidbody && (!m_Sim.GroundHits[i].rigidbody.isKinematic || m_Sim.GroundHits[i].rigidbody.mass < 1))    // TODO dont hardcode mass here
                //    continue;
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
    }


    public interface ICharacterMover
    {
        public void UpdateTimers(CharacterMovement.MovementSimulationState sim) { }
        public void PreMovement(CharacterMovement.MovementSimulationState sim) { }
        public void ModifyState(CharacterMovement.MovementSimulationState sim) { }
    }
}
