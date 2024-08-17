using Manatea.GameplaySystem;
using System;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [CreateAssetMenu(menuName = RootlingForest.AssetCreationPath + nameof(ForceDetectorConfig))]
    public class ForceDetectorConfig : ScriptableObject
    {
        [SerializeField]
        private float m_ImpulseMagnitude = 5;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_JerkInfluence = 1;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_ContactImpulseInfluence = 1;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_ContactVelocityInfluence = 1;
        [SerializeField]
        private float m_ImpulseTimeFalloff = 16;
        [SerializeField]
        private GameplayAttribute m_ForceDetectionMultiplier;
        [SerializeField]
        private GameplayAttribute m_HealthAttribute;
        [SerializeField]
        private GameplayTag m_ToughTag;
        [SerializeField]
        private GameplayTag m_SoftTag;

        public float ImpulseMagnitude => m_ImpulseMagnitude;
        public float JerkInfluence => m_JerkInfluence;
        public float ContactImpulseInfluence => m_ContactImpulseInfluence;
        public float ContactVelocityInfluence => m_ContactVelocityInfluence;
        public float ImpulseTimeFalloff => m_ImpulseTimeFalloff;
        public GameplayAttribute ForceDetectionMultiplier => m_ForceDetectionMultiplier;
        public GameplayAttribute HealthAttribute => m_HealthAttribute;
        public GameplayTag ToughTag => m_ToughTag;
        public GameplayTag SoftTag => m_SoftTag;
    }
}
