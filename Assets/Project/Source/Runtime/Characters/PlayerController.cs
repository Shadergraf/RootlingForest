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

        public InputActionReference m_MovementAction;

        private Collider[] Colliders;
        private int OverlapCount;

        private Keyboard m_Keyboard;
        private Mouse m_Mouse;
        private Gamepad m_Gamepad;

        public float InputSmoothing = 2;


        private void Awake()
        {
            Colliders = new Collider[8];
        }

        private void Start()
        {
            m_MovementAction.action.Enable();
        }
        private void OnEnable()
        {
            m_Keyboard = InputSystem.GetDevice<Keyboard>();
            m_Mouse = InputSystem.GetDevice<Mouse>();
            m_Gamepad = InputSystem.GetDevice<Gamepad>();

        }
        private void OnDisable()
        {
            m_MovementAction.action.Disable();
        }


        private void Update()
        {

            Vector3 inputVector = Vector3.zero;

            // Keyboard
            if (m_Keyboard.dKey.isPressed)
                inputVector += Vector3.right;
            if (m_Keyboard.aKey.isPressed)
                inputVector += Vector3.left;
            if (m_Keyboard.wKey.isPressed)
                inputVector += Vector3.forward;
            if (m_Keyboard.sKey.isPressed)
                inputVector += Vector3.back;

            // Gamepad
            if (m_Gamepad.leftStick.value != Vector2.zero)
            {
                Vector2 stickInput          = m_Gamepad.leftStick.value;
                Vector2 stickDir            = stickInput.normalized;
                float   stickDirAngle       = MMath.DirToAng(stickDir) * MMath.Rad2Deg / 45;
                float   stickDirAngleFrac   = MMath.Frac(stickDirAngle);
                int     stickDirAngleFloor  = MMath.FloorToInt(stickDirAngle);
                stickDirAngleFrac = Smoothing01(stickDirAngleFrac, InputSmoothing);
                stickDirAngle = (stickDirAngleFloor + stickDirAngleFrac) * 45 * MMath.Deg2Rad;
                stickDir = MMath.AngToDir(stickDirAngle);
                stickInput = stickDir * stickInput.magnitude;

                inputVector += Vector3.right * stickInput.x;
                inputVector += Vector3.forward * stickInput.y;
            }

            if (inputVector != Vector3.zero)
                inputVector.Normalize();

            inputVector = m_MovementAction.action.ReadValue<Vector2>().XZtoXYZ();

            CharacterMovement.Move(inputVector);


            if (m_Keyboard.spaceKey.isPressed || m_Gamepad.buttonSouth.isPressed)
            {
                CharacterMovement.Jump();
            }
            else
            {
                CharacterMovement.ReleaseJump();
            }


            if (m_Keyboard.eKey.wasPressedThisFrame || m_Gamepad.buttonNorth.wasPressedThisFrame)
                CharacterMovement.GetComponent<Rigidbody>().AddForce(inputVector * DashForce, ForceMode.Impulse);


            TriggerCollider.GetGlobalParams(out Vector3 p1, out Vector3 p2, out float radius);
            OverlapCount = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Colliders);
            if (m_Mouse.leftButton.wasPressedThisFrame || m_Gamepad.buttonWest.wasPressedThisFrame)
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
                        PullAbility.Target = rigid;
                        PullAbility.enabled = true;
                    }
                }
                else
                {
                    PullAbility.enabled = false;
                }
            }

            if (m_Mouse.rightButton.wasPressedThisFrame || m_Gamepad.buttonEast.wasPressedThisFrame)
            {
                if (PullAbility.enabled)
                {
                    PullAbility.Throw();
                }
            }

    #if DEBUG
            if (m_Keyboard.ctrlKey.isPressed && m_Mouse.rightButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(m_Mouse.position.value);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    transform.GetComponent<Rigidbody>().position = hit.point + hit.normal;
                }
            }
    #endif
        }


        private float Smoothing01(float x, float n)
        {
            return MMath.Pow(x, n) / (MMath.Pow(x, n) + MMath.Pow(1.0f - x, n));
        }
    }
}
