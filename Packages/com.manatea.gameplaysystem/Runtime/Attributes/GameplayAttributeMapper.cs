using System;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.GameplaySystem
{
    public class GameplayAttributeMapper : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayAttributeOwner> m_AttributeOwner = new(FetchingType.InParents);

        [SerializeField]
        private GameplayAttribute m_Attribute;
        [SerializeField]
        private GameplayAttributeValueMode m_ValueMode = GameplayAttributeValueMode.EvaluatedValue;
        [SerializeField]
        private float m_ValueSmoothing = float.PositiveInfinity;

        [SerializeField]
        private GameplayAttributeValueMapping[] m_Mappings;

        private float m_OldValue;


        private void Awake()
        {
            m_AttributeOwner.FetchFrom(gameObject);
        }
        private void OnEnable()
        {
            float attributeValue = GetValue();
            MapWithValue(attributeValue);
            m_OldValue = attributeValue;
        }

        private void Update()
        {
            float attributeValue = GetValue();

            float smoothedValue = MMath.Damp(m_OldValue, attributeValue, m_ValueSmoothing, Time.deltaTime);
            MapWithValue(smoothedValue);

            m_OldValue = smoothedValue;
        }

        private float GetValue()
        {
            float attributeValue = 0;
            switch (m_ValueMode)
            {
                case GameplayAttributeValueMode.EvaluatedValue:
                    m_AttributeOwner.value.TryGetAttributeEvaluatedValue(m_Attribute, out attributeValue);
                    break;
                case GameplayAttributeValueMode.BaseValue:
                    m_AttributeOwner.value.TryGetAttributeBaseValue(m_Attribute, out attributeValue);
                    break;
            }
            return attributeValue;
        }

        protected void MapWithValue(float value)
        {
            for (int i = 0;  i < m_Mappings.Length; i++)
            {
                m_Mappings[i].Event.Invoke(m_Mappings[i].MappingCurve.Evaluate(value));
            }
        }
    }

    public enum GameplayAttributeValueMode
    {
        BaseValue = 0,
        EvaluatedValue = 1,
    }

    [Serializable]
    public struct GameplayAttributeValueMapping
    {
        public AnimationCurve MappingCurve;
        public UnityEvent<float> Event;
    }
}
