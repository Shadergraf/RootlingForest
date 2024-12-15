using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static Manatea.RootlingForest.CharacterMovement;

namespace Manatea.RootlingForest
{
    [DefaultExecutionOrder(-10)]
    public class FlightMovementAbility : BaseAbility, ICharacterMover
    {
        [SerializeField]
        private CharacterMovement m_CharacterMovement;

        [Space]
        [SerializeField]
        private float m_FlightForce = 5;
        [SerializeField]
        private float m_GravityDefyingForce = 0;
        [SerializeField]
        private float m_LinearDrag = 0;
        [SerializeField]
        private float m_AngularDrag = 0;



        protected override void AbilityEnabled()
        {
            m_CharacterMovement.RegisterMover(this);
        }
        protected override void AbilityDisabled()
        {
            m_CharacterMovement.UnregisterMover(this);
        }


        void ICharacterMover.ModifyState(MovementSimulationState sim, float dt)
        {
            if (!enabled)
                return;
        }

        void ICharacterMover.UpdateTimers(MovementSimulationState sim, float dt)
        {
            if (!enabled)
                return;
        }

        void ICharacterMover.PreMovement(MovementSimulationState sim, float dt)
        {
            if (!enabled)
                return;

            sim.Movement.Rigidbody.AddForceAtPosition(Physics.gravity * -m_GravityDefyingForce - Physics.gravity.normalized * m_FlightForce, sim.Movement.HeadPos, ForceMode.Acceleration);

            sim.Movement.Rigidbody.linearVelocity *= Mathf.Clamp01(1 - m_LinearDrag * dt);
            sim.Movement.Rigidbody.angularVelocity *= MMath.Clamp01(1 - m_AngularDrag * dt);
        }
    }
}
