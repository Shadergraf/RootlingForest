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
        public float Speed = 1;
        public float AirSpeed = 1;
        public float GroundDetectionDistance = 0.01f;
        public float JumpForce = 5;
        public float FeetDrag = 2;
        public float MaxSlopeAngle = 46;
        public float StationaryFriction = 1;
        public float MovementFriction = 0.1f;
        public float StepHeight = 0.4f;

        public float SkinThickness = 0.05f;


        private CapsuleCollider m_CapsuleCollider;
        private Rigidbody m_RigidBody;

        private Vector3 m_ScheduledMove;
        private bool m_ScheduledJump;
        

        // Simulation
        private bool m_IsGrounded;
        private bool m_IsSliding;
        private float m_ForceAirborneTimer;
        private PhysicMaterial m_PhysicsMaterial;


        private void Start()
        {
            m_CapsuleCollider = GetComponent<CapsuleCollider>();
            m_RigidBody = GetComponent<Rigidbody>();

            m_PhysicsMaterial = new PhysicMaterial();
            m_PhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            m_CapsuleCollider.material = m_PhysicsMaterial;
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
            m_ForceAirborneTimer = MMath.Max(m_ForceAirborneTimer - dt, 0);

            // Ground detection
            bool groundDetected = DetectGround(out RaycastHit groundHitResult, out RaycastHit preciseGroundHitResult);
            Vector3 feetPos = transform.position - transform.up * m_CapsuleCollider.height / 2;


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
            Debug.DrawLine(transform.position, transform.position + m_ScheduledMove, Color.blue, dt, false);
            if (groundDetected)
                Debug.DrawLine(preciseGroundHitResult.point, preciseGroundHitResult.point + preciseGroundHitResult.normal, Color.black, dt, false);

            // Feet drag
            if (m_IsGrounded && !m_IsSliding)
            {
                m_RigidBody.velocity = m_RigidBody.velocity * Mathf.Clamp01(1 - FeetDrag * dt);
            }

            if (preciseGroundHitResult.point.y > 0.001f)
                Debug.DebugBreak();

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
                        Debug.DrawLine(preciseGroundHitResult.point + Vector3.one, preciseGroundHitResult.point + Vector3.one + wallNormal, Color.blue, dt, false);
                        if (Vector3.Dot(wallNormal, contactMove) < 0)
                        {
                            contactMove = Vector3.ProjectOnPlane(contactMove, wallNormal).normalized * contactMove.magnitude;
                        }
                    }
                    else
                    {
                        if (Vector3.Dot(contactMove, preciseGroundHitResult.normal) > 0)
                            contactMove = Vector3.ProjectOnPlane(contactMove, preciseGroundHitResult.normal);
                        else
                            contactMove = Vector3.ProjectOnPlane(contactMove - Physics.gravity * dt, preciseGroundHitResult.normal);
                    }

                    m_RigidBody.AddForce(contactMove * Speed, ForceMode.Acceleration);
                }
                // Air movement
                else
                {
                    float airSpeedMult = MMath.InverseLerpClamped(0.707f, 0f, Vector3.Dot(m_RigidBody.velocity.normalized, contactMove.normalized));
                    m_RigidBody.AddForce(contactMove * AirSpeed * airSpeedMult, ForceMode.Acceleration);
                }
            }


            // Jump
            if (m_ScheduledJump && m_IsGrounded)
            {
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, Physics.gravity.normalized);
                m_RigidBody.AddForce(-Physics.gravity.normalized * JumpForce, ForceMode.VelocityChange);
                m_ScheduledJump = false;
                m_ForceAirborneTimer = 0.05f;
            }

            float frictionTarget = MMath.LerpClamped(StationaryFriction, MovementFriction, contactMove.magnitude); ;
            if (!m_IsGrounded || m_IsSliding)
                frictionTarget = 0;
            m_PhysicsMaterial.staticFriction = frictionTarget;
            m_PhysicsMaterial.dynamicFriction = frictionTarget;

            Debug.DrawLine(transform.position, transform.position + contactMove, Color.green, dt, false);
        }


        private RaycastHit[] m_GroundHits = new RaycastHit[8];
        private bool DetectGround(out RaycastHit groundHitResult, out RaycastHit preciseGroundHitResult)
        {
            float capsuleHalfHeightWithoutHemisphere = m_CapsuleCollider.height / 2 - m_CapsuleCollider.radius;
            groundHitResult = new RaycastHit();
            preciseGroundHitResult = new RaycastHit();

            int layerMask = LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
            Ray ray = new Ray();
            ray.origin = transform.TransformPoint(m_CapsuleCollider.center - Vector3.up * capsuleHalfHeightWithoutHemisphere);
            ray.direction = Vector3.down;
            float radius = m_CapsuleCollider.radius - SkinThickness;
            float distance = GroundDetectionDistance + SkinThickness;

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

            bool preciseHit = Physics.Raycast(groundHitResult.point + Vector3.up * 0.0005f + groundHitResult.normal * 0.001f, -groundHitResult.normal, out preciseGroundHitResult, radius, layerMask);
            Debug.Assert(preciseHit);

            return true;
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
