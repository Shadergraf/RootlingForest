using Manatea;
using Manatea.AdventureRoots;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static Manatea.AdventureRoots.CharacterMovement;

public class JumpMovementAbility : MonoBehaviour, ICharacterMover
{
    [SerializeField]
    private CharacterMovement m_CharacterMovement;

    [Space]
    [FormerlySerializedAs("JumpForce")]
    [SerializeField]
    private float m_JumpForce = 5;
    [FormerlySerializedAs("JumpMoveAlignment")]
    [SerializeField]
    private float m_JumpMoveAlignment = 0.75f;

    /// <summary>
    /// The minimum time after a jump we are guaranteed to be airborne
    /// </summary>
    public const float MIN_JUMP_TIME = 0.2f;

    private bool m_ScheduledJump;
    private bool m_HasJumped;
    private float m_JumpTimer;


    private void OnEnable()
    {
        m_CharacterMovement.RegisterMover(this);
        m_ScheduledJump = false;
        m_HasJumped = false;
        m_JumpTimer = 0;
    }
    private void OnDisable()
    {
        m_CharacterMovement.UnregisterMover(this);
    }

    public void Jump()
    {
        m_ScheduledJump = true;
    }
    public void ReleaseJump()
    {
        m_ScheduledJump = false;
    }

    public void PreMovement(MovementSimulationState sim)
    {
        // TODO move this into a separate state modifier function that every CharacterMover should have
        // Guarantee airborne when jumping just occured
        if (m_HasJumped && m_JumpTimer <= MIN_JUMP_TIME)
        {
            sim.m_IsStableGrounded = false;
            sim.m_IsSliding = false;
        }
        if (sim.m_IsStableGrounded)
        {
            m_HasJumped = false;
            m_JumpTimer = 0;
        }


        if (m_HasJumped)
        {
            m_JumpTimer += Time.fixedDeltaTime;
        }



        // Jump
        if (m_ScheduledJump && !m_HasJumped && sim.m_AirborneTimer < 0.1f)
        {
            m_ScheduledJump = false;
            sim.m_ForceAirborneTimer = 0.05f;

            if (sim.m_ContactMove != Vector3.zero)
            {
                Vector3 initialDir = sim.Movement.Rigidbody.velocity;
                Vector3 targetDir = sim.m_ContactMove.FlattenY().WithMagnitude(initialDir.FlattenY().magnitude) + Vector3.up * initialDir.y;
                sim.Movement.Rigidbody.velocity = Vector3.Slerp(initialDir, targetDir, sim.m_ContactMove.magnitude * m_JumpMoveAlignment);
            }

            Vector3 jumpDir = -Physics.gravity.normalized;
            // TODO add a sliding jump here that is perpendicular to the slide normal
            sim.Movement.Rigidbody.velocity = Vector3.ProjectOnPlane(sim.Movement.Rigidbody.velocity, jumpDir);
            Vector3 jumpForce = jumpDir * m_JumpForce;
            StartCoroutine(CO_Jump(sim, jumpForce, 3));

            m_HasJumped = true;
        }
    }

    private IEnumerator CO_Jump(MovementSimulationState sim, Vector3 velocity, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            sim.Movement.Rigidbody.AddForce(velocity / iterations, ForceMode.Impulse);

            // TODO reenable this
            /*
            for (int j = 0; j < m_GroundColliderCount; j++)
            {
                if (m_GroundColliders[j] && m_GroundColliders[j].attachedRigidbody)
                {
                    // TODO very extreme jumping push to objects. Can be tested by jumping off certain items
                    if (m_GroundColliders[j].attachedRigidbody && !m_GroundColliders[j].attachedRigidbody.isKinematic)
                    {
                        m_GroundColliders[j].attachedRigidbody.AddForceAtPosition(-velocity / iterations, FeetPos, ForceMode.VelocityChange);
                    }
                }
            }
            */

            yield return new WaitForFixedUpdate();
        }
    }
}
