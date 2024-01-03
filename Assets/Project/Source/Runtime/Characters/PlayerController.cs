using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : CharacterController
{
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
    }
}
