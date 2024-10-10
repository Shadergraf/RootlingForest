using System;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.GameplaySystem
{
    public class GameplayAttributeTester : MonoBehaviour
    {
        [SerializeField]
        private GameplayAttributeOwner m_AttributeOwner;

        [SerializeField]
        private GameplayAttributeComparison m_Comparison;
        [SerializeField]
        private GameplayAttribute m_AttributeA;
        [SerializeField]
        private float m_ValueB;
        [SerializeField]
        private GameplayAttribute m_AttributeB;

        [SerializeField]
        private UnityEvent m_Event;

        private void FixedUpdate()
        {
            Test();
        }

        public void Test()
        {
            if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_AttributeA, out float valueA))
            {
                float valueB = m_ValueB;
                if (m_AttributeB)
                {
                    m_AttributeOwner.TryGetAttributeEvaluatedValue(m_AttributeB, out valueB);
                }

                if (DoComparison(m_Comparison, valueA, valueB))
                {
                    Trigger();
                }
            }
        }

        private static bool DoComparison(GameplayAttributeComparison comparison, float valueA, float valueB)
        {
            switch (comparison)
            {
                case GameplayAttributeComparison.Equal: return valueA == valueB;
                case GameplayAttributeComparison.NotEqual: return valueA != valueB;
                case GameplayAttributeComparison.LessThan: return valueA < valueB;
                case GameplayAttributeComparison.GreaterThan: return valueA > valueB;
                case GameplayAttributeComparison.LessThanOrEqual: return valueA <= valueB;
                case GameplayAttributeComparison.GreaterThanOrEqual: return valueA >= valueB;
            }
            return false;
        }

        public void Trigger()
        {
            m_Event.Invoke();
        }
    }

    public enum GameplayAttributeComparison
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        GreaterThan = 3,
        LessThanOrEqual = 4,
        GreaterThanOrEqual = 5,
    }
}
