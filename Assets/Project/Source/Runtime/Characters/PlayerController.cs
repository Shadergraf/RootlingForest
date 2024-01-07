using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : CharacterController
{
    public float DashForce;

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

        CharacterMovement.Move(inputVector);

        if (InputSystem.GetDevice<Keyboard>().spaceKey.isPressed)
            CharacterMovement.Jump();
        else
            CharacterMovement.ReleaseJump();

        if (InputSystem.GetDevice<Keyboard>().eKey.wasPressedThisFrame)
            CharacterMovement.GetComponent<Rigidbody>().AddForce(inputVector * DashForce, ForceMode.Impulse);


#if DEBUG
        if (InputSystem.GetDevice<Keyboard>().ctrlKey.isPressed && InputSystem.GetDevice<Mouse>().rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(InputSystem.GetDevice<Mouse>().position.value);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                transform.GetComponent<Rigidbody>().position = hit.point + hit.normal;
            }
        }
#endif
    }
}
