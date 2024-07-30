using Manatea.GameplaySystem;
using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.AdventureRoots
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

        [Header("Jump")]
        [FormerlySerializedAs("JumpForce")] 
        [SerializeField]
        private float m_JumpForce = 5;
        [FormerlySerializedAs("JumpMoveAlignment")] 
        [SerializeField]
        private float m_JumpMoveAlignment = 0.75f;

        [Header("Vaulting")]
        [FormerlySerializedAs("EnableVaulting")] 
        [SerializeField]
        private bool m_EnableVaulting = false;
        [FormerlySerializedAs("VaultingDetectionDistance")] 
        [SerializeField]
        private float m_VaultingDetectionDistance;
        [FormerlySerializedAs("VaultingMaxHeight")]
        [SerializeField]
        private float m_VaultingMaxHeight;
        [FormerlySerializedAs("VaultingMaxTime")]
        [SerializeField]
        private float m_VaultingMaxTime;
        [FormerlySerializedAs("VaultingForce")] 
        [SerializeField]
        private float m_VaultingForce;

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
        [FormerlySerializedAs("LogLedgeDetection")]
        [SerializeField]
        private bool m_LogLedgeDetection = false;
        [FormerlySerializedAs("DebugVaulting")]
        [SerializeField]
        private bool m_DebugVaulting = false;

        // Public
        public Rigidbody Rigidbody => m_RigidBody;
        public GameplayAttributeOwner AttributeOwner => m_AttributeOwner;
        public GameplayTagOwner TagOwner => m_TagOwner;
        public Vector3 ScheduledMove => Sim.m_ScheduledMove;
        public Vector3 MoveSpeedMult => Sim.m_ScheduledMove;
        public RaycastHit GroundLowerHit => m_SimulationState.m_GroundLowerHit;
        public RaycastHit PreciseGroundLowerHit => m_SimulationState.m_PreciseGroundLowerHit;
        public RaycastHit GroundUpperHit => m_SimulationState.m_GroundUpperHit;
        public RaycastHit PreciseGroundUpperHit => m_SimulationState.m_PreciseGroundUpperHit;


        private Rigidbody m_RigidBody;
        private GameplayAttributeOwner m_AttributeOwner;
        private GameplayTagOwner m_TagOwner;

        // Simulation
        private MovementSimulationState m_SimulationState;
        private MovementSimulationState Sim => m_SimulationState;

        private List<ICharacterMover> m_Movers = new List<ICharacterMover>();

        // Constants
        /// <summary>
        /// The gap to use when testing as the characters collision
        /// </summary>
        private const float SKIN_THICKNESS = 0.001f;
        /// <summary>
        /// The minimum time after a jump we are guaranteed to be airborne
        /// </summary>
        private const float MIN_JUMP_TIME = 0.2f;

        // Simulation

        private bool m_VaultingActive;
        private float m_VaultingTimer;

        float m_CachedMoveSpeedMult = 1;
        float m_CachedRotRateMult = 1;
        private Dictionary<Collider, bool> m_CachedWalkableColliders = new Dictionary<Collider, bool>();

        /// <summary>
        /// The caracter feet position in world space
        /// </summary>
        public Vector3 FeetPos => m_Collider.ClosestPoint(transform.position + Physics.gravity * 10000);


        private struct LedgeSample
        {
            public bool IsLedge;
            public Vector3 Direction;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public RaycastHit Hit;
        }
        public enum LedgeType
        {
            Pole,
            Cliff,
            BalancingBeam,
            UnevenTerrain,
        }
        public class MovementSimulationState
        {
            public readonly CharacterMovement Movement;

            // Input
            public Vector3 m_ScheduledMove;
            public Vector3 m_ScheduledLookDir;
            public bool m_ScheduledJump;
            public Vector3 m_TargetLookDir;

            public bool m_IsStableGrounded;
            public bool m_IsSliding;
            public bool m_HasJumped;
            public bool m_IsStepping;

            public float m_ForceAirborneTimer;
            public float m_AirborneTimer;
            public float m_JumpTimer;
            public float m_GroundTimer;

            public Vector3 m_ContactMove;

            public float m_SmoothMoveMagnitude;
            public PhysicMaterial m_PhysicsMaterial;

            public RaycastHit m_GroundLowerHit;
            public RaycastHit m_PreciseGroundLowerHit;
            public RaycastHit m_GroundUpperHit;
            public RaycastHit m_PreciseGroundUpperHit;

            public MovementSimulationState(CharacterMovement movement)
            {
                Movement = movement;
            }
        }


        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_AttributeOwner = GetComponent<GameplayAttributeOwner>();
            m_TagOwner = GetComponent<GameplayTagOwner>();

            m_SimulationState = new MovementSimulationState(this);

            m_SimulationState.m_PhysicsMaterial = new PhysicMaterial();
            m_SimulationState.m_PhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            m_Collider.material = m_SimulationState.m_PhysicsMaterial;
        }

        private void OnEnable()
        {
            Sim.m_ScheduledLookDir = transform.forward;
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
            Sim.m_ScheduledMove = moveVector;
            if (rotateTowardsMove && Sim.m_ScheduledMove != Vector3.zero)
            {
                Sim.m_ScheduledLookDir = Sim.m_ScheduledMove.normalized;
            }
        }
        public void SetTargetRotation(Vector3 targetRotation)
        {
            if (targetRotation != Vector3.zero)
            {
                Sim.m_ScheduledLookDir = targetRotation.FlattenY().normalized;
            }
        }
        public void Jump()
        {
            Sim.m_ScheduledJump = true;
        }
        public void ReleaseJump()
        {
            Sim.m_ScheduledJump = false;
        }

        private void FixedUpdate()
        {
            Debug.Assert(m_Collider, "No collider setup!", gameObject);
            Debug.Assert(m_RigidBody, "No rigidbody attached!", gameObject);

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

            // Update timers
            Sim.m_ForceAirborneTimer = MMath.Max(Sim.m_ForceAirborneTimer - dt, 0);
            if (Sim.m_IsStableGrounded)
            {
                Sim.m_GroundTimer += dt;
                Sim.m_AirborneTimer = 0;
            }
            else
            {
                Sim.m_GroundTimer = 0;
                Sim.m_AirborneTimer += dt;
            }
            if (Sim.m_HasJumped)
            {
                Sim.m_JumpTimer += dt;
            }
            Sim.m_SmoothMoveMagnitude = MMath.Damp(Sim.m_SmoothMoveMagnitude, Sim.m_ScheduledMove.magnitude, 1, Time.fixedDeltaTime * 2);

            // Ground detection
            bool groundDetected = DetectGround(out Sim.m_GroundLowerHit, out Sim.m_PreciseGroundLowerHit, out Sim.m_GroundUpperHit, out Sim.m_PreciseGroundUpperHit);
            Sim.m_IsStableGrounded = groundDetected;
            Sim.m_IsStableGrounded &= Sim.m_ForceAirborneTimer <= 0;
            Sim.m_IsSliding = Sim.m_IsStableGrounded && !IsRaycastHitWalkable(Sim.m_PreciseGroundLowerHit) && !m_VaultingActive;
            Sim.m_IsStableGrounded &= !Sim.m_IsSliding;
            // Stepping
            if (IsObjectWalkable(Sim.m_PreciseGroundUpperHit.collider) && groundDetected && !m_VaultingActive && Vector3.Dot(Sim.m_PreciseGroundUpperHit.normal, Sim.m_GroundLowerHit.normal) < 0.9f && Vector3.Project(Sim.m_PreciseGroundUpperHit.point - FeetPos, transform.up).magnitude < m_StepHeight)
            {
                Sim.m_IsStepping = true;
                Sim.m_IsStableGrounded = true;
                Sim.m_IsSliding = false;
            }
            // Guarantee airborne when jumping just occured
            if (Sim.m_HasJumped && Sim.m_JumpTimer <= MIN_JUMP_TIME)
            {
                Sim.m_IsStableGrounded = false;
                Sim.m_IsSliding = false;
            }
            if (Sim.m_IsStableGrounded)
            {
                Sim.m_HasJumped = false;
                Sim.m_JumpTimer = 0;
            }


            Sim.m_ContactMove = Sim.m_ScheduledMove;

            // The direction we want to move in
            if (m_DebugLocomotion)
            {
                Debug.DrawLine(transform.position, transform.position + Sim.m_ScheduledMove, Color.blue, dt, false);
                if (groundDetected)
                    Debug.DrawLine(Sim.m_PreciseGroundLowerHit.point, Sim.m_PreciseGroundLowerHit.point + Sim.m_PreciseGroundLowerHit.normal, Color.black, dt, false);

                if (m_StepHeight > 0)
                {
                    DebugHelper.DrawWireCircle(FeetPos + Vector3.up * m_StepHeight, CalculateFootprintRadius() * 1.2f, Vector3.up, Color.grey);
                }
            }


            // Jump
            if (Sim.m_ScheduledJump && !Sim.m_HasJumped && Sim.m_AirborneTimer < 0.1f)
            {
                Sim.m_ScheduledJump = false;
                Sim.m_ForceAirborneTimer = 0.05f;

                if (Sim.m_ContactMove != Vector3.zero)
                {
                    Vector3 initialDir = m_RigidBody.velocity;
                    Vector3 targetDir = Sim.m_ContactMove.FlattenY().WithMagnitude(initialDir.FlattenY().magnitude) + Vector3.up * initialDir.y;
                    m_RigidBody.velocity = Vector3.Slerp(initialDir, targetDir, Sim.m_ContactMove.magnitude * m_JumpMoveAlignment);
                }

                Vector3 jumpDir = -Physics.gravity.normalized;
                // TODO add a sliding jump here that is perpendicular to the slide normal
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, jumpDir);
                Vector3 jumpForce = jumpDir * m_JumpForce;
                StartCoroutine(CO_Jump(jumpForce, 3));

                Sim.m_HasJumped = true;
            }



            #region Vaulting

            if (m_EnableVaulting)
            {
                bool vaultingValid = DetectVaulting(out RaycastHit vaultingHit);
                if (vaultingValid)
                {
                    Vector3 vaultingDir = (vaultingHit.point - FeetPos).FlattenY().normalized;
                    if (!m_VaultingActive)
                    {
                        if (Vector3.Dot(Sim.m_ScheduledMove.normalized, vaultingDir) > 0.4 && Rigidbody.velocity.FlattenY().magnitude < 1 && Sim.m_IsStableGrounded)
                        {
                            m_VaultingTimer += Time.fixedDeltaTime;
                        }
                        else
                        {
                            m_VaultingTimer = 0;
                        }
                        if (m_VaultingTimer > 0.1)
                        {
                            m_VaultingActive = true;
                            m_VaultingTimer = 0;
                        }
                    }

                    if (m_VaultingActive)
                    {
                        m_VaultingTimer += Time.fixedDeltaTime;
                        if (Rigidbody.velocity.y < 0.5f)
                        {
                            Vector3 vaultingForce = Vector3.up;
                            vaultingForce *= m_VaultingForce;
                            Rigidbody.AddForce(vaultingForce, ForceMode.VelocityChange);
                        }
                        Sim.m_ContactMove = vaultingDir;

                        if (MMath.Abs(vaultingHit.point.y - FeetPos.y) < CalculateFootprintRadius() * 0.005f
                            || m_VaultingTimer > m_VaultingMaxTime
                            || (m_VaultingTimer > 0.2 && Sim.m_IsStableGrounded))
                        {
                            m_VaultingActive = false;
                        }
                    }
                }
            }
            else
            {
                m_VaultingActive = false;
            }

            #endregion

            for (int i = 0; i < m_Movers.Count; i++)
            {
                m_Movers[i].PreMovement(Sim);
            }


            // Contact Movement
            // movement that results from ground or surface contact
            Sim.m_ContactMove *= m_CachedMoveSpeedMult;
            if (Sim.m_ContactMove != Vector3.zero)
            {
                if (Sim.m_IsStableGrounded)
                {
                    // Wall movement
                    if (Sim.m_IsSliding)
                    {
                        // TODO only do this if we are moving TOWARDS the wall, not if we are moving away from it
                        Vector3 wallNormal = Vector3.ProjectOnPlane(Sim.m_GroundUpperHit.normal, Physics.gravity.normalized).normalized;
                        if (m_DebugGroundDetection)
                            Debug.DrawLine(Sim.m_GroundUpperHit.point + Vector3.one, Sim.m_GroundUpperHit.point + Vector3.one + wallNormal, Color.blue, dt, false);
                        if (Vector3.Dot(wallNormal, Sim.m_ContactMove) > 0)
                        {
                            Sim.m_ContactMove = Vector3.ProjectOnPlane(Sim.m_ContactMove, wallNormal).normalized * Sim.m_ContactMove.magnitude;
                        }
                    }
                    else
                    {
                        // Treat upperGroundHit as wall if it were unwalkable
                        if (!IsRaycastHitWalkable(Sim.m_PreciseGroundUpperHit))
                        {
                            Vector3 wallNormal = Vector3.ProjectOnPlane(Sim.m_PreciseGroundUpperHit.normal, Physics.gravity.normalized).normalized;
                            if (Vector3.Dot(wallNormal, Sim.m_ContactMove) < 0)
                            {
                                if (Sim.m_PreciseGroundUpperHit.collider.attachedRigidbody)
                                {
                                    // TODO push collider
                                    Vector3 pushForce = Vector3.Project(Sim.m_ContactMove, wallNormal);
                                    Sim.m_PreciseGroundUpperHit.collider.attachedRigidbody.AddForce(pushForce.normalized * m_PushForce, ForceMode.Force);
                                }

                                Sim.m_ContactMove = Vector3.ProjectOnPlane(Sim.m_ContactMove, wallNormal) + Vector3.Project(Sim.m_ContactMove, wallNormal) * 0.3f;
                            }
                        }

                        // When we walk perpendicular to a slope we do not expect to move down, we expect the height not to change
                        // So here we cancel out the gravity factor to reduce this effect
                        Vector3 groundNormal = Sim.m_PreciseGroundLowerHit.normal;
                        if (Sim.m_IsStepping)
                        {
                            groundNormal = Sim.m_GroundUpperHit.normal;
                        }
                        Sim.m_ContactMove += -Physics.gravity * dt * (1 + Vector3.Dot(Sim.m_ContactMove, -groundNormal)) * 0.5f;
                        Sim.m_ContactMove = Vector3.ProjectOnPlane(Sim.m_ContactMove, groundNormal);
                    }

                    m_RigidBody.AddForceAtPosition(Sim.m_ContactMove * m_GroundMoveForce, FeetPos, ForceMode.Acceleration);
                }
                // Air movement
                else
                {
                    float airSpeedMult = MMath.InverseLerpClamped(0.707f, 0f, Vector3.Dot(m_RigidBody.velocity.normalized, Sim.m_ContactMove.normalized));
                    m_RigidBody.AddForceAtPosition(Sim.m_ContactMove * m_AirMoveForce * airSpeedMult, FeetPos, ForceMode.Acceleration);
                }

                // Push at feet
                for (int i = 0; i < m_GroundColliderCount; i++)
                {
                    if (m_GroundColliders[i].attachedRigidbody)
                    {
                        m_GroundColliders[i].attachedRigidbody.AddForceAtPosition(-Sim.m_ScheduledMove * m_PushForce, FeetPos, ForceMode.Force);
                    }
                }

                // TODO adding 90 deg to the character rotation works out, it might be a hack tho and is not tested in every scenario, could break
                float targetRotationRate = 1;
                if (Sim.m_IsStableGrounded && !Sim.m_IsSliding)
                {
                    targetRotationRate *= m_GroundRotationRate;
                }
                else
                {
                    targetRotationRate *= m_AirRotationRate;
                }
                Sim.m_TargetLookDir = Vector3.RotateTowards(Sim.m_TargetLookDir, Sim.m_ScheduledLookDir, targetRotationRate * MMath.Deg2Rad * Time.fixedDeltaTime, 1);
                float targetRotationTorque = MMath.DeltaAngle((m_RigidBody.rotation.eulerAngles.y + 90) * MMath.Deg2Rad, MMath.Atan2(Sim.m_TargetLookDir.z, -Sim.m_TargetLookDir.x)) * MMath.Rad2Deg;
                targetRotationTorque *= m_CachedRotRateMult;
                if (Sim.m_IsStableGrounded && !Sim.m_IsSliding)
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
                m_RigidBody.AddTorque(0, targetRotationTorque, 0, ForceMode.Force);
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
            if (Sim.m_IsStableGrounded && !Sim.m_IsSliding)
            {
                /* TODO find better approach for this...
                 * When applying an upwards force to a character the feet dampening kicks in and limits the upwards motion
                 * However, when simply not dampening the y component of the velocity, walking up slopes causes issues
                 * In other words: Correctly walking up slopes is only possible because of the dampening applied here.
                 * That is weird and should not be the case, so walking up slopes needs a fix!
                */
                float lerpFactor = MMath.InverseLerpClamped(4f, 6f, MMath.Abs(m_RigidBody.velocity.y));

                float feetLinearResistance = Mathf.Clamp01(1 - m_FeetLinearResistance * dt);
                m_RigidBody.velocity = Vector3.Scale(m_RigidBody.velocity, Vector3.Lerp(Vector3.one * feetLinearResistance, new Vector3(feetLinearResistance, 1, feetLinearResistance), lerpFactor));
                
                m_RigidBody.angularVelocity *= Mathf.Clamp01(1 - m_FeetAngularResistance * dt);

                //Vector3 accVel = (m_RigidBody.GetAccumulatedForce() * Mathf.Clamp01(1 - FeetLinearResistance * dt)) - m_RigidBody.GetAccumulatedForce();
                //m_RigidBody.AddForceAtPosition(accVel, feetPos, ForceMode.Force);
                //Vector3 accTorque = m_RigidBody.GetAccumulatedTorque() - (m_RigidBody.GetAccumulatedTorque() * Mathf.Clamp01(1 - FeetAngularResistance * dt));
                //m_RigidBody.AddTorque(accTorque, ForceMode.Force);
            }

            float frictionTarget = MMath.LerpClamped(m_StationaryFriction, m_MovementFriction, Sim.m_ContactMove.magnitude); ;
            if (!Sim.m_IsStableGrounded || Sim.m_IsSliding)
                frictionTarget = 0;
            Sim.m_PhysicsMaterial.staticFriction = frictionTarget;
            Sim.m_PhysicsMaterial.dynamicFriction = frictionTarget;

            if (m_DebugLocomotion)
                Debug.DrawLine(transform.position, transform.position + Sim.m_ContactMove, Color.green, dt, false);
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

        private float CalculateBodyHeight()
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
        private float CalculateFootprintRadius()
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


        private int m_GroundColliderCount = 0;
        private Collider[] m_GroundColliders = new Collider[32];

        private RaycastHit[] m_GroundHits = new RaycastHit[32];
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
                hits = Physics.SphereCastNonAlloc(detectionRay, radius, m_GroundHits, distance, layerMask, QueryTriggerInteraction.Ignore);
            }
            else if (m_Collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)m_Collider;
                detectionRay.origin = transform.TransformPoint(sphereCollider.center);
                detectionRay.direction = Vector3.down;
                float scaledRadius = sphereCollider.radius * MMath.Max(transform.localScale);
                radius = scaledRadius - SKIN_THICKNESS;
                hits = Physics.SphereCastNonAlloc(detectionRay, radius, m_GroundHits, distance, layerMask, QueryTriggerInteraction.Ignore);

            }
            else
                Debug.Assert(false, "Collider type is not supported!", gameObject);

            if (hits == 0)
                return false;
            
            groundLowerHit.point = new Vector3(0, float.PositiveInfinity, 0);
            groundUpperHit.point = new Vector3(0, float.NegativeInfinity, 0);
            m_GroundColliderCount = 0;
            for (int i = 0; i < hits; i++)
            {
                // Discard overlaps
                if (m_GroundHits[i].distance == 0)
                    continue;
                // Discard self collisions
                if (m_GroundHits[i].collider.transform == Rigidbody.transform)
                    continue;
                if (m_GroundHits[i].collider.transform.IsChildOf(Rigidbody.transform))
                    continue;

                m_GroundColliders[m_GroundColliderCount] = m_GroundHits[i].collider;
                m_GroundColliderCount++;

                if (m_GroundHits[i].point.y < groundLowerHit.point.y)
                {
                    groundLowerHit = m_GroundHits[i];
                }
                if (m_GroundHits[i].point.y > groundUpperHit.point.y)
                {
                    groundUpperHit = m_GroundHits[i];
                }
                // Choose better ground if two colliders are similar
                if (MMath.Approximately(m_GroundHits[i].point.y, groundUpperHit.point.y))
                {
                    groundUpperHit = m_GroundHits[i].normal.y > groundUpperHit.normal.y ? m_GroundHits[i] : groundUpperHit;
                }
                if (MMath.Approximately(m_GroundHits[i].point.y, groundLowerHit.point.y))
                {
                    groundLowerHit = m_GroundHits[i].normal.y > groundLowerHit.normal.y ? m_GroundHits[i] : groundLowerHit;
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

        private bool DetectVaulting(out RaycastHit vaultingHit)
        {
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            float radius = CalculateFootprintRadius() - SKIN_THICKNESS * 0.5f;
            float height = CalculateBodyHeight();
            // TODO the top should start further up to account for ledges with ramps on top. These dont get picked up right now as the raycast starts inside those ramps
            Vector3 top = FeetPos + Rigidbody.rotation * Vector3.forward * m_VaultingDetectionDistance + Vector3.up * (m_VaultingMaxHeight + radius);
            Vector3 bottom = FeetPos + Rigidbody.rotation * Vector3.forward * m_VaultingDetectionDistance + Vector3.up * radius;
            if (Vector3.Dot(top - bottom, Vector3.down) > 0)
            {
                vaultingHit = new RaycastHit();
                return false;
            }
            int hitCount = Physics.SphereCastNonAlloc(top, radius, Vector3.down, m_GroundHits, (top - bottom).magnitude, layerMask, QueryTriggerInteraction.Ignore);

            if (m_DebugVaulting)
            {
                DebugHelper.DrawWireCapsule(top, bottom, radius, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }

            List<RaycastHit> validHits = new List<RaycastHit>();
            for (int i = 0; i < hitCount; i++)
            {
                // Discard overlaps
                if (m_GroundHits[i].distance == 0)
                    continue;
                // Discard self collisions
                if (m_GroundHits[i].collider.transform == Rigidbody.transform)
                    continue;
                if (m_GroundHits[i].collider.transform.IsChildOf(Rigidbody.transform))
                    continue;
                if (m_GroundHits[i].normal.y <= 0)
                    continue;
                if (!IsRaycastHitWalkable(m_GroundHits[i]))
                    continue;

                validHits.Add(m_GroundHits[i]);
            }
            validHits.Sort((a, b) => b.distance.CompareTo(a.distance));

            for (int i = 0; i < validHits.Count; i++)
            {
                Vector3 targetA = top + Vector3.down * (validHits[i].distance - 0.05f);
                DebugHelper.DrawWireSphere(validHits[i].point, 0.05f, Color.red);
                DebugHelper.DrawWireSphere(top, 0.05f, Color.blue);
                DebugHelper.DrawWireSphere(targetA, 0.05f, Color.green);

                Collider[] overlaps = new Collider[8];
                hitCount = Physics.OverlapCapsuleNonAlloc(targetA, targetA + Vector3.up * (height - radius * 2), radius, overlaps, layerMask, QueryTriggerInteraction.Ignore);
                bool validVolume = true;
                for (int j = 0; j < hitCount; j++)
                {
                    // Discard self collisions
                    if (overlaps[j].transform == Rigidbody.transform)
                        continue;
                    if (overlaps[j].transform.IsChildOf(Rigidbody.transform))
                        continue;
                    validVolume = false;
                }
                if (validVolume)
                {
                    vaultingHit = validHits[i];
                    DebugHelper.DrawWireCapsule(targetA, targetA + Vector3.up * (height - radius * 2), radius, Color.green);
                    return true;
                }
            }

            vaultingHit = new RaycastHit();
            return false;
        }

        private IEnumerator CO_Jump(Vector3 velocity, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                m_RigidBody.AddForce(velocity / iterations, ForceMode.Impulse);

                for (int j = 0; j < m_GroundColliderCount; j++)
                {
                    if (m_GroundColliders[j] && m_GroundColliders[j].attachedRigidbody)
                    {
                        // TODO very extreme jumping push to objects. Can be tested by jumping off certain items
                        if (m_GroundColliders[j].attachedRigidbody && !m_GroundColliders[j].attachedRigidbody.isKinematic)
                        {
                            m_GroundColliders[j].attachedRigidbody.AddForceAtPosition(-velocity / iterations, FeetPos, ForceMode.VelocityChange);
                        }
                    }
                }

                yield return new WaitForFixedUpdate();
            }
        }

    }


    public interface ICharacterMover
    {
        public void PreMovement(CharacterMovement.MovementSimulationState sim) { }
    }
}
