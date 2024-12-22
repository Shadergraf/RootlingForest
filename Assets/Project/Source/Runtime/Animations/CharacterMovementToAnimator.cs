using Manatea.RootlingForest;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.GameplaySystem
{
    public class CharacterMovementToAnimator : MonoBehaviour
    {
        [SerializeField]
        private CharacterMovement m_CharacterMovement;
        [SerializeField]
        private Animator m_Animator;
        [SerializeField]
        private AnimatorHash m_SpeedParameter;


        private void Update()
        {
            m_Animator.SetFloat(m_SpeedParameter, m_CharacterMovement.Rigidbody.linearVelocity.magnitude);
        }
    }
}
