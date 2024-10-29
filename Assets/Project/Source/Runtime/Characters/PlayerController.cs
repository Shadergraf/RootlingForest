using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    public class PlayerController : CharacterController
    {
        [SerializeField]
        private JumpMovementAbility m_JumpAbility;
        [SerializeField]
        [FormerlySerializedAs("PullAbility")]
        private GrabAbility m_GrabAbility;
        [SerializeField]
        [FormerlySerializedAs("TriggerCollider")]
        private CapsuleCollider m_TriggerCollider;
        [SerializeField]
        private ClimbAbility m_ClimbAbility;
        [SerializeField]
        private EatAbility m_EatAbility;
        [SerializeField]
        private AccessInventoryAbility m_InventoryAbility;

        [SerializeField]
        private InputActionAsset m_InputAsset;
        [SerializeField]
        private int m_Player = -1;

        [SerializeField]
        private LayerMask m_GrabLayerMask;

        [Header("Debug")]
        [SerializeField]
        private Vector3 m_DebugMoveInput;

        private Collider[] Colliders;
        private int OverlapCount;

        private InputActionAsset m_InputActions;
        private InputAction m_MovementAction;
        private InputAction m_JumpAction;
        private InputAction m_GrabAction;
        private InputAction m_EatAction;
        private InputAction m_InventoryAction;

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
            m_EatAction         = m_InputActions.actionMaps[0].actions[3];
            m_InventoryAction   = m_InputActions.actionMaps[0].actions[4];
        }

        private void Start()
        {
            if (m_MovementAction == null)
            {
                return;
            }

            m_JumpAction.performed += JumpAction;
            m_GrabAction.performed += GrabAction;
            m_EatAction.performed += EatAction;
            m_InventoryAction.performed += AccessInventoryAction;
        }

        private void OnEnable()
        {
            m_MovementAction.Enable();
            m_JumpAction.Enable();
            m_GrabAction.Enable();
            m_EatAction.Enable();
            m_InventoryAction.Enable();
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
            m_EatAction.Disable();
            m_InventoryAction.Disable();
        }
        private void OnDestroy()
        {

            m_JumpAction.performed -= JumpAction;
            m_GrabAction.performed -= GrabAction;
            m_EatAction.performed -= EatAction;
            m_InventoryAction.performed -= AccessInventoryAction;
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
            if (!m_GrabAbility.enabled)
            {
                m_TriggerCollider.GetGlobalParams(out Vector3 p1, out Vector3 p2, out float radius);
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
                    m_GrabAbility.Target = rigid;
                    m_GrabAbility.enabled = true;
                    break;
                }
            }
            else
            {
                if (m_InventoryAbility.enabled && m_GrabAbility.enabled)
                {
                    GameObject item = m_GrabAbility.Target.gameObject;
                    if (m_InventoryAbility.Inventory.CouldAddItem(item))
                    {
                        m_GrabAbility.enabled = false;
                        m_InventoryAbility.Inventory.AddItem(item);
                    }
                    else
                    {
                        m_GrabAbility.Drop();
                    }
                }
                else
                {
                    if (m_MovementAction.ReadValue<Vector2>().magnitude > 0.3f)
                    {
                        m_GrabAbility.Throw();
                    }
                    else
                    {
                        m_GrabAbility.Drop();
                    }
                }
            }
        }
        private void EatAction(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed && ctx.ReadValue<float>() < 0.5f)
                return;

            if (!m_GrabAbility.Target)
                return;

            var eatPrefs = m_GrabAbility.Target.GetComponentInChildren<EatPreferences>();
            if (!eatPrefs)
                return;

            m_EatAbility.Target = m_GrabAbility.Target.gameObject;
            m_EatAbility.enabled = true;
        }

        private void AccessInventoryAction(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed && ctx.ReadValue<float>() < 0.5f)
                return;

            if (!m_InventoryAbility)
                return;

            if (m_InventoryAbility.enabled)
            {
                m_InventoryAbility.enabled = false;
            }
            else
            {
                m_InventoryAbility.enabled = true;
            }
        }
    }
}
