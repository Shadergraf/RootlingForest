using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [Serializable]
    public class GameplayAttributeInstance
    {
        [SerializeField]
        private GameplayAttribute m_Attribute;
        [SerializeField]
        private GameplayAttributeOwner m_AttributeOwner;
        [SerializeField]
        private float m_BaseValue;
        [SubclassSelector]
        [SerializeReference]
        private IGameplayAttributePostProcessor[] m_PostProcessors;
        [SerializeField]
        private List<GameplayAttributeModifierInstance> m_Modifiers = new();

        public event Action OnChange;


        public GameplayAttribute Attribute => m_Attribute;
        public IGameplayAttributePostProcessor[] PostProcessors => m_PostProcessors;

        public float BaseValue
        {
            get => m_BaseValue;
            set
            {
                if (m_BaseValue != value)
                {
                    m_BaseValue = value;
                    OnChange?.Invoke();
                }
            }
        }


        public float EvaluatedValue
        {
            get
            {
                float sum = 0;
                float product = 1;
                for (int i = 0; i < m_Modifiers.Count; i++)
                {
                    switch (m_Modifiers[i].Type)
                    {
                        case GameplayAttributeModifierType.Additive: sum += m_Modifiers[i].Value; break;
                        case GameplayAttributeModifierType.Multiplicative: product *= m_Modifiers[i].Value; break;
                    }
                }
                float evaluatedValue = (BaseValue + sum) * product;
                for (int i = 0; i < m_PostProcessors.Length; i++)
                {
                    m_PostProcessors[i].Process(m_AttributeOwner, m_Attribute, ref evaluatedValue);
                }
                return evaluatedValue;
            }
        }

        public GameplayAttributeInstance(GameplayAttributeOwner attributeOwner, GameplayAttribute attribute, float baseValue = 0, IGameplayAttributePostProcessor[] postProcessors = null)
        {
            m_AttributeOwner = attributeOwner;
            m_Attribute = attribute;
            BaseValue = baseValue;
            if (postProcessors != null)
                m_PostProcessors = postProcessors;
            else
                m_PostProcessors = new IGameplayAttributePostProcessor[0];
        }

        public bool AddModifier(GameplayAttributeModifierInstance modifier)
        {
            if (m_Modifiers.Contains(modifier))
            {
                return false;
            }

            m_Modifiers.Add(modifier);
            return true;
        }
        public bool RemoveModifier(GameplayAttributeModifierInstance modifier)
        {
            return m_Modifiers.Remove(modifier);
        }
        public ReadOnlyCollection<GameplayAttributeModifierInstance> GetModifierList()
        {
            return m_Modifiers.AsReadOnly();
        }
    }
}
