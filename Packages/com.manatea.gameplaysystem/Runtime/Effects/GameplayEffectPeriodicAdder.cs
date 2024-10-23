using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class GameplayEffectPeriodicAdder : MonoBehaviour
    {
        [SerializeField]
        private Optional<GameplayEffectOwner> m_EffectsOwner;
        [SerializeField]
        private List<GameplayEffect> m_Effects;
        [SerializeField]
        [Min(0f)]
        private float m_Period = float.PositiveInfinity;
        [SerializeField]
        private PeriodicTriggerMode m_TriggerMode;


        private void Awake()
        {
            if (!m_EffectsOwner.hasValue)
            {
                m_EffectsOwner.value = GetComponentInParent<GameplayEffectOwner>();
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Loop());
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                if (m_TriggerMode == PeriodicTriggerMode.Immediate)
                    Trigger();

                yield return new WaitForSeconds(m_Period);

                if (m_TriggerMode == PeriodicTriggerMode.Late)
                    Trigger();
            }
        }

        private void Trigger()
        {
            for (int i = 0; i < m_Effects.Count; i++)
            {
                m_EffectsOwner.value.AddEffect(m_Effects[i]);
            }
        }
    }

    public enum PeriodicTriggerMode
    {
        Immediate = 0,
        Late = 1,
    }
}
