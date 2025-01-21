using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class GameplayEffectPeriodicAdder : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayEffectOwner> m_EffectsOwner = new(FetchingType.InParents);
        [SerializeField]
        private List<GameplayEffect> m_Effects;
        [SerializeField]
        [Min(0f)]
        private float m_Period = float.PositiveInfinity;
        [SerializeField]
        private PeriodicTriggerMode m_TriggerMode;


        private void Awake()
        {
            m_EffectsOwner.FetchFrom(gameObject);
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
