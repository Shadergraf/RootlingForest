using Manatea.GameplaySystem;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.AdventureRoots
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : MonoBehaviour
    {
        public float GroundMoveForce = 1;
        public float AirMoveForce = 1;
        public float JumpForce = 5;
        public float StepHeight = 0.4f;
        public float FeetLinearResistance = 2;
        public float FeetAngularResistance = 2;
        public float StationaryFriction = 1;
        public float MovementFriction = 0.1f;
        public float GroundRotationRate = 10;
        public float AirRotationRate = 0.05f;

        [Header("Ground Magnetism")]
        public float GroundMagnetismRadius = 0.7f;
        public float GroundMagnetismDepth = 0.5f;
        public float GroundMagnetismForce = 50;

        [Header("Ledge Detection")]
        public float LedgeDetectionStart = 0.2f;
        public float LedgeDetectionEnd = -0.2f;
        public float LedgeDetectionDistance = 0.2f;
        public float LedgeDetectionRadius = 0.4f;

        public float LedgeBalancingForce = 50;
        public float LedgeMoveMultiplier = 0.75f;

        public float LedgeStableBalancingForce = 150;
        public float LedgeStableMoveMultiplier = 0.5f;

        public float LedgeBalancingWobbleTime = 0.8f;
        public float LedgeBalancingWobbleAmount = 0.45f;

        public float LedgeLandMagnetism = 1;

        [Header("Collision Detection")]
        public Collider Collider;
        public float SkinThickness = 0.05f;
        public float GroundDetectionDistance = 0.01f;
        public float MaxSlopeAngle = 46;

        [Header("Attributes & Tags")]
        public GameplayAttribute m_MoveSpeedAttribute;
        public GameplayAttribute m_RotationRateAttribute;
        public GameplayAttribute m_LedgeBalancingAttribute;
        public GameplayTag m_NoStableGroundTag;

        [Header("Debug")]
        public bool DebugCharacter = false;

        public Rigidbody Rigidbody => m_RigidBody;

        private Rigidbody m_RigidBody;
        private GameplayAttributeOwner m_AttributeOwner;
        private GameplayTagOwner m_TagOwner;

        // Input
        private Vector3 m_ScheduledMove;
        private Vector3 m_TargetLookDir;
        private bool m_ScheduledJump;

        // Constants
        private const float MIN_JUMP_TIME = 0.2f;       // The minimum time after a jump we are guaranteed to be airborne

        // Simulation
        private bool m_IsStableGrounded;
        private bool m_IsSliding;
        private float m_ForceAirborneTimer;
        private float m_AirborneTimer;
        private float m_JumpTimer;
        private float m_LedgeTimer;
        private bool m_HasJumped;
        private PhysicMaterial m_PhysicsMaterial;

        // Ledge Detection
        private const int c_LedgeDetectionSamples = 8;
        private const int c_LedgeDetectionIterations = 2;
        private const int c_TotalLedgeDetectionSamples = c_LedgeDetectionSamples * c_LedgeDetectionIterations;
        private LedgeSample[] m_LedgeSamples = new LedgeSample[c_LedgeDetectionSamples * c_LedgeDetectionIterations];
        private int m_LedgeDetectionFrame = -1;

        /// <summary>
        /// The caracter feet position in world space
        /// </summary>
        public Vector3 FeetPos => Collider.ClosestPoint(transform.position + Physics.gravity * 10000);


        private struct LedgeSample
        {
            public bool IsLedge;
            public Vector3 Direction;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public RaycastHit Hit;
        }


        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_AttributeOwner = GetComponent<GameplayAttributeOwner>();
            m_TagOwner = GetComponent<GameplayTagOwner>();

            m_PhysicsMaterial = new PhysicMaterial();
            m_PhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            Collider.material = m_PhysicsMaterial;
        }

        private void OnEnable()
        {
            m_TargetLookDir = transform.forward;

            Rigidbody.automaticInertiaTensor = false;
            Rigidbody.inertiaTensorRotation = Quaternion.Euler(0, 360, 0);
        }

        public void Move(Vector3 moveVector, bool rotateTowardsMove = true)
        {
            m_ScheduledMove = moveVector;
            if (rotateTowardsMove && m_ScheduledMove != Vector3.zero)
            {
                m_TargetLookDir = m_ScheduledMove.normalized;
            }
        }
        public void SetTargetRotation(Vector3 targetRotation)
        {
            if (targetRotation != Vector3.zero)
            {
                m_TargetLookDir = targetRotation.FlattenY().normalized;
            }
        }
        public void Jump()
        {
            m_ScheduledJump = true;
        }
        public void ReleaseJump()
        {
            m_ScheduledJump = false;
        }

        private void FixedUpdate()
        {
            Debug.Assert(Collider, "No collider setup!", gameObject);
            Debug.Assert(m_RigidBody, "No rigidbody attached!", gameObject);

            UpdatePhysics(Time.fixedDeltaTime);
        }

        protected void UpdatePhysics(float dt)
        {
            // Get Attributes
            float moveSpeedMult = 1;
            float rotRateMult = 1;
            if (m_AttributeOwner)
            {
                if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_MoveSpeedAttribute, out float speed))
                {
                    moveSpeedMult = speed;
                }
                if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_RotationRateAttribute, out float rot))
                {
                    rotRateMult = rot;
                }
            }

            // Update timers
            m_ForceAirborneTimer = MMath.Max(m_ForceAirborneTimer - dt, 0);

            if (!m_IsStableGrounded)
            {
                m_AirborneTimer += dt;
            }
            else
            {
                m_AirborneTimer = 0;
            }
            if (m_HasJumped)
            {
                m_JumpTimer += dt;
            }

            // Ground detection
            bool groundDetected = DetectGround(out RaycastHit groundHitResult, out RaycastHit preciseGroundHitResult);
            m_IsStableGrounded = groundDetected;
            m_IsStableGrounded &= m_ForceAirborneTimer <= 0;
            m_IsSliding = m_IsStableGrounded && MMath.Acos(Vector3.Dot(preciseGroundHitResult.normal, -Physics.gravity.normalized)) * MMath.Rad2Deg > MaxSlopeAngle;
            m_IsStableGrounded &= !m_IsSliding;
            // Step height
            if (m_IsSliding && Vector3.Dot(preciseGroundHitResult.normal, groundHitResult.normal) < 0.9f && Vector3.Project(preciseGroundHitResult.point - FeetPos, transform.up).magnitude < StepHeight)
            {
                m_IsStableGrounded = true;
                m_IsSliding = false;
            }
            // Guarantee airborne when jumping just occured
            if (m_HasJumped && m_JumpTimer <= MIN_JUMP_TIME)
            {
                m_IsStableGrounded = false;
                m_IsSliding = false;
            }
            if (m_IsStableGrounded)
            {
                m_HasJumped = false;
                m_JumpTimer = 0;
            }
            if (m_IsStableGrounded && m_NoStableGroundTag)
            {
                var tagOwner = groundHitResult.collider.GetComponentInParent<GameplayTagOwner>();
                if (tagOwner && tagOwner.Tags.Contains(m_NoStableGroundTag))
                {
                    m_IsStableGrounded = false;
                }
            }


            Vector3 contactMove = m_ScheduledMove;


            #region Ledge Detection

            bool ledgeFound = LedgeDetection();
            if (DebugCharacter)
            {
                for (int i = 0; i < c_TotalLedgeDetectionSamples; i++)
                {
                    var sample = m_LedgeSamples[i];
                    //DebugHelper.DrawWireSphere(sample.StartPosition, LedgeDetectionRadius, sample.IsLedge ? Color.red : Color.green, Time.fixedDeltaTime, false);
                    DebugHelper.DrawWireSphere(transform.position + sample.Direction, LedgeDetectionRadius, sample.IsLedge ? Color.red : Color.green, Time.fixedDeltaTime, false);
                }
            }

            // TODO better analysis of what kind of ledge we are currently dealing with
            // Single ledge, ledge on one distinct side of the samples (standing on on the edge of teh crater)
            // Dual ledge, ledge on two distinct regions (when balancing on a beam)
            // Multi ledge, multiple/random ledge regions (when balancing over a T-crossing)
            // Pole ledge, ledges all around the player (when balancing on a single pole)


            // TODO when detecting ground below the player (in a bigger radius) slightly push the player towards that direction
            //      to make difficult jumps a bit easier (like landing on a thin pole, or making a long jump)

            if (ledgeFound)
            {
                if (m_IsStableGrounded)
                {
                    m_LedgeTimer += dt;

                    Vector3 ledgeDir = groundHitResult.point - FeetPos;
                    Vector3 ledgeDirProjected = ledgeDir.FlattenY();

                    Vector3 ledgeForce = ledgeDirProjected;

                    // Balancing wiggle
                    if (m_ScheduledMove != Vector3.zero)
                    {
                        Vector3 imbalance = Vector3.Cross(m_ScheduledMove.normalized, Vector3.up);
                        imbalance *= Mathf.PerlinNoise1D(Time.time * m_ScheduledMove.magnitude * LedgeBalancingWobbleTime) * 2 - 1;
                        imbalance *= m_ScheduledMove.magnitude;
                        imbalance *= LedgeBalancingWobbleAmount;
                        contactMove += imbalance * m_ScheduledMove.magnitude;
                    }

                    // TODO remove this and put it in a dedicated ability that allows the player to balance better by using their hands
                    // Stabilize when holding mouse
                    if (Input.GetMouseButton(0) || UnityEngine.InputSystem.Gamepad.current.buttonWest.ReadValue() > 0)
                    {
                        ledgeForce *= LedgeStableBalancingForce;
                        contactMove *= LedgeStableMoveMultiplier;
                    }
                    else
                    {
                        ledgeForce *= LedgeBalancingForce;
                        contactMove *= LedgeMoveMultiplier;
                    }

                    // Balance attribute
                    if (m_AttributeOwner && m_AttributeOwner.TryGetAttributeEvaluatedValue(m_LedgeBalancingAttribute, out float att_balance))
                    {
                        ledgeForce *= att_balance;
                    }

                    // Stabilize player when not moving
                    if (m_ScheduledMove == Vector3.zero)
                    {
                        Vector3 stabilizationForce = Vector3.ProjectOnPlane(Vector3.up, groundHitResult.normal);
                        stabilizationForce = stabilizationForce.FlattenY().normalized + stabilizationForce.y * Vector3.up;
                        stabilizationForce *= 50;

                        ledgeForce += stabilizationForce * (1 - m_ScheduledMove.magnitude);
                    }

                    m_RigidBody.AddForceAtPosition(ledgeForce, FeetPos, ForceMode.Acceleration);
                }
            }
            else
            {
                m_LedgeTimer = 0;
            }

            #endregion


            #region Ground Magnetism

            bool groundMagnetFound = DetectGroundMagnetism(out RaycastHit groundMagnetHit);
            if (groundMagnetFound)
            {
                if (Rigidbody.velocity.y < 0 && !groundDetected)
                {
                    Vector3 groundMagnetForce = groundMagnetHit.point - FeetPos;
                    groundMagnetForce = groundMagnetForce.FlattenY();
                    groundMagnetForce *= GroundMagnetismForce;
                    Rigidbody.AddForce(groundMagnetForce, ForceMode.Acceleration);
                }
            }

            #endregion


            // The direction we want to move in
            if (DebugCharacter)
            {
                Debug.DrawLine(transform.position, transform.position + m_ScheduledMove, Color.blue, dt, false);
                if (groundDetected)
                    Debug.DrawLine(preciseGroundHitResult.point, preciseGroundHitResult.point + preciseGroundHitResult.normal, Color.black, dt, false);
            }


            // Contact Movement
            // movement that results from ground or surface contact
            contactMove *= moveSpeedMult;
            if (contactMove != Vector3.zero)
            {
                if (m_IsStableGrounded)
                {
                    // Wall movement
                    if (m_IsSliding)
                    {
                        // TODO only do this if we are moving TOWARDS the wall, not if we are moving away from it
                        Vector3 wallNormal = Vector3.ProjectOnPlane(preciseGroundHitResult.normal, Physics.gravity.normalized).normalized;
                        if (DebugCharacter)
                            Debug.DrawLine(preciseGroundHitResult.point + Vector3.one, preciseGroundHitResult.point + Vector3.one + wallNormal, Color.blue, dt, false);
                        if (Vector3.Dot(wallNormal, contactMove) < 0)
                        {
                            contactMove = Vector3.ProjectOnPlane(contactMove, wallNormal).normalized * contactMove.magnitude;
                        }
                    }
                    else
                    {
                        // When we walk perpendicular to a slope we do not expect to move down, we expect the height not to change
                        // So here we cancel out the gravity factor to reduce this effect
                        contactMove += -Physics.gravity * dt * (1 + Vector3.Dot(contactMove, -preciseGroundHitResult.normal)) * 0.5f;
                        contactMove = Vector3.ProjectOnPlane(contactMove, preciseGroundHitResult.normal);
                    }

                    m_RigidBody.AddForceAtPosition(contactMove * GroundMoveForce, FeetPos, ForceMode.Acceleration);
                }
                // Air movement
                else
                {
                    float airSpeedMult = MMath.InverseLerpClamped(0.707f, 0f, Vector3.Dot(m_RigidBody.velocity.normalized, contactMove.normalized));
                    m_RigidBody.AddForceAtPosition(contactMove * AirMoveForce * airSpeedMult, FeetPos, ForceMode.Acceleration);
                }


                // TODO adding 90 deg to the character rotation works out, it might be a hack tho and is not tested in every scenario, could break
                float targetRotationTorque = MMath.DeltaAngle((m_RigidBody.rotation.eulerAngles.y + 90) * MMath.Deg2Rad, MMath.Atan2(m_TargetLookDir.z, -m_TargetLookDir.x)) * MMath.Rad2Deg;
                if (m_IsStableGrounded && !m_IsSliding)
                {
                    targetRotationTorque *= GroundRotationRate;
                }
                else
                {
                    targetRotationTorque *= AirRotationRate;
                }
                targetRotationTorque *= rotRateMult;
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


            // Jump
            if (m_ScheduledJump && !m_HasJumped && m_AirborneTimer < 0.1f)
            {
                m_ScheduledJump = false;
                m_ForceAirborneTimer = 0.05f;
                Vector3 jumpDir = -Physics.gravity.normalized;
                // TODO add a sliding jump here that is perpendicular to the slide normal
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, jumpDir);
                Vector3 jumpForce = jumpDir * JumpForce;
                StartCoroutine(CO_Jump(jumpForce, 3));

                m_HasJumped = true;
            }

            // Feet drag
            if (m_IsStableGrounded && !m_IsSliding)
            {
                /* TODO find better approach for this...
                 * When applying an upwards force to a character the feet dampening kicks in and limits the upwards motion
                 * However, when simply not dampening the y component of the velocity, walking up slopes causes issues
                 * In other words: Correctly walking up slopes is only possible because of the dampening applied here.
                 * That is weird and should not be the case, so walking up slopes needs a fix!
                */
                float lerpFactor = MMath.InverseLerpClamped(4f, 6f, MMath.Abs(m_RigidBody.velocity.y));

                float feetLinearResistance = Mathf.Clamp01(1 - FeetLinearResistance * dt);
                m_RigidBody.velocity = Vector3.Scale(m_RigidBody.velocity, Vector3.Lerp(Vector3.one * feetLinearResistance, new Vector3(feetLinearResistance, 1, feetLinearResistance), lerpFactor));
                
                m_RigidBody.angularVelocity *= Mathf.Clamp01(1 - FeetAngularResistance * dt);

                //Vector3 accVel = (m_RigidBody.GetAccumulatedForce() * Mathf.Clamp01(1 - FeetLinearResistance * dt)) - m_RigidBody.GetAccumulatedForce();
                //m_RigidBody.AddForceAtPosition(accVel, feetPos, ForceMode.Force);
                //Vector3 accTorque = m_RigidBody.GetAccumulatedTorque() - (m_RigidBody.GetAccumulatedTorque() * Mathf.Clamp01(1 - FeetAngularResistance * dt));
                //m_RigidBody.AddTorque(accTorque, ForceMode.Force);
            }

            float frictionTarget = MMath.LerpClamped(StationaryFriction, MovementFriction, contactMove.magnitude); ;
            if (!m_IsStableGrounded || m_IsSliding)
                frictionTarget = 0;
            m_PhysicsMaterial.staticFriction = frictionTarget;
            m_PhysicsMaterial.dynamicFriction = frictionTarget;

            if (DebugCharacter)
                Debug.DrawLine(transform.position, transform.position + contactMove, Color.green, dt, false);
        }

        private int m_GroundColliderCount = 0;
        private Collider[] m_GroundColliders = new Collider[8];

        private RaycastHit[] m_GroundHits = new RaycastHit[32];
        private bool DetectGround(out RaycastHit groundHitResult, out RaycastHit preciseGroundHitResult)
        {
            groundHitResult = new RaycastHit();
            preciseGroundHitResult = new RaycastHit();

            
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);
            float distance = GroundDetectionDistance + SkinThickness;

            int hits = 0;
            if (Collider is CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = (CapsuleCollider)Collider;
                float scaledRadius = capsuleCollider.radius * MMath.Max(Vector3.ProjectOnPlane(transform.localScale, capsuleCollider.direction == 0 ? Vector3.right : (capsuleCollider.direction == 1 ? Vector3.up : Vector3.forward)));
                float scaledHeight = MMath.Max(capsuleCollider.height, capsuleCollider.radius * 2) * (capsuleCollider.direction == 0 ? transform.localScale.x : (capsuleCollider.direction == 1 ? transform.localScale.y : transform.localScale.z));

                float capsuleHalfHeightWithoutHemisphereScaled = scaledHeight / 2 - scaledRadius;
                Vector3 p1 = transform.TransformPoint(capsuleCollider.center) + transform.TransformDirection(Vector2.up) * capsuleHalfHeightWithoutHemisphereScaled;
                Vector3 p2 = transform.TransformPoint(capsuleCollider.center) - transform.TransformDirection(Vector2.up) * capsuleHalfHeightWithoutHemisphereScaled;
                float raycastRadius = scaledRadius - SkinThickness;
                hits = Physics.CapsuleCastNonAlloc(p1, p2, raycastRadius, Vector3.down, m_GroundHits, distance, layerMask);

                if (DebugCharacter)
                {
                    DebugHelper.DrawWireSphere(p2, raycastRadius, Color.red, Time.fixedDeltaTime, false);
                    DebugHelper.DrawWireSphere(p2 + Vector3.down * distance, raycastRadius, Color.red, Time.fixedDeltaTime, false);
                }
            }
            else if (Collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)Collider;
                Ray ray = new Ray();
                ray.origin = transform.TransformPoint(sphereCollider.center);
                ray.direction = Vector3.down;
                float scaledRadius = sphereCollider.radius * MMath.Max(transform.localScale);
                float radius = scaledRadius - SkinThickness;
                hits = Physics.SphereCastNonAlloc(ray, radius, m_GroundHits, distance, layerMask);

                if (DebugCharacter)
                {
                    DebugHelper.DrawWireSphere(ray.origin, radius, Color.red, Time.fixedDeltaTime, false);
                    DebugHelper.DrawWireSphere(ray.GetPoint(distance), radius, Color.red, Time.fixedDeltaTime, false);
                }
            }
            else
                Debug.Assert(false, "Collider type is not supported!", gameObject);

            if (hits == 0)
                return false;

            groundHitResult.distance = float.PositiveInfinity;
            m_GroundColliderCount = 0;
            for (int i = 0; i < hits; i++)
            {
                // Discard overlaps
                if (m_GroundHits[i].distance == 0)
                    continue;
                // Discard collisions that are further away
                if (m_GroundHits[i].distance > groundHitResult.distance)
                    continue;
                // Discard self collisions
                if (m_GroundHits[i].collider.transform == Rigidbody.transform)
                    continue;
                if (m_GroundHits[i].collider.transform.IsChildOf(Rigidbody.transform))
                    continue;

                groundHitResult = m_GroundHits[i];
                m_GroundColliders[i] = groundHitResult.collider;
                m_GroundColliderCount++;
            }
            if (groundHitResult.distance == float.PositiveInfinity)
                return false;

            // Raycast for precise ground collision
            //bool preciseHit = Physics.Raycast(groundHitResult.point + Vector3.up * 0.0001f + groundHitResult.normal * 0.001f, -groundHitResult.normal, out preciseGroundHitResult, 0.1f, layerMask);
            //if (!preciseHit)
            //    preciseHit = Physics.Raycast(groundHitResult.point - Vector3.up * 0.0001f + groundHitResult.normal * 0.001f, -groundHitResult.normal, out preciseGroundHitResult, 0.1f, layerMask);
            bool preciseHit = groundHitResult.collider.Raycast(new Ray(groundHitResult.point + Vector3.up * 0.0001f + groundHitResult.normal * 0.001f, -groundHitResult.normal), out preciseGroundHitResult, 0.1f);
            if (!preciseHit)
                preciseHit = groundHitResult.collider.Raycast(new Ray(groundHitResult.point - Vector3.up * 0.0001f + groundHitResult.normal * 0.001f, -groundHitResult.normal), out preciseGroundHitResult, 0.1f);

            return preciseHit;
        }

        private bool LedgeDetection()
        {
            float radius = LedgeDetectionRadius;
            Vector3 offset = Vector3.right * LedgeDetectionDistance;
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            m_LedgeDetectionFrame++;
            int frame = m_LedgeDetectionFrame % c_LedgeDetectionIterations;

            int ledgeCount = 0;
            int hitCount;

            for (int i = 0; i < c_LedgeDetectionSamples; i++)
            {
                int ledgeId = i * c_LedgeDetectionIterations + frame;

                Vector3 direction = Quaternion.Euler(0, ledgeId / (float)c_TotalLedgeDetectionSamples * -360, 0) * offset;
                Vector3 p1 = FeetPos + Vector3.up * LedgeDetectionStart + direction;
                Vector3 p2 = FeetPos + Vector3.up * LedgeDetectionEnd + direction;

                hitCount = Physics.SphereCastNonAlloc(p1, radius, (p2 - p1).normalized, m_GroundHits, (p2 - p1).magnitude, layerMask);

                RaycastHit groundHitResult = new RaycastHit();
                groundHitResult.distance = float.PositiveInfinity;
                bool groundFound = false;
                for (int j =  0; j < hitCount; j++)
                {
                    // Discard overlaps
                    if (m_GroundHits[j].distance == 0)
                        continue;
                    // Discard collisions that are further away
                    if (m_GroundHits[j].distance > groundHitResult.distance)
                        continue;
                    // Discard self collisions
                    if (m_GroundHits[j].collider.transform == Rigidbody.transform)
                        continue;
                    if (m_GroundHits[j].collider.transform.IsChildOf(Rigidbody.transform))
                        continue;

                    groundHitResult = m_GroundHits[j];
                    groundFound = true;
                }

                m_LedgeSamples[ledgeId].IsLedge = !groundFound;
                m_LedgeSamples[ledgeId].Direction = direction;
                m_LedgeSamples[ledgeId].StartPosition = p1;
                m_LedgeSamples[ledgeId].EndPosition = p2;
                m_LedgeSamples[ledgeId].Hit = groundHitResult;

                if (!groundFound)
                {
                    ledgeCount++;
                }
            }

            return ledgeCount > 0;
        }
        private int DirectionToLedgeId(Vector3 direction)
        {
            return MMath.Mod(MMath.RoundToInt(MMath.DirToAng(direction.XZ()) / MMath.TAU * c_LedgeDetectionSamples), c_LedgeDetectionSamples);
        }


        private bool DetectGroundMagnetism(out RaycastHit hit)
        {
            float radius = GroundMagnetismRadius;
            Vector3 p1 = FeetPos + Vector3.up * radius;
            Vector3 p2 = FeetPos - Vector3.up * GroundMagnetismDepth;
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            int hitCount = Physics.SphereCastNonAlloc(p1, radius, (p2 - p1).normalized, m_GroundHits, (p2 - p1).magnitude, layerMask);

            // Trajectory tests
            Vector2 vel2D = new Vector2(Rigidbody.velocity.XZ().magnitude, Rigidbody.velocity.y);
            (float a, float b) trajectoryParams = CalculateParabola(vel2D, Physics.gravity.y);
            if (DebugCharacter)
            {
                for (int i = 0; i < 200; i++)
                {
                    float px1 = i * 0.1f;
                    float px2 = (i + 1) * 0.1f;
                    float py1 = Parabola(trajectoryParams.a, trajectoryParams.b, px1);
                    float py2 = Parabola(trajectoryParams.a, trajectoryParams.b, px2);
                    Debug.DrawLine(
                        FeetPos + Rigidbody.velocity.FlattenY().normalized * px1 + Vector3.up * py1,
                        FeetPos + Rigidbody.velocity.FlattenY().normalized * px2 + Vector3.up * py2, 
                        Color.blue);
                }
            }

            RaycastHit groundHitResult = new RaycastHit();
            groundHitResult.distance = float.PositiveInfinity;
            groundHitResult.point = Vector3.positiveInfinity;
            float closestDistance = float.PositiveInfinity;
            bool groundFound = false;
            for (int i = 0; i < hitCount; i++)
            {
                // Discard overlaps
                if (m_GroundHits[i].distance == 0)
                    continue;
                // Discard collisions that are further away
                if (m_GroundHits[i].distance > groundHitResult.distance)
                    continue;
                // Discard self collisions
                if (m_GroundHits[i].collider.transform == Rigidbody.transform)
                    continue;
                if (m_GroundHits[i].collider.transform.IsChildOf(Rigidbody.transform))
                    continue;

                // TODO correctly transform the 3D contact point so that the closest distance can be calculated
                Vector3 pointFeetSpace = m_GroundHits[i].point - FeetPos;
                Vector2 point2D = new Vector2(pointFeetSpace.XZ().magnitude, pointFeetSpace.y);
                Vector2 sampledPoint = GetClosestPointOnParabola(trajectoryParams.a, trajectoryParams.b, point2D);
                float sampledDistance = Vector2.Distance(point2D, sampledPoint);
                if (sampledDistance > closestDistance)
                    continue;

                groundHitResult = m_GroundHits[i];
                closestDistance = sampledDistance;
                groundFound = true;
            }

            if (DebugCharacter)
            {
                DebugHelper.DrawWireSphere(p1, radius, groundFound ? Color.green : Color.red, Time.fixedDeltaTime, false);
                DebugHelper.DrawWireSphere(p2, radius, groundFound ? Color.green : Color.red, Time.fixedDeltaTime, false);
            }

            hit = groundHitResult;
            return groundFound;
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

        #region Parabolic Math Helper

        /// <summary>
        /// Defines a 2D parabola of form y=ax²+bx that starts in (0,0)
        /// </summary>
        static float Parabola(float a, float b, float x)
        {
            return a * x * x + b * x;
        }
        /// <summary>
        /// Calculates the parabolic coefficients a and b that define a parabola matching a projectile path with initial velocity and signed gravity
        /// </summary>
        private static (float a, float b) CalculateParabola(Vector2 velocity, float gravity)
        {
            float a = gravity / (2 * velocity.x * velocity.x);
            float b = velocity.y / velocity.x;
            return (a, b);
        }
        /// <summary>
        /// Calculates the closest location on a 2D parabola defined by a and b that starts in (0,0) to a specific point
        /// </summary>
        private static Vector2 GetClosestPointOnParabola(float a, float b, Vector2 point)
        {
            // ChatGPT to the rescue

            // TODO return separate solutions for a = 0

            // Coefficients of the cubic equation in the form Ax^3 + Bx^2 + Cx + D = 0
            float A = 2 * a * a;
            float B = 3 * a * b;
            float C = -2 * a * point.y + b * b + 1;
            float D = -b * point.y - point.x;

            // Convert to a depressed cubic t^3 + pt + q = 0 using the substitution x = t - B/(3A)
            float p = (3 * A * C - B * B) / (3 * A * A);
            float q = (2 * B * B * B - 9 * A * B * C + 27 * A * A * D) / (27 * A * A * A);

            // Calculate the discriminant
            float discriminant = q * q / 4 + p * p * p / 27;

            float[] roots;
            if (discriminant > 0)
            {
                // One real root and two complex roots
                float sqrtDiscriminant = MMath.Sqrt(discriminant);
                float u = MMath.Cbrt(-q / 2 + sqrtDiscriminant);
                float v = MMath.Cbrt(-q / 2 - sqrtDiscriminant);
                float root1 = u + v - B / (3 * A);
                roots = new float[] { root1 };
            }
            else if (discriminant == 0)
            {
                // All roots are real and at least two are equal
                float u = MMath.Cbrt(-q / 2);
                float root1 = 2 * u - B / (3 * A);
                float root2 = -u - B / (3 * A);
                roots = new float[] { root1, root2 };
            }
            else
            {
                // Three distinct real roots
                float rho = MMath.Sqrt(-p * p * p / 27);
                float theta = MMath.Acos(-q / (2 * rho));
                float rhoCbrt = MMath.Cbrt(rho);
                float root1 = 2 * rhoCbrt * MMath.Cos(theta / 3) - B / (3 * A);
                float root2 = 2 * rhoCbrt * MMath.Cos((theta + 2 * MMath.PI) / 3) - B / (3 * A);
                float root3 = 2 * rhoCbrt * MMath.Cos((theta + 4 * MMath.PI) / 3) - B / (3 * A);
                roots = new float[] { root1, root2, root3 };
            }

            // Only return the closest point
            float closestX = roots[0];
            float closestY = Parabola(a, b, closestX);
            for (int i = 0; i < roots.Length; i++)
            {
                float py = Parabola(a, b, roots[i]);
                if (Vector2.Distance(new Vector2(closestX, closestY), point) > Vector2.Distance(new Vector2(roots[i], py), point))
                {
                    closestX = roots[i];
                    closestY = py;
                }
            }

            return new Vector2(closestX, closestY);
        }

        #endregion
    }
}
