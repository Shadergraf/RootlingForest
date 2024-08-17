using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatea.RootlingForest
{
    public class PlayerController : CharacterController
    {
        public JumpMovementAbility m_JumpAbility;
        public float DashForce;
        public GrabAbility PullAbility;
        public CapsuleCollider TriggerCollider;
        public ClimbAbility m_ClimbAbility;

        public InputActionAsset m_InputAsset;
        public int m_Player = -1;

        public LayerMask m_GrabLayerMask;

        [Header("Debug")]
        public Vector3 m_DebugMoveInput;

        private Collider[] Colliders;
        private int OverlapCount;

        private Keyboard m_Keyboard;
        private Mouse m_Mouse;
        private Gamepad m_Gamepad;

        public float InputSmoothing = 2;

        private InputActionAsset m_InputActions;
        private InputAction m_MovementAction;
        private InputAction m_JumpAction;
        private InputAction m_GrabAction;
        private InputAction m_ThrowAction;

        private Vector2 m_LastInput;

        private int m_Climbing_ReferenceFrame = 0;  // 1 or -1 depending on when we invert controls when on a wall


        private void Awake()
        {
            Colliders = new Collider[8];

            m_InputActions = Instantiate(m_InputAsset);
            List<InputDevice> devices = new List<InputDevice>();
            switch (m_Player)
            {
                case 0:
                    if (Keyboard.current != null)
                        devices.Add(Keyboard.current);
                    if (Mouse.current != null)
                        devices.Add(Mouse.current);
                    if (devices.Count > 0)
                    {
                        m_InputActions.devices = devices.ToArray();
                    }
                    else
                    {
                        enabled = false;
                        return;
                    }
                    break;
                case 1:
                    if (Gamepad.current != null)
                        devices.Add(Gamepad.current);
                    if (devices.Count > 0)
                    {
                        m_InputActions.devices = devices.ToArray();
                    }
                    else
                    {
                        enabled = false;
                        return;
                    }
                    break;
            }

            m_MovementAction    = m_InputActions.actionMaps[0].actions[0];
            m_JumpAction        = m_InputActions.actionMaps[0].actions[1];
            m_GrabAction        = m_InputActions.actionMaps[0].actions[2];
            m_ThrowAction       = m_InputActions.actionMaps[0].actions[3];
        }

        private void Start()
        {
            if (m_MovementAction == null)
            {
                return;
            }

            m_MovementAction.Enable();
            m_JumpAction.Enable();
            m_GrabAction.Enable();
            m_ThrowAction.Enable();
        }

        private void OnEnable()
        {
            m_Keyboard = InputSystem.GetDevice<Keyboard>();
            m_Mouse = InputSystem.GetDevice<Mouse>();
            m_Gamepad = InputSystem.GetDevice<Gamepad>();


            m_JumpAction.performed += JumpAction;
            m_GrabAction.performed += GrabAction;
        }
        private void OnDisable()
        {
            if (m_MovementAction == null)
            {
                return;
            }

            m_MovementAction.Disable();
            m_JumpAction.Disable();
            m_GrabAction.Disable();
            m_ThrowAction.Disable();

            m_JumpAction.performed -= JumpAction;
            m_GrabAction.performed -= GrabAction;
        }


        private void Update()
        {
            Vector2 rawInput = m_MovementAction.ReadValue<Vector2>();
            Vector3 inputVector = rawInput.XZtoXYZ();

            CharacterMovement.Move((inputVector + m_DebugMoveInput).ClampMagnitude(0, 1));

            if (m_ClimbAbility && m_ClimbAbility.isActiveAndEnabled)
            {
                Vector3 wallNormal = m_ClimbAbility.CurrentWallNormal;
                Vector3 verticalAxis = Vector3.up;
                Vector3 horizontalAxis = Vector3.Cross(wallNormal, verticalAxis);
                if (rawInput.x == 0)
                {
                    m_Climbing_ReferenceFrame = 0;
                }
                if ((rawInput.x == 0 && m_Climbing_ReferenceFrame == 0) || MMath.SignAsInt(m_LastInput.x) != MMath.SignAsInt(rawInput.x))
                {
                    m_Climbing_ReferenceFrame = MMath.SignAsInt(Vector3.Dot(horizontalAxis, Vector3.right));
                }
                //if (Vector3.Dot(horizontalAxis, inputVector) < 0)
                //{
                //    m_Climbing_ReferenceFrame = 1;
                //}
                horizontalAxis *= m_Climbing_ReferenceFrame;
                Vector3 move = rawInput.x * horizontalAxis + rawInput.y * verticalAxis;
                m_ClimbAbility.Move(move);
            }

            // TODO test if single button grab/throw input feels good
            //if (m_ThrowAction.WasPressedThisFrame())
            //{
            //    if (PullAbility.enabled)
            //    {
            //        PullAbility.Throw();
            //    }
            //}

            if (m_ClimbAbility && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (!m_ClimbAbility.enabled)
                {
                    m_ClimbAbility.enabled = true;
                }
                else
                {
                    m_ClimbAbility.enabled = false;
                }
            }

            m_LastInput = rawInput;
        }


        private void JumpAction(InputAction.CallbackContext ctx)
        {
            // TODO add jump buffering if not currently grounded
            if (ctx.ReadValue<float>() > 0.5f)
            {
                m_JumpAbility.Jump();
            }
            else
            {
                m_JumpAbility.ReleaseJump();
            }
        }
        private void GrabAction(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed && ctx.ReadValue<float>() < 0.5f)
            {
                return;
            }

            // TODO capsule cast to find nearest collider
            if (!PullAbility.enabled)
            {
                TriggerCollider.GetGlobalParams(out Vector3 p1, out Vector3 p2, out float radius);
                OverlapCount = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Colliders, m_GrabLayerMask);

                for (int i = 0; i < OverlapCount; i++)
                {
                    if (Colliders[i].gameObject == CharacterMovement.gameObject)
                        continue;
                    Rigidbody rigid = Colliders[i].attachedRigidbody;
                    if (rigid == null)
                        continue;
                    if (!rigid.GetComponent<GrabPreferences>())
                        continue;
                    PullAbility.Target = rigid;
                    PullAbility.enabled = true;
                    break;
                }
            }
            else
            {
                if (m_MovementAction.ReadValue<Vector2>().magnitude > 0.3f)
                {
                    PullAbility.Throw();
                }
                else
                {
                    PullAbility.Drop();
                }
            }
        }

        private float Smoothing01(float x, float n)
        {
            return MMath.Pow(x, n) / (MMath.Pow(x, n) + MMath.Pow(1.0f - x, n));
        }
    }
}
