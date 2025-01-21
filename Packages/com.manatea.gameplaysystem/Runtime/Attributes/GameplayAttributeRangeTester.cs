using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Manatea.GameplaySystem
{
    public class GameplayAttributeRangeTester : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayAttributeOwner> m_AttributeOwner = new(FetchingType.InParents);
        
        [SerializeField]
        private GameplayAttribute m_Attribute;
        [SerializeField]
        private RangeFloat m_Range;
        [SerializeField]
        private GameplayAttributeRangeComparison m_Type;

        [SerializeField]
        [FormerlySerializedAs("m_Event")]
        private UnityEvent m_TestSucceded;
        [SerializeField]
        private UnityEvent m_TestFailed;

        private bool m_Triggered;


        private void Awake()
        {
            m_AttributeOwner.FetchFrom(gameObject);
        }
        private void Start()
        {
            if (m_AttributeOwner.value.TryGetAttributeEvaluatedValue(m_Attribute, out float value))
            {
                m_Triggered = !DoComparison(m_Type, value, m_Range);
                if (m_Triggered)
                    Trigger();
                else
                    ResetTrigger();
            }
        }

        private void FixedUpdate()
        {
            Test();
        }

        public void Test()
        {
            if (m_AttributeOwner.value.TryGetAttributeEvaluatedValue(m_Attribute, out float value))
            {
                if (DoComparison(m_Type, value, m_Range))
                    Trigger();
                else
                    ResetTrigger();
            }
        }

        private static bool DoComparison(GameplayAttributeRangeComparison comparison, float value, RangeFloat range)
        {
            switch (comparison)
            {
                case GameplayAttributeRangeComparison.ClosedStartClosedEnd: return value >= range.start && value <= range.end;
                case GameplayAttributeRangeComparison.OpenStartClosedEnd: return value > range.start && value <= range.end;
                case GameplayAttributeRangeComparison.ClosedStartOpenEnd: return value >= range.start && value < range.end;
                case GameplayAttributeRangeComparison.OpenStartOpenEnd: return value > range.start && value < range.end;
            }
            return false;
        }

        public void Trigger()
        {
            if (m_Triggered)
                return;
            m_TestSucceded.Invoke();
            m_Triggered = true;

        }
        public void ResetTrigger()
        {
            if (!m_Triggered)
                return;
            m_TestFailed.Invoke();
            m_Triggered = false;
        }
    }

    public enum GameplayAttributeRangeComparison
    {
        ClosedStartClosedEnd = 0,
        OpenStartClosedEnd = 1,
        ClosedStartOpenEnd = 2,
        OpenStartOpenEnd = 3,
    }
}
