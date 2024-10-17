using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Manatea.GameplaySystem;

namespace Manatea.RootlingForest
{
    public class GenericForceDetector : BaseForceDetector
    {
        [SerializeField]
        private GameplayEffectOwner m_GameplayEffectOwner;
        [SerializeField]
        private GameplayEffect[] m_EffectsToApply;


        protected override void ForceDetected(Vector3 force)
        {
            for (int i = 0; i < m_EffectsToApply.Length; i++)
            {
                m_GameplayEffectOwner.AddEffect(m_EffectsToApply[i]);
            }
        }
    }
}
