using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [CreateAssetMenu(menuName = GameplaySystemGlobals.AssetCreationPath + "Effect")]
    public class GameplayEffect : GameplayAsset
    {
        [SerializeField]
        private EffectLifetime m_Lifetime;
        [SerializeField]
        private float m_Duration;

        [Space]
        [SerializeField]
        [Tooltip("The tags to add to the effect owner. These will be added as unmanaged tags if the lifetime is instant and as managed tags otherwise.")]
        private List<GameplayTag> m_TagsToAdd;
        [SerializeField]
        private GameplayAttributeModification[] m_AttributeModifications;


        public EffectLifetime Lifetime => m_Lifetime;
        public float Duration => m_Duration;
        public List<GameplayTag> TagsToAdd => m_TagsToAdd;
        public GameplayAttributeModification[] GameplayAttributeModifications => m_AttributeModifications;
    }

    public enum EffectLifetime
    {
        Infinite = 0,
        Instant = 1,
        Duration = 2,
    }
}
