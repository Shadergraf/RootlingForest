using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class GameplayEffectAdder : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayEffectOwner> m_EffectsOwner = new(FetchingType.InParents);
        [SerializeField]
        private List<GameplayEffect> m_Effects;

        private List<GameplayEffectInstance> m_EffectInstances = new();


        private void Awake()
        {
            m_EffectsOwner.FetchFrom(gameObject);
        }

        private void OnEnable()
        {
            for (int i = 0; i < m_Effects.Count; i++)
            {
                m_EffectInstances.Add(m_EffectsOwner.value.AddEffect(m_Effects[i]));
            }
        }
        private void OnDisable()
        {
            for (int i = 0; i < m_EffectInstances.Count; i++)
            {
                m_EffectsOwner.value.RemoveEffect(m_EffectInstances[i]);
            }
            m_EffectInstances.Clear();
        }
    }
}
