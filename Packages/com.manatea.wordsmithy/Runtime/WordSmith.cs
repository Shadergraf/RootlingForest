using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Manatea.WordSmithy.Animations;

namespace Manatea.WordSmithy
{
    public enum WordSmithEnableBehavior
    {
        DoNothing,
        StartAnimation,
        ShowImmediately,
    }

    internal enum WordSmithAnimationState
    {
        Idle,
        Animating,
        Ended,
    }
    internal enum WordSmithTagType
    {
        Wait,
        ShowImmediate,
        AwaitContinue,
        DisableSkip,
        Speed,
        Animation,
        Event,
        AwaitTrigger,
    }
    [Serializable]
    internal struct WordSmithTextTag
    {
        public int CleanIndex;
        public int RichIndex;
        public WordSmithTagType Type;
        public object Data;
    }

    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(TextAnimator))]
    public class WordSmith : MonoBehaviour
    {
        [SerializeField]
        [TextArea(5, 5)]
        private string m_Text;
        [SerializeField]
        private float m_TextSpeed = 20;
        [SerializeField]
        private WordSmithEnableBehavior m_OnEnable = WordSmithEnableBehavior.ShowImmediately;
        [SerializeField]
        private WordSmithStyleSheet m_StyleSheet;

        public event Action OnAnimationStarted;
        public event TextEventDelegate OnTextEvent;
        public event CharacterDelegate OnCharacterEvent;
        public event Action OnAnimationEnded;

        public delegate void TextEventDelegate(string eventName);
        public delegate void CharacterDelegate(int index, TMP_CharacterInfo TMP_character);

        public string Text
        {
            get => m_Text;
            set
            {
                m_Text = value;
            }
        }

        public bool IsWaitingForAnimation => m_TextAnimationState == WordSmithAnimationState.Idle;
        public bool IsAnimating => m_TextAnimationState == WordSmithAnimationState.Animating;
        public bool IsAnimationFinished => m_TextAnimationState == WordSmithAnimationState.Ended;

        public bool IsWaitingForTrigger => string.IsNullOrEmpty(m_Anim_AwaitTrigger);
        public string CurrentAwaitTrigger => m_Anim_AwaitTrigger;

        private TMP_Text m_TMP_Text;
        private TextAnimator m_TextAnimator;

        // animation
        private WordSmithAnimationState m_TextAnimationState;
        private int m_Anim_CurrentCharId;
        private int m_Anim_CurrentTagId;
        private float m_Anim_Timer;
        private bool m_Anim_WantsContinue;
        private float m_Anim_TextSpeed;
        private float m_Anim_Wait;
        private bool m_Anim_ShowImmediate;
        private bool m_Anim_AwaitContinue;
        private string m_Anim_AwaitTrigger;

        // triggers
        private HashSet<string> m_PersistentTriggers = new HashSet<string>();
        private HashSet<string> m_FrameTriggers = new HashSet<string>();

        // parsing results
        private List<WordSmithTextTag> m_Parsed_Tags = new List<WordSmithTextTag>();
        private string m_Parsed_RawText;
        private string m_Parsed_WSText;
        private string m_Parsed_RichText;
        private string m_Parsed_CleanText;

        // variables used for parsing
        private StringBuilder m_SB_Temp = new StringBuilder();
        private StringBuilder m_SB_CleanText = new StringBuilder();
        private StringBuilder m_SB_RichText = new StringBuilder();
        private Stack<WordSmithPreset> m_TagStack_Presets = new Stack<WordSmithPreset>();
        private Stack<float> m_TagStack_TextSpeed = new Stack<float>();
        private Stack<TextAnimation> m_TagStack_Animation = new Stack<TextAnimation>();

        private bool Anim_EndReached => m_Anim_CurrentCharId >= m_Parsed_CleanText.Length - 1 && m_Anim_CurrentTagId >= m_Parsed_Tags.Count;


        private void Awake()
        {
            TryGetComponent(out m_TMP_Text);
            TryGetComponent(out m_TextAnimator);

            m_TextAnimationState = WordSmithAnimationState.Idle;
            m_Parsed_WSText = string.Empty;
            m_TMP_Text.maxVisibleCharacters = 0;
        }

        private void OnEnable()
        {
            Parse();

            switch (m_OnEnable)
            {
                case WordSmithEnableBehavior.DoNothing:
                    break;
                case WordSmithEnableBehavior.StartAnimation:
                    ResetText();
                    StartText();
                    break;
                case WordSmithEnableBehavior.ShowImmediately:
                    ShowAllText();
                    break;
            }
        }
        private void OnDisable()
        {
            ResetText();
            m_TMP_Text.maxVisibleCharacters = 0;
        }

        private void Update()
        {
            Parse();
            AnimateText();
        }


        public void StartText()
        {
            if (m_TextAnimationState != WordSmithAnimationState.Idle)
                return;

            m_TextAnimationState = WordSmithAnimationState.Animating;
            m_Anim_CurrentCharId = 0;
            m_Anim_CurrentTagId = 0;
            m_Anim_Timer = 0;

            m_Anim_WantsContinue = false;
            m_Anim_TextSpeed = 1;
            m_Anim_Wait = 0;
            m_Anim_ShowImmediate = false;
            m_Anim_AwaitContinue = false;
            m_Anim_AwaitTrigger = string.Empty;

            m_PersistentTriggers.Clear();
            m_FrameTriggers.Clear();
        }
        public void ResetText()
        {
            m_TextAnimationState = WordSmithAnimationState.Idle;
        }
        public void Continue()
        {
            m_Anim_WantsContinue = true;
        }
        public void ShowAllText()
        {
            m_TextAnimationState = WordSmithAnimationState.Ended;
        }

        public void SetTrigger(string trigger)
        {
            m_FrameTriggers.Add(trigger);
        }
        public void SetTrigger(string trigger, bool stayUntilConsumed = false)
        {
            if (stayUntilConsumed)
                m_PersistentTriggers.Add(trigger);
            else
                m_FrameTriggers.Add(trigger);
        }
        public bool HasTrigger(string trigger) => m_FrameTriggers.Contains(trigger) || m_PersistentTriggers.Contains(trigger);
        public void ClearTrigger(string trigger)
        {
            m_PersistentTriggers.Remove(trigger);
            m_FrameTriggers.Remove(trigger);
        }
        public void ClearAllTriggers()
        {
            m_PersistentTriggers.Clear();
            m_FrameTriggers.Clear();
        }

        private void AnimateText()
        {
            if (m_Anim_CurrentCharId == 0 && m_Anim_CurrentTagId == 0)
            {
                OnAnimationStarted?.Invoke();
            }

            switch (m_TextAnimationState)
            {
                case WordSmithAnimationState.Idle:
                    m_TMP_Text.maxVisibleCharacters = 0;
                    return;
                case WordSmithAnimationState.Ended:
                    m_TMP_Text.maxVisibleCharacters = m_Parsed_CleanText.Length;
                    return;
            }

            m_Anim_Timer += Time.deltaTime;

            // Await Continue
            if (m_Anim_AwaitContinue && m_Anim_WantsContinue)
            {
                m_Anim_AwaitContinue = false;
                m_Anim_WantsContinue = false;
                m_Anim_Timer = m_Anim_Wait;
            }
            // Await Trigger
            if (!string.IsNullOrEmpty(m_Anim_AwaitTrigger))
            {
                m_Anim_WantsContinue = false;
                if (HasTrigger(m_Anim_AwaitTrigger))
                {
                    ClearTrigger(m_Anim_AwaitTrigger);
                    m_Anim_AwaitTrigger = string.Empty;
                    m_Anim_Timer = m_Anim_Wait;
                }
            }
            m_FrameTriggers.Clear();

            while (m_TextAnimationState == WordSmithAnimationState.Animating && !m_Anim_AwaitContinue && string.IsNullOrEmpty(m_Anim_AwaitTrigger) && (m_Anim_Timer >= m_Anim_Wait || m_Anim_ShowImmediate || m_Anim_WantsContinue))
            {
                m_Anim_Timer -= m_Anim_Wait;
                AdvanceSingle();
            }

            if (m_TMP_Text.maxVisibleCharacters != m_Anim_CurrentCharId)
                m_TMP_Text.maxVisibleCharacters = m_Anim_CurrentCharId;
        }

        /// <summary>
        /// Advances a single unit of text. This can be a text character or a tag
        /// </summary>
        private void AdvanceSingle()
        {
            if (Anim_EndReached)
            {
                // End of animation reached
                m_Anim_WantsContinue = false;
                m_TextAnimationState = WordSmithAnimationState.Ended;
                OnAnimationEnded?.Invoke();
            }
            else if (m_Anim_CurrentTagId < m_Parsed_Tags.Count && m_Parsed_Tags[m_Anim_CurrentTagId].CleanIndex == m_Anim_CurrentCharId)
            {
                // Advance a tag
                switch (m_Parsed_Tags[m_Anim_CurrentTagId].Type)
                {
                    case WordSmithTagType.Wait:
                        m_Anim_Wait = (float)m_Parsed_Tags[m_Anim_CurrentTagId].Data;
                        break;
                    case WordSmithTagType.ShowImmediate:
                        m_Anim_ShowImmediate = (bool)m_Parsed_Tags[m_Anim_CurrentTagId].Data;
                        m_Anim_Timer = 0;
                        break;
                    case WordSmithTagType.Speed:
                        m_Anim_TextSpeed = (float)m_Parsed_Tags[m_Anim_CurrentTagId].Data;
                        break;
                    case WordSmithTagType.AwaitContinue:
                        m_Anim_WantsContinue = false;
                        m_Anim_AwaitContinue = true;
                        break;
                    case WordSmithTagType.Event:
                        OnTextEvent?.Invoke((string)m_Parsed_Tags[m_Anim_CurrentTagId].Data);
                        break;
                    case WordSmithTagType.AwaitTrigger:
                        m_Anim_WantsContinue = false;
                        m_Anim_AwaitTrigger = (string)m_Parsed_Tags[m_Anim_CurrentTagId].Data;
                        break;
                }
                m_Anim_CurrentTagId++;
            }
            else
            {
                // Advance a character
                m_Anim_CurrentCharId++;
                m_Anim_Wait = 1 / (m_TextSpeed * m_Anim_TextSpeed);
                if (m_Anim_CurrentCharId < m_TMP_Text.textInfo.characterInfo.Length)
                    OnCharacterEvent?.Invoke(m_Anim_CurrentCharId, m_TMP_Text.textInfo.characterInfo[m_Anim_CurrentCharId]);
            }
        }

        private void Parse()
        {
            string text = m_Text;
            if (text.Equals(m_Parsed_RawText))
                return;

            UnityEngine.Profiling.Profiler.BeginSample("WordSmith parser");

            m_Parsed_RawText = m_Text;

            m_SB_Temp.Clear();
            m_SB_CleanText.Clear();
            m_SB_RichText.Clear();
            m_Parsed_Tags.Clear();

            m_TagStack_Presets.Clear();
            m_TagStack_TextSpeed.Clear();
            m_TagStack_Animation.Clear();

            m_TextAnimator.AnimationSegments.Clear();

            string[] tagSplits;
            float floatParam = default;
            string stringParam = string.Empty;

            // TODO Parsing could be made more efficient

            // Parse escape characters
            while (text.Contains("\\n"))
                text = text.Replace("\\n", "\n");
            while (text.Contains("\\r"))
                text = text.Replace("\\r", "\r");
            while (text.Contains("\\t"))
                text = text.Replace("\\t", "\t");
            while (text.Contains("\\v"))
                text = text.Replace("\\v", "\v");
            while (text.Contains("\\\\"))
                text = text.Replace("\\\\", "\\");

            // Parse replacement
            for (int i = 0; i < m_StyleSheet.Replacements.Length; i++)
                while (text.Contains(m_StyleSheet.Replacements[i].Name))
                    text = text.Replace(m_StyleSheet.Replacements[i].Name, m_StyleSheet.Replacements[i].Replacement);

            // Preset parsing
            tagSplits = Regex.Split(text, "(<preset=[^<]*?>|</preset>)");
            for (int i = 0; i < tagSplits.Length; i++)
            {
                if (i % 2 == 0)
                {
                    m_SB_Temp.Append(tagSplits[i]);
                }
                else
                {
                    string fullTag = tagSplits[i];
                    if (fullTag.StartsWith("<preset=") && TryParseStringParameterFromFullTag(fullTag, out stringParam))
                    {
                        // Use for loop to avoid using a lambda expression that causes GC even when parsing is skipped
                        WordSmithPreset? newPreset = null;
                        for (int j = 0; j < m_StyleSheet.Presets.Length; j++)
                            if (m_StyleSheet.Presets[j].Name.Equals(stringParam))
                                newPreset = m_StyleSheet.Presets[j];
                        if (newPreset == null)
                            continue;
                        m_TagStack_Presets.Push(newPreset.Value);
                        m_SB_Temp.Append(newPreset.Value.OpeningTags);
                        continue;
                    }
                    if (fullTag.StartsWith("</preset>"))
                    {
                        if (m_TagStack_Presets.TryPop(out WordSmithPreset presetToClose))
                        {
                            m_SB_Temp.Append(presetToClose.ClosingTags);
                        }
                        continue;
                    }
                }
            }
            text = m_SB_Temp.ToString();
            m_SB_Temp.Clear();

            // Main parsing
            tagSplits = Regex.Split(text, "(<[^<]*?>)");
            // even id are text, odd ids are tags
            for (int i = 0; i < tagSplits.Length; i++)
            {
                if (i % 2 == 0)
                {
                    m_SB_CleanText.Append(tagSplits[i]);
                    m_SB_RichText.Append(tagSplits[i]);
                }
                else
                {
                    string fullTag = tagSplits[i];

                    #region Parse all tags

                    #region Rich text tags

                    // TODO Catching rich text correctly tags here is the only way the parsing works correctly.
                    //      We are not correctly catching *every* rich tag here however. That means if a tag
                    //      satisfies out logic here, but does not do so within the TMP text component, our tag
                    //      indices will be wrong and the resulting WordSmith text will be incorrect.
                    //      We need some way to figure out if the current tag is a *correct* rich TMP tag.

                    // Add rich text tags.
                    if (fullTag.Equals("<b>")         || fullTag.Equals("</b>") ||              // Bold
                        fullTag.Equals("<i>")         || fullTag.Equals("</i>") ||              // Italic
                        fullTag.Equals("<u>")         || fullTag.Equals("</u>") ||              // Underline
                        fullTag.Equals("<s>")         || fullTag.Equals("</s>") ||              // Strikethrough
                        fullTag.Equals("<sub>")       || fullTag.Equals("</sub>") ||            // Subscript
                        fullTag.Equals("<sup>")       || fullTag.Equals("</sup>") ||            // Superscript
                        fullTag.Equals("<lowercase>") || fullTag.Equals("</lowercase>") ||      // Lowercase
                        fullTag.Equals("<allcaps>")   || fullTag.Equals("</allcaps>") ||        // Allcaps
                        fullTag.Equals("<uppercase>") || fullTag.Equals("</uppercase>") ||      // Uppercase
                        fullTag.Equals("<smallcaps>") || fullTag.Equals("</smallcaps>") ||      // Smallcaps
                        fullTag.StartsWith("<color")  || fullTag.StartsWith("</color") ||       // Color
                        fullTag.StartsWith("<size")   || fullTag.StartsWith("</size") ||        // Size
                        false)
                    {
                        m_SB_RichText.Append(fullTag);
                        continue;
                    }

                    #endregion

                    #region Speed
                    if (fullTag.StartsWith("<speed=") && TryParseFloatParameterFromFullTag(fullTag, out floatParam))
                    {
                        m_TagStack_TextSpeed.Push(floatParam);
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.Speed,
                            Data = floatParam,
                        });
                        continue;
                    }
                    if (fullTag.StartsWith("</speed>") && m_TagStack_TextSpeed.TryPop(out _))
                    {
                        if (!m_TagStack_TextSpeed.TryPeek(out floatParam))
                            floatParam = 1;   // Default value
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.Speed,
                            Data = floatParam,
                        });
                        continue;
                    }
                    #endregion

                    #region Wait
                    if (fullTag.StartsWith("<wait=") && TryParseFloatParameterFromFullTag(fullTag, out floatParam))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.Wait,
                            Data = floatParam,
                        });
                        continue;
                    }
                    #endregion

                    #region Show Immediate
                    if (fullTag.StartsWith("<showimmediate>"))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.ShowImmediate,
                            Data = true,
                        });
                        continue;
                    }
                    if (fullTag.StartsWith("</showimmediate>"))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.ShowImmediate,
                            Data = false,
                        });
                        continue;
                    }
                    #endregion

                    #region Await Continue
                    if (fullTag.StartsWith("<awaitcontinue>"))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.AwaitContinue,
                            Data = null,
                        });
                        continue;
                    }
                    #endregion

                    #region Disable Skip
                    if (fullTag.StartsWith("<disableskip>"))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.DisableSkip,
                            Data = true,
                        });
                        continue;
                    }
                    if (fullTag.StartsWith("</disableskip>"))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.DisableSkip,
                            Data = false,
                        });
                        continue;
                    }
                    #endregion

                    #region Animation
                    if (fullTag.StartsWith("<anim=") && TryParseStringParameterFromFullTag(fullTag, out stringParam))
                    {
                        var appliedAnims = m_TextAnimator.AnimationSegments;
                        if (m_TagStack_Animation.Count > 0)
                        {
                            var lastAnimSegment = appliedAnims[appliedAnims.Count - 1];
                            lastAnimSegment.Length = m_SB_CleanText.Length - lastAnimSegment.Start;
                            appliedAnims[appliedAnims.Count - 1] = lastAnimSegment;
                        }

                        // Use for loop to avoid using a lambda expression that causes GC even when parsing is skipped
                        TextAnimation newAnim = null;
                        for (int j = 0; j < m_StyleSheet.Animations.Length; j++)
                            if (m_StyleSheet.Animations[j].Name.Equals(stringParam))
                                newAnim = m_StyleSheet.Animations[j].Animation;
                        if (!newAnim)
                            continue;
                        appliedAnims.Add(new TextAnimatonSegment() { Animation = newAnim, Start = m_SB_CleanText.Length, Length = -1 });
                        m_TagStack_Animation.Push(newAnim);

                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.Animation,
                            Data = stringParam,
                        });
                        continue;
                    }
                    if (fullTag.StartsWith("</anim>"))
                    {
                        var appliedAnims = m_TextAnimator.AnimationSegments;
                        if (m_TagStack_Animation.Count > 0)
                        {
                            var lastAnimSegment = appliedAnims[appliedAnims.Count - 1];
                            lastAnimSegment.Length = m_SB_CleanText.Length - lastAnimSegment.Start;
                            appliedAnims[appliedAnims.Count - 1] = lastAnimSegment;

                            m_TagStack_Animation.Pop();
                            if (m_TagStack_Animation.Count > 0)
                            {
                                TextAnimation newAnim = m_TagStack_Animation.Peek();
                                appliedAnims.Add(new TextAnimatonSegment() { Animation = newAnim, Start = m_SB_CleanText.Length, Length = -1 });
                            }
                        }

                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.Animation,
                            Data = stringParam,
                        });
                        continue;
                    }
                    #endregion

                    #region Event
                    if (fullTag.StartsWith("<event=") && TryParseStringParameterFromFullTag(fullTag, out stringParam))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.Event,
                            Data = stringParam,
                        });
                        continue;
                    }
                    #endregion

                    #region Await Trigger
                    if (fullTag.StartsWith("<awaittrigger=") && TryParseStringParameterFromFullTag(fullTag, out stringParam))
                    {
                        m_Parsed_Tags.Add(new WordSmithTextTag()
                        {
                            CleanIndex = m_SB_CleanText.Length,
                            Type = WordSmithTagType.AwaitTrigger,
                            Data = stringParam,
                        });
                        continue;
                    }
                    #endregion

                    #endregion

                    m_SB_CleanText.Append(tagSplits[i]);
                    m_SB_RichText.Append(tagSplits[i]);
                }
            }

            m_Parsed_WSText = text;
            m_Parsed_CleanText = m_SB_CleanText.ToString();
            m_Parsed_RichText = m_SB_RichText.ToString();

            m_TMP_Text.SetText(m_Parsed_RichText);
            m_TMP_Text.maxVisibleCharacters = 0;

            UnityEngine.Profiling.Profiler.EndSample();
        }


        private bool StringBuilderEquals(StringBuilder stringBuilder, string str)
        {
            if (stringBuilder == null || str == null)
                return false;
            if (stringBuilder.Length != str.Length)
                return false;
            for (int i = 0; i < stringBuilder.Length; i++)
                if (stringBuilder[i] != str[i])
                    return false;
            return true;
        }
        private bool StringBuilderStartsWith(StringBuilder stringBuilder, string str)
        {
            if (stringBuilder == null || str == null)
                return false;
            if (stringBuilder.Length < str.Length)
                return false;
            for (int i = 0; i < str.Length; i++)
                if (stringBuilder[i] != str[i])
                    return false;
            return true;
        }

        private bool TryParseStringParameterFromFullTag(string fullTag, out string parameter)
        {
            parameter = default;
            int equalsCharId = fullTag.IndexOf('=');
            if (equalsCharId == -1)
                return false;
            parameter = fullTag.Substring(equalsCharId + 1, fullTag.Length - 2 - equalsCharId);
            return true;
        }
        private bool TryParseFloatParameterFromFullTag(string fullTag, out float parameter)
        {
            parameter = default;
            int equalsCharId = fullTag.IndexOf('=');
            if (equalsCharId == -1)
                return false;
            return float.TryParse(fullTag.Substring(equalsCharId + 1, fullTag.Length - 2 - equalsCharId), NumberStyles.Number, CultureInfo.InvariantCulture, out parameter);
        }
        private bool TryParseIntParameterFromFullTag(string fullTag, out int parameter)
        {
            parameter = default;
            int equalsCharId = fullTag.IndexOf('=');
            if (equalsCharId == -1)
                return false;
            return int.TryParse(fullTag.Substring(equalsCharId + 1, fullTag.Length - 2 - equalsCharId), NumberStyles.Integer, CultureInfo.InvariantCulture, out parameter);
        }
        private bool TryParseBoolParameterFromFullTag(string fullTag, out bool parameter)
        {
            parameter = default;
            int equalsCharId = fullTag.IndexOf('=');
            if (equalsCharId == -1)
                return false;
            return bool.TryParse(fullTag.Substring(equalsCharId + 1, fullTag.Length - 2 - equalsCharId), out parameter);
        }
    }
}
