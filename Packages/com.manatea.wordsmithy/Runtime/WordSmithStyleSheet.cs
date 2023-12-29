using System;
using UnityEditor;
using UnityEngine;
using Manatea.WordSmithy.Animations;

namespace Manatea.WordSmithy
{
    [CreateAssetMenu(menuName = "Manatea/Word Smithy/Style Sheet")]
    public class WordSmithStyleSheet : ScriptableObject
    {
        [SerializeField]
        private WordSmithReplacement[] m_Replacements;
        [SerializeField]
        private WordSmithPreset[] m_Presets;
        [SerializeField]
        private WordSmithAnimation[] m_Animations;

        public WordSmithReplacement[] Replacements => m_Replacements;
        public WordSmithPreset[] Presets => m_Presets;
        public WordSmithAnimation[] Animations => m_Animations;
    }

    [Serializable]
    public struct WordSmithReplacement
    {
        public string Name;
        public string Replacement;
    }
    [Serializable]
    public struct WordSmithPreset
    {
        public string Name;
        [TextArea(2, 2)]
        public string OpeningTags;
        [TextArea(2, 2)]
        public string ClosingTags;
    }
    [Serializable]
    public struct WordSmithAnimation
    {
        public string Name;
        public TextAnimation Animation;
    }
}