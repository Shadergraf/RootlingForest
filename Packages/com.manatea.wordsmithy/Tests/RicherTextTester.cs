using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Manatea.WordSmithy;

namespace Manatea
{
    public class RicherTextTester : MonoBehaviour
    {
        public WordSmithy.Animations.TextAnimator m_TextAnimator;
        public WordSmithy.WordSmith m_RicherText;

        public WordSmithy.Animations.TextAnimation[] m_Animations;
        public Slider m_AnimSlider;
        public TMPro.TMP_Text m_AnimName;

        public void OnEnable()
        {
            m_RicherText.OnTextEvent += M_RicherText_OnTextEvent;

            m_AnimSlider.maxValue = m_Animations.Length - 1;
            if (m_TextAnimator.ConstantAnimations.Count > 0 && m_Animations.Length > 0)
                SetTextAnimation(0);
        }

        private void M_RicherText_OnTextEvent(string eventName)
        {
            Debug.Log("Animation event:" + eventName);
        }

        public void SetTextAnimation(float f)
        {
            SetTextAnimation(Mathf.RoundToInt(f));
        }
        public void SetTextAnimation(int i)
        {
            m_AnimName.text = m_Animations[i].name;
            m_TextAnimator.ConstantAnimations[0] = m_Animations[i];
        }
    }
}
