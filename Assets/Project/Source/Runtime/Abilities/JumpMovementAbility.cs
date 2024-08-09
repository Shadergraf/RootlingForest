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
    [SerializeField]
    private float m_JumpForce = 5;
    [SerializeField]
    private float m_JumpMoveAlignment = 0.75f;
    [SerializeField]
    private bool m_AllowGroundJump = true;
    [SerializeField]
    private bool m_AllowAirJump;
    [SerializeField]
    private int m_JumpIterations = 3;
    [SerializeField]
    private Optional<float> m_JumpCooldownTime = new Optional<float>(0.3f, true);

    /// <summary>
    /// The minimum time after a jump we are guaranteed to be airborne
    /// </summary>
    public const float MIN_JUMP_TIME = 0.2f;
    /// <summary>
    /// The amount of time after dropping off a cliff we are still allowed to jump
    /// </summary>
    public const float COYOTE_TIME = 0.1f;

    private bool m_ScheduledJump;
    private bool m_HasJumped;
    private float m_JumpTimer;
    private float m_ForceAirborneTimer;
    private float m_JumpCooldownTimer;


    private void OnEnable()
    {
        m_ScheduledJump = false;
        m_HasJumped = false;
        m_JumpTimer = 0;
        m_JumpCooldownTimer = 0;
        m_ForceAirborneTimer = 0;

        m_CharacterMovement.RegisterMover(this);
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

    public void ModifyState(MovementSimulationState sim)
    {
        // Guarantee airborne when jumping just occured
        if (m_HasJumped && m_JumpTimer <= MIN_JUMP_TIME)
        {
            sim.IsStableGrounded = false;
            sim.IsSliding = false;
        }
        // Stop jump
        if (sim.IsStableGrounded)
        {
            m_HasJumped = false;
            m_JumpTimer = 0;
        }

        sim.IsStableGrounded &= m_ForceAirborneTimer <= 0;
    }

    public void UpdateTimers(MovementSimulationState sim)
    {
        m_ForceAirborneTimer = MMath.Max(m_ForceAirborneTimer - Time.fixedDeltaTime, 0);
        if (m_HasJumped)
        {
            m_JumpTimer += Time.fixedDeltaTime;
        }

        m_JumpCooldownTimer = MMath.Max(m_JumpCooldownTimer - Time.fixedDeltaTime, 0);
    }

    public void PreMovement(MovementSimulationState sim)
    {
        // No jump input
        if (!m_ScheduledJump)
            return;
        if (m_JumpCooldownTime.hasValue && m_JumpCooldownTimer > 0)
            return;

        bool validGroundJump = m_AllowGroundJump && sim.AirborneTimer <= COYOTE_TIME;
        bool validAirJump = m_AllowAirJump && !sim.IsStableGrounded;
        if (!validGroundJump && !validAirJump)
            return;

        // Ground jump has precedence
        if (validGroundJump && validAirJump)
            validAirJump = false;

        m_ScheduledJump = false;
        m_ForceAirborneTimer = 0.05f;

        if (sim.ContactMove != Vector3.zero)
        {
            Vector3 initialDir = sim.Movement.Rigidbody.velocity;
            Vector3 targetDir = sim.ContactMove.FlattenY().WithMagnitude(initialDir.FlattenY().magnitude) + Vector3.up * initialDir.y;
            sim.Movement.Rigidbody.velocity = Vector3.Slerp(initialDir, targetDir, sim.ContactMove.magnitude * m_JumpMoveAlignment);
        }

        Vector3 jumpDir = -Physics.gravity.normalized;
        // TODO add a sliding jump here that is perpendicular to the slide normal
        sim.Movement.Rigidbody.velocity = Vector3.ProjectOnPlane(sim.Movement.Rigidbody.velocity, jumpDir);
        Vector3 jumpForce = jumpDir * m_JumpForce;
        StartCoroutine(CO_Jump(sim, jumpForce, m_JumpIterations));

        m_JumpCooldownTimer = m_JumpCooldownTime.value;
        m_HasJumped = true;
    }

    private IEnumerator CO_Jump(MovementSimulationState sim, Vector3 velocity, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            sim.Movement.Rigidbody.AddForce(velocity / iterations, ForceMode.Impulse);

            for (int j = 0; j < sim.GroundColliderCount; j++)
            {
                if (sim.GroundColliders[j] && sim.GroundColliders[j].attachedRigidbody)
                {
                    // TODO causes very extreme jumping push to objects. Can be tested by jumping off certain items
                    if (sim.GroundColliders[j].attachedRigidbody && !sim.GroundColliders[j].attachedRigidbody.isKinematic)
                    {
                        sim.GroundColliders[j].attachedRigidbody.AddForceAtPosition(-velocity / iterations, sim.Movement.FeetPos, ForceMode.VelocityChange);
                    }
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }
}
