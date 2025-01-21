using Manatea;
using Manatea.GameplaySystem;
using Manatea.RootlingForest;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeAbility : BaseAbility
{
    [SerializeField]
    private Fetched<GameplayEffectOwner> m_EffectOwner = new(FetchingType.InParents);

    [SerializeField]
    private GrabAbility m_GrabAbility;
    [SerializeField]
    private float m_Speed = 10;
    [SerializeField]
    private float m_Amount = 0.01f;

    [SerializeField]
    private GameplayEffect[] m_GrabberEffects;
    [SerializeField]
    private GameplayEffect[] m_GrabbedObjectEffects;

    private float m_Time;

    private List<GameplayEffectInstance> m_GrabberEffectInstances = new List<GameplayEffectInstance>();
    private GameplayEffectOwner m_TargetEffectOwner;
    private List<GameplayEffectInstance> m_GrabbedObjectEffectInstances = new List<GameplayEffectInstance>();


    private void Awake()
    {
        m_EffectOwner.FetchFrom(gameObject);
    }

    protected override void AbilityEnabled()
    {
        if (!m_GrabAbility.enabled)
        {
            enabled = false;
            return;
        }

        m_Time = 0;

        if (m_EffectOwner.value)
        {
            for (int i = 0; i < m_GrabberEffects.Length; i++)
            {
                m_GrabberEffectInstances.Add(m_EffectOwner.value.AddEffect(m_GrabberEffects[i]));
            }
        }

        if (m_GrabAbility.Target.TryGetComponent(out m_TargetEffectOwner))
        {
            for (int i = 0; i < m_GrabbedObjectEffects.Length; i++)
            {
                m_GrabbedObjectEffectInstances.Add(m_TargetEffectOwner.AddEffect(m_GrabbedObjectEffects[i]));
            }
        }
    }

    protected override void AbilityDisabled()
    {
        if (m_EffectOwner.value)
        {
            for (int i = 0; i < m_GrabberEffects.Length; i++)
            {
                m_EffectOwner.value.RemoveEffect(m_GrabberEffectInstances[i]);
            }
        }
        m_GrabberEffectInstances.Clear();

        if (m_TargetEffectOwner)
        {
            for (int i = 0; i < m_GrabbedObjectEffects.Length; i++)
            {
                m_TargetEffectOwner.RemoveEffect(m_GrabbedObjectEffectInstances[i]);
            }
        }
        m_TargetEffectOwner = null;
        m_GrabbedObjectEffectInstances.Clear();
    }

    private void FixedUpdate()
    {
        m_Time += Time.fixedDeltaTime;
        m_GrabAbility.Joint.anchor += Vector3.up * MMath.Sin(m_Time * m_Speed) * m_Amount;
    }
}
