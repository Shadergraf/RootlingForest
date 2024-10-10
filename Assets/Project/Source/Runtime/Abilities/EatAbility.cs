using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatAbility : MonoBehaviour
{
    [SerializeField]
    private GameObject m_Self;
    [SerializeField]
    private float m_AbilityTime = 0.8f;
    [SerializeField]
    private float m_EatTiming = 0.6f;
    [SerializeField]
    private GameplayEffect[] m_EffectsForDuration;

    public GameObject Target;

    private GameplayTagOwner m_SelfTagOwner;
    private GameplayEffectOwner m_SelfEffectOwner;
    private GameplayEffectOwner m_TargetEffectOwner;
    private EatPreferences m_TargetEatPrefs;
    private List<GameplayEffectInstance> m_Effects = new();


    private void OnEnable()
    {
        m_SelfTagOwner = m_Self.GetComponentInChildren<GameplayTagOwner>();
        m_SelfEffectOwner = m_Self.GetComponentInChildren<GameplayEffectOwner>();
        m_TargetEffectOwner = Target.GetComponentInChildren<GameplayEffectOwner>();
        var eatPrefList = Target.GetComponentsInChildren<EatPreferences>();

        for (int i = 0; i < eatPrefList.Length; i++)
        {
            var eatPref = eatPrefList[i];
            if ((!m_TargetEatPrefs || eatPref.Priority > m_TargetEatPrefs.Priority) && m_SelfTagOwner.SatisfiesTagFilter(eatPref.EaterRequirements))
            {
                m_TargetEatPrefs = eatPref;
            }
        }

        if (!m_TargetEatPrefs)
        {
            enabled = false;
            return;
        }

        StartCoroutine(ExecuteAbility());
    }
    private void OnDisable()
    {
        StopAllCoroutines();

        for (int i = 0; i < m_Effects.Count; i++)
        {
            m_SelfEffectOwner.RemoveEffect(m_Effects[i]);
        }

        m_TargetEatPrefs = null;
    }

    private IEnumerator ExecuteAbility()
    {
        for (int i = 0; i < m_EffectsForDuration.Length; i++)
        {
            var effectInst = m_SelfEffectOwner.AddEffect(m_EffectsForDuration[i]);
            if (effectInst != null)
                m_Effects.Add(effectInst);
        }

        yield return new WaitForSeconds(m_EatTiming);

        Bite();

        yield return new WaitForSeconds(m_AbilityTime - m_EatTiming);

        enabled = false;
    }

    private void Bite()
    {
        for (int i = 0; i < m_TargetEatPrefs.EaterEffects.Length; i++)
        {
            m_SelfEffectOwner.AddEffect(new GameplayEffectInstance(m_TargetEatPrefs.EaterEffects[i]));
        }
        for (int i = 0; i < m_TargetEatPrefs.SelfEffects.Length; i++)
        {
            m_TargetEffectOwner.AddEffect(new GameplayEffectInstance(m_TargetEatPrefs.SelfEffects[i]));
        }
    }
}
