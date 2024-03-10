using UnityEditor;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class GameplayAsset : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>
        /// Message describing the usage of this GameplayAsset
        /// </summary>
        /// <remarks>This field is only available in the editor</remarks>
        [Tooltip("Message describing the usage of this GameplayAsset")]
        [TextArea(3, 3)]
        [SerializeField] private string m_Description;
#endif
    }
}