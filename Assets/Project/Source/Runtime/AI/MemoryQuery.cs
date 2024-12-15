using Manatea.GameplaySystem;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [CreateAssetMenu(fileName = "MemoryQuery", menuName = RootlingForest.AssetCreationPath + "AI/MemoryQuery")]
    public class MemoryQuery : ScriptableObject
    {
        [SerializeField]
        private GameplayTagFilter m_TagFilter;


        public GameplayTagFilter TagFilter => m_TagFilter;
    }
}
