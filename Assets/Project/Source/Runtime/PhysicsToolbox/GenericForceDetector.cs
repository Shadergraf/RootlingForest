using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Manatea.GameplaySystem;

namespace Manatea.RootlingForest
{
    public class GenericForceDetector : BaseForceDetector
    {
        [SerializeField]
        private Optional<GameplayEffectOwner> m_GameplayEffectOwner;
        [SerializeField]
        private GameplayEffect[] m_EffectsToApply;


        protected override void Awake()
        {
            base.Awake();

            if (!m_GameplayEffectOwner.hasValue)
            {
                m_GameplayEffectOwner.value = GetComponentInParent<GameplayEffectOwner>();
            }
        }

        protected override void ForceDetected(Vector3 force)
        {
            for (int i = 0; i < m_EffectsToApply.Length; i++)
            {
                m_GameplayEffectOwner.value.AddEffect(m_EffectsToApply[i]);
            }
        }
    }
}
