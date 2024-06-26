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

        public float LedgeDetectionDepth = 0.2f;
        public float LedgeDetectionRadius = 0.2f;

        public float LedgeBalancingForce = 50;
        public float LedgeMoveMultiplier = 0.75f;

        public float LedgeStableBalancingForce = 150;
        public float LedgeStableMoveMultiplier = 0.5f;

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
        private bool m_IsRotating;
        private bool m_ScheduledJump;

        // Constants
        private const float MIN_JUMP_TIME = 0.2f;       // The minimum time after a jump we are guaranteed to be airborne

        // Simulation
        private bool m_IsStableGrounded;
        private bool m_IsSliding;
        private float m_ForceAirborneTimer;
        private float m_AirborneTimer;
        private float m_JumpTimer;
        private bool m_HasJumped;
        private PhysicMaterial m_PhysicsMaterial;
        private Vector3 m_CurrentRotation;

        public Vector3 FeetPos => Collider.ClosestPoint(transform.position + Physics.gravity * 10000);


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
                m_IsRotating = true;
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


            List<RaycastHit> hitResults = new List<RaycastHit>();
            bool ledgeFound = LedgeDetection(hitResults);
            if (groundDetected && ledgeFound)
            {
                Vector3 ledgeForce = Vector3.zero;
                int count = 0;
                for (int i = 0; i < hitResults.Count; i++)
                {
                    var currentHit = hitResults[i];
                    if (currentHit.distance == 0)
                    {
                        continue;
                    }
            
                    Debug.DrawLine(currentHit.point, currentHit.point + currentHit.normal, Color.yellow, Time.fixedDeltaTime);
                    ledgeForce += -currentHit.normal.FlattenY();
                    count++;
                }
                ledgeForce /= MMath.Max(count, 1);

                // Balancing wiggle
                if (m_ScheduledMove != Vector3.zero)
                {
                    Vector3 imbalance = Vector3.Cross(m_ScheduledMove.normalized, Vector3.up);
                    imbalance *= MMath.Pow(Mathf.PerlinNoise1D(Time.time * m_ScheduledMove.magnitude * 0.4f), 2) * 2 - 1;
                    imbalance *= m_ScheduledMove.magnitude;
                    imbalance *= 0.45f;
                    m_ScheduledMove += imbalance;
                }

                // TODO remove this and put it in a dedicated ability that allows the player to balance better by using their hands
                if (Input.GetMouseButton(0))
                {
                    ledgeForce *= LedgeStableBalancingForce;
                    m_ScheduledMove *= LedgeStableMoveMultiplier;
                }
                else
                {
                    ledgeForce *= LedgeBalancingForce;
                    m_ScheduledMove *= LedgeMoveMultiplier;
                }

                if (m_AttributeOwner && m_AttributeOwner.TryGetAttributeEvaluatedValue(m_LedgeBalancingAttribute, out float att_balance))
                {
                    ledgeForce *= att_balance;
                }

                m_RigidBody.AddForceAtPosition(ledgeForce, FeetPos, ForceMode.Acceleration);

                m_IsStableGrounded = true;
                m_IsSliding = false;
            }


            // The direction we want to move in
            if (DebugCharacter)
            {
                Debug.DrawLine(transform.position, transform.position + m_ScheduledMove, Color.blue, dt, false);
                if (groundDetected)
                    Debug.DrawLine(preciseGroundHitResult.point, preciseGroundHitResult.point + preciseGroundHitResult.normal, Color.black, dt, false);
            }


            // Contact Movement
            // movement that results from ground or surface contact
            Vector3 contactMove = m_ScheduledMove;
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


                // Add rotation torque
                if (m_IsRotating)
                {
                    m_CurrentRotation = Vector3.Slerp(transform.forward, m_TargetLookDir, MMath.Damp(0, 1, 100, dt));
                    m_IsRotating = false;
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

        private RaycastHit[] m_GroundHits = new RaycastHit[8];
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

        private RaycastHit[] m_LedgeHits = new RaycastHit[8];
        public int c_LedgeSamples = 16;
        private bool LedgeDetection(List<RaycastHit> hitResults)
        {
            hitResults.Clear();

            float radius;
            float height;
            Vector3 worldCenter;

            // TODO apply scaling here
            if (Collider is CapsuleCollider)
            {
                var capsuleCollider = Collider as CapsuleCollider;
                radius = capsuleCollider.radius;
                height = capsuleCollider.height - radius * 2;
                worldCenter = transform.TransformPoint(capsuleCollider.center);
            }
            else if (Collider is SphereCollider)
            {
                var sphereCollider = Collider as SphereCollider;
                radius = sphereCollider.radius;
                height = 0;
                worldCenter = transform.TransformPoint(sphereCollider.center);
            }
            else
            {
                return false;
            }

            Vector3 offset = Vector3.forward * LedgeDetectionRadius;
            


            int hits;

            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            bool[] ledgeId = new bool[c_LedgeSamples];
            int ledgeCount = 0;

            Vector3 p1 = worldCenter + Vector3.up * LedgeDetectionDepth;
            Vector3 p2 = FeetPos;
            float distance = height + LedgeDetectionDepth;
            float raycastRadius = radius - SkinThickness;
            for (int i = 0; i < c_LedgeSamples; i++)
            {
                RaycastHit groundHitResult = new RaycastHit();
                groundHitResult.distance = float.PositiveInfinity;

                Vector3 p1WithOffset = p1 + Quaternion.Euler(0, i / (float)c_LedgeSamples * 360, 0) * offset;
                Vector3 p1toP2 = p2 - p1WithOffset;
                hits = Physics.SphereCastNonAlloc(p1WithOffset, raycastRadius, p1toP2.normalized, m_GroundHits, p1toP2.magnitude, layerMask);

                for (int j =  0; j < hits; j++)
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
                }

                bool ledge = groundHitResult.distance == float.PositiveInfinity;
                ledgeId[i] = ledge;
                if (ledge)
                {
                    hitResults.Add(new RaycastHit());
                    ledgeCount++;
                }
                else
                {
                    hitResults.Add(groundHitResult);
                }

                if (DebugCharacter)
                {
                    DebugHelper.DrawWireSphere(p1WithOffset, raycastRadius, groundHitResult.distance < float.PositiveInfinity ? Color.red : Color.green, Time.fixedDeltaTime, false);
                    DebugHelper.DrawWireSphere(p1WithOffset + p1toP2, raycastRadius, groundHitResult.distance < float.PositiveInfinity ? Color.red : Color.green, Time.fixedDeltaTime, false);
                    if (!ledge)
                    {
                        Debug.DrawLine(groundHitResult.point, groundHitResult.point + groundHitResult.normal, Color.yellow, Time.fixedDeltaTime);
                    }
                }
            }

            return true;
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

        //private void OnGUI()
        //{
        //    GUILayout.BeginVertical();
        //    GUI.color = Color.red;
        //    GUILayout.Label("Is Grounded:" + m_IsStableGrounded);
        //    GUILayout.Label("Is Sliding:" + m_IsSliding);
        //    if (TryGetComponent(out Joint joint) && joint.connectedBody)
        //    {
        //        GUILayout.Label("Pulling:" + joint.connectedBody.name);
        //        GUILayout.Label("Joint Force:" + joint.currentForce.magnitude);
        //    }
        //    GUILayout.EndVertical();
        //}
    }
}
