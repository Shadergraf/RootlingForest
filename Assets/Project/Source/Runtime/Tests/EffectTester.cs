using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectTester : MonoBehaviour
{
    [SerializeField]
    private GameplayEffectOwner m_EffectOwner;
    [SerializeField]
    private GameplayEffect m_Effect;

    private GameplayEffectInstance m_EffectInst;


    private void OnEnable()
    {
        m_EffectInst = new GameplayEffectInstance(m_Effect);
        m_EffectOwner.AddEffect(m_EffectInst);
    }

    private void OnDisable()
    {
        m_EffectOwner.RemoveEffect(m_EffectInst);
    }
}
