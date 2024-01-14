using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : CharacterController
{
    public float DashForce;
    public PullAbility PullAbility;
    public CapsuleCollider TriggerCollider;

    private Collider[] Colliders;
    private int OverlapCount;

    private Keyboard keeb;
    private Mouse mickey;


    private void Awake()
    {
        Colliders = new Collider[8];
    }

    private void OnEnable()
    {
        keeb = InputSystem.GetDevice<Keyboard>();
        mickey = InputSystem.GetDevice<Mouse>();
    }


    private void Update()
    {

        Vector3 inputVector = Vector3.zero;

        if (keeb.dKey.isPressed)
            inputVector += Vector3.right;
        if (keeb.aKey.isPressed)
            inputVector += Vector3.left;
        if (keeb.wKey.isPressed)
            inputVector += Vector3.forward;
        if (keeb.sKey.isPressed)
            inputVector += Vector3.back;
        if (inputVector != Vector3.zero)
            inputVector.Normalize();

        CharacterMovement.Move(inputVector);

        if (keeb.spaceKey.isPressed)
            CharacterMovement.Jump();
        else
            CharacterMovement.ReleaseJump();

        if (keeb.eKey.wasPressedThisFrame)
            CharacterMovement.GetComponent<Rigidbody>().AddForce(inputVector * DashForce, ForceMode.Impulse);


        TriggerCollider.GetGlobalParams(out Vector3 p1, out Vector3 p2, out float radius);
        OverlapCount = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Colliders);
        if (mickey.leftButton.wasPressedThisFrame)
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

#if DEBUG
        if (keeb.ctrlKey.isPressed && mickey.rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(mickey.position.value);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                transform.GetComponent<Rigidbody>().position = hit.point + hit.normal;
            }
        }
#endif
    }
}
