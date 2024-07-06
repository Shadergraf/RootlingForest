using Manatea.GameplaySystem;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Manatea.AdventureRoots
{

    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : MonoBehaviour
    {
        public float GroundMoveForce = 1;
        public float AirMoveForce = 1;
        public float StepHeight = 0.4f;
        public float FeetLinearResistance = 2;
        public float FeetAngularResistance = 2;
        public float StationaryFriction = 1;
        public float MovementFriction = 0.1f;
        public float GroundRotationRate = 10;
        public float AirRotationRate = 0.05f;

        [Header("Jump")]
        public float JumpForce = 5;
        public float JumpMoveAlignment = 0.75f;

        [Header("Ground Magnetism")]
        public bool EnableGroundMagnetism = false;
        public float GroundMagnetismRadiusStart = 0.5f;
        public float GroundMagnetismRadiusEnd = 1.5f;
        public float GroundMagnetismDepth = 0.5f;
        public float GroundMagnetismForce = 50;

        [Header("Ledge Detection")]
        public bool EnableLedgeDetection = false;
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

        public float m_StabilizationForce = 50;
        public float m_StabilizationForcePoles = 50;

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
        public bool DebugLocomotion = false;
        public bool DebugGroundDetection = false;
        public bool DebugLedgeDetection = false;
        public bool LogLedgeDetection = false;
        public bool DebugGroundMagnetism = false;

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
        private RaycastHit m_GroundHitResult;
        private RaycastHit m_PreciseGroundHitResult;

        // Ledge Detection
        // TODO increase iterations and decrease samples to deffer ledge sampling over multiple frames
        private const int c_LedgeDetectionSamples = 16;
        private const int c_LedgeDetectionIterations = 1;
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
        private struct GroundMagnetismSample
        {
            public Vector3 ClosestPointOnTrajectory;
            public RaycastHit Hit;
        }
        public enum LedgeType
        {
            Pole,
            Cliff,
            BalancingBeam,
            UnevenTerrain,
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
            bool groundDetected = DetectGround(out m_GroundHitResult, out m_PreciseGroundHitResult);
            m_IsStableGrounded = groundDetected;
            m_IsStableGrounded &= m_ForceAirborneTimer <= 0;
            m_IsSliding = m_IsStableGrounded && !IsSlopeWalkable(m_PreciseGroundHitResult.normal);
            m_IsStableGrounded &= !m_IsSliding;
            // Step height
            if (m_IsSliding && Vector3.Dot(m_PreciseGroundHitResult.normal, m_GroundHitResult.normal) < 0.9f && Vector3.Project(m_PreciseGroundHitResult.point - FeetPos, transform.up).magnitude < StepHeight)
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
                var tagOwner = m_GroundHitResult.collider.GetComponentInParent<GameplayTagOwner>();
                if (tagOwner && tagOwner.Tags.Contains(m_NoStableGroundTag))
                {
                    m_IsStableGrounded = false;
                }
            }


            Vector3 contactMove = m_ScheduledMove;


            // Jump
            if (m_ScheduledJump && !m_HasJumped && m_AirborneTimer < 0.1f)
            {
                m_ScheduledJump = false;
                m_ForceAirborneTimer = 0.05f;

                if (contactMove != Vector3.zero)
                {
                    Vector3 initialDir = m_RigidBody.velocity;
                    Vector3 targetDir = contactMove.FlattenY().WithMagnitude(initialDir.FlattenY().magnitude) + Vector3.up * initialDir.y;
                    m_RigidBody.velocity = Vector3.Slerp(initialDir, targetDir, contactMove.magnitude * JumpMoveAlignment);
                }

                Vector3 jumpDir = -Physics.gravity.normalized;
                // TODO add a sliding jump here that is perpendicular to the slide normal
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, jumpDir);
                Vector3 jumpForce = jumpDir * JumpForce;
                StartCoroutine(CO_Jump(jumpForce, 3));

                m_HasJumped = true;
            }


            #region Ledge Detection

            if (EnableLedgeDetection)
            {
                bool ledgeFound = LedgeDetection();
                //if (DebugCharacter)
                //{
                //    for (int i = 0; i < c_TotalLedgeDetectionSamples; i++)
                //    {
                //        var sample = m_LedgeSamples[i];
                //        DebugHelper.DrawWireCapsule(sample.StartPosition, transform.position + sample.Direction, LedgeDetectionRadius, sample.IsLedge ? Color.red : Color.green);
                //    }
                //}

                // TODO better analysis of what kind of ledge we are currently dealing with
                // Single ledge, ledge on one distinct side of the samples (standing on on the edge of teh crater)
                // Dual ledge, ledge on two distinct regions (when balancing on a beam)
                // Multi ledge, multiple/random ledge regions (when balancing over a T-crossing)
                // Pole ledge, ledges all around the player (when balancing on a single pole)


                // TODO when detecting ground below the player (in a bigger radius) slightly push the player towards that direction
                //      to make difficult jumps a bit easier (like landing on a thin pole, or making a long jump)

                if (ledgeFound && !m_ScheduledJump)
                {
                    if (m_IsStableGrounded)
                    {
                        #region Analyze Ledge Features

                        // Average ledge direction
                        Vector3 averageLedgeDir = FeetPos;
                        for (int i = 0; i < m_LedgeSamples.Length; i++)
                        {
                            Vector3 delta = m_LedgeSamples[i].Direction;
                            delta *= (m_LedgeSamples[i].IsLedge ? 1 : -1);
                            delta /= m_LedgeSamples.Length;
                            if (DebugLedgeDetection)
                            {
                                Debug.DrawLine(averageLedgeDir, averageLedgeDir + delta, m_LedgeSamples[i].IsLedge ? Color.red : Color.green);
                            }
                            averageLedgeDir += delta;
                        }
                        averageLedgeDir -= FeetPos;

                        if (DebugLedgeDetection)
                        {
                            DebugHelper.DrawWireSphere(averageLedgeDir, 0.2f, Color.blue);
                        }

                        // Ledge noise level
                        float noise = 0;
                        for (int i = 0; i < m_LedgeSamples.Length; i++)
                        {
                            if (m_LedgeSamples[i].IsLedge ^ m_LedgeSamples[MMath.Mod(i + 1, m_LedgeSamples.Length)].IsLedge)
                            {
                                noise++;
                            }
                        }
                        noise /= m_LedgeSamples.Length;

                        // Ledge amount
                        float amount = 0;
                        for (int i = 0; i < m_LedgeSamples.Length; i++)
                        {
                            if (m_LedgeSamples[i].IsLedge)
                            {
                                amount++;
                            }
                        }
                        amount /= m_LedgeSamples.Length;

                        if (LogLedgeDetection)
                        {
                            Debug.Log("Noise level: " + noise);
                            Debug.Log("Ledge amount: " + amount);
                        }

                        LedgeType currentLedgeType = LedgeType.Pole;
                        if (noise <= 0.2 && amount >= 0.85)
                        {
                            currentLedgeType = LedgeType.Pole;
                        }
                        else if (noise <= 0.2 && amount < 0.75)
                        {
                            currentLedgeType = LedgeType.Cliff;
                        }
                        else if (noise > 0.2 && noise <= 0.55 && amount > 0.25)
                        {
                            currentLedgeType = LedgeType.BalancingBeam;
                        }
                        else
                        {
                            currentLedgeType = LedgeType.UnevenTerrain;
                        }
                        if (LogLedgeDetection)
                        {
                            Debug.Log("Current Ledge Type: " + currentLedgeType);
                        }

                        #endregion


                        m_LedgeTimer += dt;

                        Vector3 ledgeDir = m_GroundHitResult.point - FeetPos;
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


                        // Disable ledge force if walking down a cliff
                        float intentionalOverride = 0;
                        if (currentLedgeType == LedgeType.Cliff && m_ScheduledMove != Vector3.zero)
                        {
                            intentionalOverride = MMath.RemapClamped(0.2f, -0.25f, 1, 0, Vector3.Dot(averageLedgeDir.normalized, m_ScheduledMove.normalized));
                        }

                        // TODO remove this and put it in a dedicated ability that allows the player to balance better by using their hands
                        // Stabilize when holding mouse
                        if (Input.GetMouseButton(0) || (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonWest.ReadValue() > 0))
                        {
                            ledgeForce *= LedgeStableBalancingForce;
                            contactMove *= LedgeStableMoveMultiplier;
                        }
                        else
                        {
                            ledgeForce *= MMath.Lerp(LedgeBalancingForce, 1, intentionalOverride);
                            contactMove *= MMath.Lerp(LedgeMoveMultiplier, 1, intentionalOverride);
                        }

                        // Balance attribute
                        if (m_AttributeOwner && m_AttributeOwner.TryGetAttributeEvaluatedValue(m_LedgeBalancingAttribute, out float att_balance))
                        {
                            ledgeForce *= att_balance;
                        }

                        // Stabilize player when not moving
                        if (m_ScheduledMove == Vector3.zero)
                        {
                            Vector3 stabilizationForce = Vector3.ProjectOnPlane(m_PreciseGroundHitResult.normal, m_GroundHitResult.normal);
                            stabilizationForce = stabilizationForce.FlattenY().normalized + stabilizationForce.y * Vector3.up;
                            if (currentLedgeType == LedgeType.Pole)
                            {
                                stabilizationForce *= m_StabilizationForcePoles;

                            }
                            else
                            {
                                stabilizationForce *= m_StabilizationForce;
                            }

                            ledgeForce += stabilizationForce * (1 - m_ScheduledMove.magnitude);
                        }

                        m_RigidBody.AddForceAtPosition(ledgeForce, FeetPos, ForceMode.Acceleration);
                    }
                }
                else
                {
                    m_LedgeTimer = 0;
                }
            }

            #endregion


            #region Ground Magnetism

            if (EnableGroundMagnetism)
            {
                // TODO better ground magnetism
                // Possible approach: Calculate multiple sphere casts along the player trajectory and record best landing spot
                // Nudge player in that direction so that the accumulated velocity from those nudges, over roughly the time it will
                // take to reach the target point on the trajectory, equal the required delta to move the player from the
                // target point on the trajectory to the actual hit point.
                bool groundMagnetFound = DetectGroundMagnetismNEW(out RaycastHit groundMagnetHit);
                if (groundMagnetFound)
                {
                    //if (Rigidbody.velocity.y < 0 && !groundDetected)
                    //{
                    //    Vector3 groundMagnetForce = groundMagnetHit.point - FeetPos;
                    //    groundMagnetForce = groundMagnetForce.FlattenY();
                    //    groundMagnetForce = groundMagnetForce.ClampMagnitude(0, 0.2f) * 5;
                    //    groundMagnetForce *= GroundMagnetismForce;
                    //    Rigidbody.AddForce(groundMagnetForce, ForceMode.Acceleration);
                    //}

                }
            }

            #endregion


            // The direction we want to move in
            if (DebugLocomotion)
            {
                Debug.DrawLine(transform.position, transform.position + m_ScheduledMove, Color.blue, dt, false);
                if (groundDetected)
                    Debug.DrawLine(m_PreciseGroundHitResult.point, m_PreciseGroundHitResult.point + m_PreciseGroundHitResult.normal, Color.black, dt, false);
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
                        Vector3 wallNormal = Vector3.ProjectOnPlane(m_PreciseGroundHitResult.normal, Physics.gravity.normalized).normalized;
                        if (DebugGroundDetection)
                            Debug.DrawLine(m_PreciseGroundHitResult.point + Vector3.one, m_PreciseGroundHitResult.point + Vector3.one + wallNormal, Color.blue, dt, false);
                        if (Vector3.Dot(wallNormal, contactMove) < 0)
                        {
                            contactMove = Vector3.ProjectOnPlane(contactMove, wallNormal).normalized * contactMove.magnitude;
                        }
                    }
                    else
                    {
                        // When we walk perpendicular to a slope we do not expect to move down, we expect the height not to change
                        // So here we cancel out the gravity factor to reduce this effect
                        contactMove += -Physics.gravity * dt * (1 + Vector3.Dot(contactMove, -m_PreciseGroundHitResult.normal)) * 0.5f;
                        contactMove = Vector3.ProjectOnPlane(contactMove, m_PreciseGroundHitResult.normal);
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

            if (DebugLocomotion)
                Debug.DrawLine(transform.position, transform.position + contactMove, Color.green, dt, false);
        }


        public bool IsSlopeWalkable(Vector3 normal)
        {
            return MMath.Acos(MMath.ClampNeg1to1(Vector3.Dot(normal, -Physics.gravity.normalized))) * MMath.Rad2Deg <= MaxSlopeAngle;
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

                if (DebugGroundDetection)
                {
                    DebugHelper.DrawWireCapsule(p2, p2 + Vector3.down * distance, raycastRadius, Color.red);
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

                if (DebugGroundDetection)
                {
                    DebugHelper.DrawWireCapsule(ray.origin, ray.GetPoint(distance), radius, Color.red);
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

            bool preciseHit = groundHitResult.collider.Raycast(new Ray(groundHitResult.point + Vector3.up * 0.01f + Vector3.Cross(Vector3.Cross(groundHitResult.normal, Vector3.up), groundHitResult.normal) * 0.01f, Vector3.down), out preciseGroundHitResult, 0.05f);
            return preciseHit;
        }
        private bool LedgeDetection()
        {
            float radius = LedgeDetectionRadius;
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            m_LedgeDetectionFrame++;
            int frame = m_LedgeDetectionFrame % c_LedgeDetectionIterations;

            int ledgeCount = 0;
            int hitCount;

            for (int i = 0; i < c_LedgeDetectionSamples; i++)
            {
                int ledgeId = i * c_LedgeDetectionIterations + frame;

                Vector3 direction = Quaternion.Euler(0, ledgeId / (float)c_TotalLedgeDetectionSamples * -360, 0) * Vector3.right;
                direction *= LedgeDetectionDistance;
                Vector3 p1 = FeetPos + Vector3.up * LedgeDetectionStart + direction;
                Vector3 p2 = FeetPos + Vector3.up * LedgeDetectionEnd + direction;

                hitCount = Physics.SphereCastNonAlloc(p1, radius, (p2 - p1).normalized, m_GroundHits, (p2 - p1).magnitude, layerMask);
                if (DebugLedgeDetection)
                {
                    DebugHelper.DrawWireCapsule(p1, p2, LedgeDetectionRadius, Color.black * 0.5f);
                }

                RaycastHit groundHitResult = new RaycastHit();
                groundHitResult.distance = float.PositiveInfinity;
                bool groundFound = false;
                for (int j =  0; j < hitCount; j++)
                {
                    var hit = m_GroundHits[j];

                    // Discard overlaps
                    if (hit.distance == 0)
                        continue;
                    // Discard collisions that are further away
                    if (hit.distance > groundHitResult.distance)
                        continue;
                    // Discard self collisions
                    if (hit.collider.transform == Rigidbody.transform)
                        continue;
                    if (hit.collider.transform.IsChildOf(Rigidbody.transform))
                        continue;

                    // Precise raycast
                    bool hasPreciseLedge = hit.collider.Raycast(new Ray(hit.point + Vector3.up * 0.01f + Vector3.Cross(Vector3.Cross(hit.normal, Vector3.up), hit.normal) * 0.01f, Vector3.down), out RaycastHit preciseHit, 0.05f);
                    if (!hasPreciseLedge)
                        continue;
                    if (!IsSlopeWalkable(preciseHit.normal))
                        continue;
                    
                    if (DebugLedgeDetection)
                    {
                        Debug.DrawLine(preciseHit.point, preciseHit.point + preciseHit.normal * 0.1f, Color.cyan);
                        DebugHelper.DrawWireCapsule(p1, p1 + (p2 - p1).normalized * hit.distance, LedgeDetectionRadius, Color.green);
                    }
                    
                    // Test if this could be the ground we are currently standing on
                    Plane ledgeGroundPlane = new Plane(preciseHit.normal, preciseHit.point);
                    float feetDistance = MMath.Abs(ledgeGroundPlane.GetDistanceToPoint(FeetPos));
                    Plane feetGroundPlane = new Plane(m_PreciseGroundHitResult.normal, m_PreciseGroundHitResult.point);
                    float contactDistance = MMath.Abs(feetGroundPlane.GetDistanceToPoint(preciseHit.point));
                    if (feetDistance > 0.3f && contactDistance > 0.4f)
                    {
                        if (LogLedgeDetection)
                        {
                            Debug.Log(feetDistance + " - " + contactDistance);
                        }
                        continue;
                    }

                    groundHitResult = hit;
                    groundFound = true;
                }
                if (DebugLedgeDetection && !groundFound)
                    DebugHelper.DrawWireCapsule(p1, p2, LedgeDetectionRadius, Color.red);

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
            float radius = GroundMagnetismRadiusStart;
            Vector3 p1 = FeetPos + Vector3.up * radius;
            Vector3 p2 = FeetPos - Vector3.up * GroundMagnetismDepth;
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            int hitCount = Physics.SphereCastNonAlloc(p1, radius, (p2 - p1).normalized, m_GroundHits, (p2 - p1).magnitude, layerMask);

            // Trajectory tests
            Vector2 vel2D = new Vector2(Rigidbody.velocity.XZ().magnitude, Rigidbody.velocity.y);
            (float a, float b) trajectoryParams = CalculateParabola(vel2D, Physics.gravity.y);
            if (DebugGroundMagnetism)
            {
                for (int i = 0; i < 4; i++)
                {
                    const float c_StepSize = 0.5f;
                    float px1 = i * c_StepSize;
                    float px2 = (i + 1) * c_StepSize;
                    float py1 = Parabola(trajectoryParams.a, trajectoryParams.b, px1);
                    float py2 = Parabola(trajectoryParams.a, trajectoryParams.b, px2);
                    Debug.DrawLine(
                        FeetPos + Rigidbody.velocity.FlattenY().normalized * px1 + Vector3.up * py1,
                        FeetPos + Rigidbody.velocity.FlattenY().normalized * px2 + Vector3.up * py2,
                        Color.blue);
                    DebugHelper.DrawWireCircle(
                        FeetPos + Rigidbody.velocity.FlattenY().normalized * px2 + Vector3.up * py2,
                        0.4f, Vector3.up, Color.blue);
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
                if (m_GroundHits[i].normal.y <= 0)
                    continue;
                if (m_GroundHits[i].point.y > FeetPos.y)
                    continue;
                if (Vector3.Dot(m_GroundHits[i].point - FeetPos, Rigidbody.velocity) < 0)
                    continue;

                // TODO correctly transform the 3D contact point so that the closest distance can be calculated
                Vector3 pointFeetSpace = m_GroundHits[i].point - FeetPos;
                Vector2 point2D = new Vector2(pointFeetSpace.XZ().magnitude, pointFeetSpace.y);
                Vector2 sampledPoint = GetClosestPointOnParabola(trajectoryParams.a, trajectoryParams.b, point2D);
                Vector3 pointOnTrajectory = FeetPos + Rigidbody.velocity.FlattenY().normalized * sampledPoint.x + Vector3.up * sampledPoint.y;
                float distanceHeuristic = Vector3.Distance(m_GroundHits[i].point, pointOnTrajectory) + Vector3.Distance(m_GroundHits[i].point, FeetPos);
                if (DebugGroundMagnetism)
                {
                    DebugHelper.DrawWireSphere(pointOnTrajectory, 0.2f, Color.white);
                    DebugHelper.DrawWireSphere(m_GroundHits[i].point, 0.2f, Color.red);
                }
                if (distanceHeuristic > closestDistance)
                    continue;

                groundHitResult = m_GroundHits[i];
                groundHitResult.point = pointOnTrajectory;
                closestDistance = distanceHeuristic;
                groundFound = true;
            }

            if (DebugGroundMagnetism)
            {
                DebugHelper.DrawWireSphere(p1, radius, groundFound ? Color.green : Color.red, Time.fixedDeltaTime, false);
                DebugHelper.DrawWireSphere(p2, radius, groundFound ? Color.green : Color.red, Time.fixedDeltaTime, false);

                DebugHelper.DrawWireCircle(groundHitResult.point, 0.2f, Color.green);
            }

            hit = groundHitResult;
            return groundFound;
        }
        private bool DetectGroundMagnetismNEW(out RaycastHit hit)
        {
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);


            // Trajectory tests
            Vector2 vel2D = new Vector2(Rigidbody.velocity.XZ().magnitude, Rigidbody.velocity.y);
            (float a, float b) trajectoryParams = CalculateParabola(vel2D, Physics.gravity.y);

            const int c_TrejectoryIterations = 4;
            const float c_StepSize = 0.5f;
            bool groundFound = false;
            for (int i = 0; i <= c_TrejectoryIterations; i++)
            {
                float radius = MMath.Lerp(GroundMagnetismRadiusStart, GroundMagnetismRadiusEnd, i / (float)c_TrejectoryIterations);
                float px1 = i * c_StepSize;
                float px2 = (i + 1) * c_StepSize;
                float py1 = Parabola(trajectoryParams.a, trajectoryParams.b, px1);
                float py2 = Parabola(trajectoryParams.a, trajectoryParams.b, px2);
                Vector3 p1 = FeetPos + Rigidbody.velocity.FlattenY().normalized * px1 + Vector3.up * py1;
                Vector3 p2 = FeetPos + Rigidbody.velocity.FlattenY().normalized * px2 + Vector3.up * py2;
                if (DebugGroundMagnetism)
                {
                    Debug.DrawLine(p1, p2, Color.blue);
                    DebugHelper.DrawWireCircle(p2, 0.4f, Vector3.up, Color.blue);
                }

                Vector3 pp1 = p1 + (p1 - p2).normalized * radius / 2;
                int hitCount = Physics.SphereCastNonAlloc(pp1, radius, (p2 - pp1).normalized, m_GroundHits, (p2 - pp1).magnitude, layerMask);
                DebugHelper.DrawWireCapsule(pp1, p2, radius, Color.grey);

                GroundMagnetismSample groundMagnet = new GroundMagnetismSample();
                groundMagnet.Hit.distance = float.PositiveInfinity;
                groundMagnet.Hit.point = Vector3.positiveInfinity;
                float closestDistance = float.PositiveInfinity;
                for (int j = 0; j < hitCount; j++)
                {
                    // Discard overlaps
                    if (m_GroundHits[j].distance == 0)
                        continue;
                    // Discard collisions that are further away
                    if (m_GroundHits[j].distance > groundMagnet.Hit.distance)
                        continue;
                    // Discard self collisions
                    if (m_GroundHits[j].collider.transform == Rigidbody.transform)
                        continue;
                    if (m_GroundHits[j].collider.transform.IsChildOf(Rigidbody.transform))
                        continue;
                    if (m_GroundHits[j].normal.y <= 0)
                        continue;
                    if (m_GroundHits[j].point.y > FeetPos.y)
                        continue;
                    if (Vector3.Dot(m_GroundHits[j].point - FeetPos, Rigidbody.velocity) < 0)
                        continue;

                    // TODO correctly transform the 3D contact point so that the closest distance can be calculated
                    Vector3 pointFeetSpace = m_GroundHits[j].point - FeetPos;
                    Vector2 point2D = new Vector2(pointFeetSpace.XZ().magnitude, pointFeetSpace.y);
                    Vector2 sampledPoint = GetClosestPointOnParabola(trajectoryParams.a, trajectoryParams.b, point2D);
                    Vector3 pointOnTrajectory = FeetPos + Rigidbody.velocity.FlattenY().normalized * sampledPoint.x + Vector3.up * sampledPoint.y;
                    float distanceHeuristic = Vector3.Distance(m_GroundHits[j].point, pointOnTrajectory) + Vector3.Distance(m_GroundHits[j].point, FeetPos);
                    if (DebugGroundMagnetism)
                    {
                        DebugHelper.DrawWireSphere(pointOnTrajectory, 0.2f, Color.green);
                        DebugHelper.DrawWireSphere(m_GroundHits[j].point, 0.2f, Color.red);
                    }
                    if (distanceHeuristic > closestDistance)
                        continue;

                    groundMagnet.ClosestPointOnTrajectory = pointOnTrajectory;
                    groundMagnet.Hit = m_GroundHits[j];
                    groundMagnet.Hit.point = pointOnTrajectory;
                    closestDistance = distanceHeuristic;
                    groundFound = true;
                }
            }

            hit = new RaycastHit();
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
