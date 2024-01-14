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
        public float AirRotationRate = .05f;

        [Header("Collision Detection")]
        public Collider Collider;
        public float SkinThickness = 0.05f;
        public float GroundDetectionDistance = 0.01f;
        public float MaxSlopeAngle = 46;

        [Header("Debug")]
        public bool DebugCharacter = false;


        private Rigidbody m_RigidBody;

        private Vector3 m_ScheduledMove;
        private Vector3 m_TargetRotation;
        private bool m_ScheduledJump;
        

        // Simulation
        private bool m_IsGrounded;
        private bool m_IsSliding;
        private float m_ForceAirborneTimer;
        private PhysicMaterial m_PhysicsMaterial;


        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();

            m_PhysicsMaterial = new PhysicMaterial();
            m_PhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            Collider.material = m_PhysicsMaterial;
        }

        private void OnEnable()
        {
            m_TargetRotation = transform.forward;
        }

        public void Move(Vector3 moveVector, bool rotateTowardsMove = true)
        {
            m_ScheduledMove = moveVector;
            if (rotateTowardsMove && m_ScheduledMove != Vector3.zero)
                m_TargetRotation = m_ScheduledMove.normalized;
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
            // Decrement timers
            m_ForceAirborneTimer = MMath.Max(m_ForceAirborneTimer - dt, 0);

            // Ground detection
            bool groundDetected = DetectGround(out RaycastHit groundHitResult, out RaycastHit preciseGroundHitResult);
            Vector3 feetPos = Collider.ClosestPoint(transform.position + Physics.gravity * 10000);


            m_IsGrounded = groundDetected;
            m_IsGrounded &= m_ForceAirborneTimer <= 0;
            m_IsSliding = m_IsGrounded && MMath.Acos(Vector3.Dot(preciseGroundHitResult.normal, -Physics.gravity.normalized)) * MMath.Rad2Deg > MaxSlopeAngle;
            m_IsGrounded &= !m_IsSliding;
            // Step height
            if (m_IsSliding && Vector3.Dot(preciseGroundHitResult.normal, groundHitResult.normal) < 0.9f && Vector3.Project(preciseGroundHitResult.point - feetPos, transform.up).magnitude < StepHeight)
            {
                m_IsGrounded = true;
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
            if (contactMove != Vector3.zero)
            {
                if (m_IsGrounded)
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

                    m_RigidBody.AddForce(contactMove * GroundMoveForce, ForceMode.Acceleration);
                }
                // Air movement
                else
                {
                    float airSpeedMult = MMath.InverseLerpClamped(0.707f, 0f, Vector3.Dot(m_RigidBody.velocity.normalized, contactMove.normalized));
                    m_RigidBody.AddForce(contactMove * AirMoveForce * airSpeedMult, ForceMode.Acceleration);
                }


                // Add rotation torque
                // TODO adding 90 deg to the character rotation works out, it might be a hack tho and is not tested in every scenario, could break 
                float targetRotationTorque = MMath.DeltaAngle((m_RigidBody.rotation.eulerAngles.y + 90) * MMath.Deg2Rad, MMath.Atan2(m_TargetRotation.z, -m_TargetRotation.x)) * MMath.Rad2Deg;
                if (m_IsGrounded && !m_IsSliding)
                {
                    targetRotationTorque *= GroundRotationRate;
                }
                else
                {
                    targetRotationTorque *= AirRotationRate;
                }
                m_RigidBody.AddTorque(0, targetRotationTorque, 0, ForceMode.Force);
            }


            // Jump
            if (m_ScheduledJump && m_IsGrounded)
            {
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, Physics.gravity.normalized);
                Vector3 jumpForce = -Physics.gravity.normalized * JumpForce;
                m_RigidBody.AddForce(jumpForce, ForceMode.Impulse);
                m_ScheduledJump = false;
                m_ForceAirborneTimer = 0.05f;

                for (int i = 0; i < m_GroundColliderCount; i++)
                {
                    if (m_GroundColliders[i] && m_GroundColliders[i].attachedRigidbody)
                    {
                        m_GroundColliders[i].attachedRigidbody.AddForceAtPosition(-m_RigidBody.velocity, feetPos, ForceMode.Impulse);
                        m_GroundColliders[i].attachedRigidbody.AddForceAtPosition(-m_RigidBody.GetAccumulatedForce(), feetPos, ForceMode.Force);
                    }
                }
            }

            // Feet drag
            if (m_IsGrounded && !m_IsSliding)
            {
                m_RigidBody.velocity *= Mathf.Clamp01(1 - FeetLinearResistance * dt);
                m_RigidBody.angularVelocity *= Mathf.Clamp01(1 - FeetAngularResistance * dt);

                Vector3 accVel = (m_RigidBody.GetAccumulatedForce() * Mathf.Clamp01(1 - FeetLinearResistance * dt)) - m_RigidBody.GetAccumulatedForce();
                m_RigidBody.AddForce(accVel, ForceMode.Force);
                Vector3 accTorque = m_RigidBody.GetAccumulatedTorque() - (m_RigidBody.GetAccumulatedTorque() * Mathf.Clamp01(1 - FeetAngularResistance * dt));
                m_RigidBody.AddTorque(accTorque, ForceMode.Force);
            }

            float frictionTarget = MMath.LerpClamped(StationaryFriction, MovementFriction, contactMove.magnitude); ;
            if (!m_IsGrounded || m_IsSliding)
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

            int layerMask = LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
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
                if (m_GroundHits[i].distance > groundHitResult.distance)
                    continue;
                if (m_GroundHits[i].collider.gameObject == gameObject)
                    continue;

                groundHitResult = m_GroundHits[i];
                m_GroundColliders[i] = groundHitResult.collider;
                m_GroundColliderCount++;
            }
            if (groundHitResult.distance == float.PositiveInfinity)
                return false;

            bool preciseHit = Physics.Raycast(groundHitResult.point + Vector3.up * 0.0001f + groundHitResult.normal * 0.001f, -groundHitResult.normal, out preciseGroundHitResult, 0.1f, layerMask);
            if (!preciseHit)
                preciseHit = Physics.Raycast(groundHitResult.point - Vector3.up * 0.0001f + groundHitResult.normal * 0.001f, -groundHitResult.normal, out preciseGroundHitResult, 0.1f, layerMask);

            return preciseHit;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUI.color = Color.red;
            GUILayout.Label("Is Grounded:" + m_IsGrounded);
            GUILayout.Label("Is Sliding:" + m_IsSliding);
            GUILayout.EndVertical();
        
        }
    }
}
