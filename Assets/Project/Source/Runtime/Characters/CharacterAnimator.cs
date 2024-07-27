using Manatea;
using Manatea.AdventureRoots;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator m_Animator;
    [SerializeField]
    private CharacterMovement m_CharacterMovement;
    [SerializeField]
    private AnimatorHash m_MoveSpeedParam;

    private Vector3 m_CurrentMoveInput;

    private float m_Anim_MoveSpeed;


    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        m_CurrentMoveInput = m_CharacterMovement.ScheduledMove;
    }

    private void Update()
    {
        UpdateVariables();
        UpdateAnimator();
    }

    private void UpdateVariables()
    {
        Vector3 scheduledPreciseMove = m_CurrentMoveInput;
        m_Anim_MoveSpeed = MMath.Damp(m_Anim_MoveSpeed, scheduledPreciseMove.magnitude, 10, Time.deltaTime);
    }
    private void UpdateAnimator()
    {
        m_Animator.SetFloat(m_MoveSpeedParam, m_Anim_MoveSpeed);
    }
}
