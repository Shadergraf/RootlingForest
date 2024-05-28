using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatea
{
    public class PlayerController : CharacterController
    {
        public float DashForce;
        public PullAbility PullAbility;
        public CapsuleCollider TriggerCollider;

        public InputActionAsset m_InputAsset;
        public int m_Player = -1;

        public LayerMask m_GrabLayerMask;

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
        }


        private void Update()
        {

            Vector3 inputVector = Vector3.zero;

            inputVector = m_MovementAction.ReadValue<Vector2>().XZtoXYZ();

            CharacterMovement.Move(inputVector);


            if (m_JumpAction.IsPressed())
            {
                CharacterMovement.Jump();
            }
            else
            {
                CharacterMovement.ReleaseJump();
            }


            TriggerCollider.GetGlobalParams(out Vector3 p1, out Vector3 p2, out float radius);
            OverlapCount = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Colliders, m_GrabLayerMask);
            // TODO capsule cast to find nearest collider
            if (m_GrabAction.WasPressedThisFrame())
            {
                if (!PullAbility.enabled)
                {
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
            // TODO test if single button grab/throw input feels good
            //if (m_ThrowAction.WasPressedThisFrame())
            //{
            //    if (PullAbility.enabled)
            //    {
            //        PullAbility.Throw();
            //    }
            //}
        }


        private float Smoothing01(float x, float n)
        {
            return MMath.Pow(x, n) / (MMath.Pow(x, n) + MMath.Pow(1.0f - x, n));
        }
    }
}
