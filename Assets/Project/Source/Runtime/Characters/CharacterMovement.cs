using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatea.AdventureRoots
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : MonoBehaviour
    {
        public const float k_SkinThickness = 0.05f;

        public float Speed = 1;
        public float MaxMoveSpeed = 2;
        public float GroundDetectionDistance = 0.01f;
        public float GroundDrag = 5;
        public float AirDrag = 5;
        public float JumpForce = 5;
        public float GroundingAttractionForce = 20;
        public float FeetDrag = 2;
        public float MaxSlopeAngle = 46;

        private CapsuleCollider m_CapsuleCollider;
        private Rigidbody m_RigidBody;

        private Vector3 m_ScheduledMove;
        private bool m_ScheduledJump;
        

        // Simulation
        private bool m_IsGrounded;
        private float m_ForceAirborneTimer;


        private void Start()
        {
            m_CapsuleCollider = GetComponent<CapsuleCollider>();
            m_RigidBody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            Vector3 inputVector = Vector3.zero;
            if (InputSystem.GetDevice<Keyboard>().dKey.isPressed)
                inputVector += Vector3.right;
            if (InputSystem.GetDevice<Keyboard>().aKey.isPressed)
                inputVector += Vector3.left;
            if (InputSystem.GetDevice<Keyboard>().wKey.isPressed)
                inputVector += Vector3.forward;
            if (InputSystem.GetDevice<Keyboard>().sKey.isPressed)
                inputVector += Vector3.back;
            if (inputVector != Vector3.zero)
                inputVector.Normalize();

            m_ScheduledMove = inputVector;


            if (InputSystem.GetDevice<Keyboard>().spaceKey.wasPressedThisFrame)
                m_ScheduledJump = true;
        }

        private void FixedUpdate()
        {
            UpdatePhysics(Time.fixedDeltaTime);
        }

        protected void UpdatePhysics(float dt)
        {

            // Decrement timers
            //m_ForceAirborneTimer = MMath.Max(m_ForceAirborneTimer - dt, 0);

            // Ground detection
            m_IsGrounded = DetectGround(out RaycastHit groundHitResult);

            // The direction we want to move in
            Debug.DrawLine(transform.position, transform.position + m_ScheduledMove, Color.blue, dt, false);

            // Feet drag
            if (m_IsGrounded)
            {
                m_RigidBody.velocity = m_RigidBody.velocity * Mathf.Clamp01(1 - FeetDrag * dt);
            }

            // Contact Movement
            // movement that results from ground or surface contact
            Vector3 contactMove = m_ScheduledMove;
            if (m_IsGrounded && contactMove != Vector3.zero)
            {
                if (MMath.Acos(Vector3.Dot(groundHitResult.normal, -Physics.gravity.normalized)) * MMath.Rad2Deg < MaxSlopeAngle)
                {
                    contactMove = Vector3.ProjectOnPlane(contactMove, groundHitResult.normal);
                }
                else
                {
                    Vector3 wallNormal = Vector3.ProjectOnPlane(groundHitResult.normal, Physics.gravity.normalized).normalized;
                    Debug.DrawLine(groundHitResult.point, groundHitResult.point + wallNormal, Color.blue, dt, false);
                    contactMove = Vector3.ProjectOnPlane(contactMove, wallNormal).normalized * contactMove.magnitude;
                }

                Debug.DrawLine(groundHitResult.point, groundHitResult.point + groundHitResult.normal, Color.red, dt, false);
                Debug.DrawLine(groundHitResult.point, groundHitResult.point + contactMove.normalized, Color.red, dt, false);

                // Increase speed when moving up slopes to make speed more consistent with flat surfaces
                //contactMove *= 1 + MMath.Clamp01(Vector3.Dot(-Physics.gravity.normalized, contactMove.normalized)) / 2;


                m_RigidBody.AddForce(contactMove * Speed, ForceMode.Acceleration);
            }

            // Jump
            //if (m_ScheduledJump && m_IsGrounded)
            //{
            //    m_RigidBody.AddForce(-Physics.gravity.normalized * JumpForce, ForceMode.VelocityChange);
            //    m_ScheduledJump = false;
            //    m_ForceAirborneTimer = 0.05f;
            //}

            Debug.DrawLine(transform.position, transform.position + contactMove, Color.green, dt, false);
        }


        private RaycastHit[] m_GroundHits = new RaycastHit[8];
        private bool DetectGround(out RaycastHit groundHitResult)
        {
            float capsuleHalfHeightWithoutHemisphere = m_CapsuleCollider.height / 2 - m_CapsuleCollider.radius;
            groundHitResult = new RaycastHit();

            int layerMask = LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
            Ray ray = new Ray();
            ray.origin = transform.TransformPoint(m_CapsuleCollider.center - Vector3.up * capsuleHalfHeightWithoutHemisphere);
            ray.direction = Vector3.down;
            float radius = m_CapsuleCollider.radius - k_SkinThickness;
            float distance = GroundDetectionDistance + k_SkinThickness;

            DebugHelper.DrawWireSphere(ray.origin, radius, Color.red, Time.fixedDeltaTime, false);
            DebugHelper.DrawWireSphere(ray.GetPoint(distance), radius, Color.red, Time.fixedDeltaTime, false);
            int hits = Physics.SphereCastNonAlloc(ray, radius, m_GroundHits, distance, layerMask);
            if (hits == 0)
                return false;

            groundHitResult.distance = float.PositiveInfinity;
            for (int i = 0; i < hits; i++)
            {
                if (m_GroundHits[i].distance < groundHitResult.distance && m_GroundHits[i].collider.gameObject != gameObject)
                {
                    groundHitResult = m_GroundHits[i];
                }
            }
            if (groundHitResult.distance == float.PositiveInfinity)
                return false;

            DebugHelper.DrawWireSphere(ray.GetPoint(groundHitResult.distance), radius, Color.green, Time.fixedDeltaTime, false);

            return true;
        }
    }
}
