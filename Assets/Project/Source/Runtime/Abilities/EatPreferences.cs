using Manatea.GameplaySystem;
using Manatea.RootlingForest.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatPreferences : GameplayFeaturePreference
{
    [SerializeField]
    private GameplayTagFilter m_EaterRequirements;
    [SerializeField]
    private AbilityPriority m_Priority = AbilityPriority.Default;

    [Space]
    [SerializeField]
    private GameplayEffect[] m_EaterEffects;
    [SerializeField]
    private GameplayEffect[] m_SelfEffects;

    public AbilityPriority Priority => m_Priority;
    public GameplayTagFilter EaterRequirements => m_EaterRequirements;
    public GameplayEffect[] EaterEffects => m_EaterEffects;
    public GameplayEffect[] SelfEffects => m_SelfEffects;
}
