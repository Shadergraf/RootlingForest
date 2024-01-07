using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : CharacterController
{
    public float DashForce;

    private Keyboard keeb;
    private Mouse mickey;

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
