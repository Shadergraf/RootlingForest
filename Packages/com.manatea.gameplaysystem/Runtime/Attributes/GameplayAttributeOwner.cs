using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [DisallowMultipleComponent]
    public class GameplayAttributeOwner : MonoBehaviour
    {
        [Serializable]
        public class ValuedGameplayAttribute
        {
            [SerializeField]
            private GameplayAttribute m_Attribute;
            [SerializeField]
            private float m_BaseValue;
            [SerializeField]
            private List<GameplayAttributeModifier> m_Modifiers = new();

            public event Action OnChange;

            public GameplayAttribute Attribute => m_Attribute;
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
                    return (BaseValue + sum) * product;
                }
            }

            public ValuedGameplayAttribute(GameplayAttribute attribute, float baseValue = 0)
            {
                m_Attribute = attribute;
                BaseValue = baseValue;
            }

            public bool AddModifier(GameplayAttributeModifier modifier)
            {
                if (m_Modifiers.Contains(modifier))
                {
                    return false;
                }

                m_Modifiers.Add(modifier);
                return true;
            }
            public bool RemoveModifier(GameplayAttributeModifier modifier)
            {
                return m_Modifiers.Remove(modifier);
            }
            public ReadOnlyCollection<GameplayAttributeModifier> GetModifierList()
            {
                return m_Modifiers.AsReadOnly();
            }
        }

        [SerializeField]
        private List<ValuedGameplayAttribute> m_Attributes;

        public bool AddAttribute(GameplayAttribute attribute, float baseValue = 0)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
                if (m_Attributes[i].Attribute == attribute)
                    return false;

            m_Attributes.Add(new ValuedGameplayAttribute(attribute, baseValue));

            return true;
        }
        public bool HasAttribute(GameplayAttribute attribute)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
                if (m_Attributes[i].Attribute == attribute)
                    return true;
            return false;
        }

        public bool SetAttributeBaseValue(GameplayAttribute attribute, float baseValue)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    m_Attributes[i].BaseValue = baseValue;
                    return true;
                }
            }
            return false;
        }
        public bool ChangeAttributeBaseValue(GameplayAttribute attribute, Func<float, float> function)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    m_Attributes[i].BaseValue = function(m_Attributes[i].BaseValue);
                    return true;
                }
            }
            return false;
        }
        public bool AddAttributeModifier(GameplayAttribute attribute, GameplayAttributeModifier modifier)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    m_Attributes[i].AddModifier(modifier);
                    return true;
                }
            }
            return false;
        }
        public bool RemoveAttributeModifier(GameplayAttribute attribute, GameplayAttributeModifier modifier)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    return m_Attributes[i].RemoveModifier(modifier);
                }
            }
            return false;
        }

        public ValuedGameplayAttribute GetValuedAttribute(GameplayAttribute attribute)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    return m_Attributes[i];
                }
            }
            return null;
        }

        public bool TryGetAttributeBaseValue(GameplayAttribute attribute, out float baseValue)
        {
            baseValue = 0;
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    baseValue = m_Attributes[i].BaseValue;
                    return true;
                }
            }
            return false;
        }
        public bool TryGetAttributeEvaluatedValue(GameplayAttribute attribute, out float evaluatedValue)
        {
            evaluatedValue = 0;
            for (int i = 0; i < m_Attributes.Count; i++)
            {
                if (m_Attributes[i].Attribute == attribute)
                {
                    evaluatedValue = m_Attributes[i].EvaluatedValue;
                    return true;
                }
            }
            return false;
        }

        public void OnValidate()
        {
            // TODO editor change needs to send events
        }
    }

    [Serializable]
    public class GameplayAttributeModifier
    {
        [SerializeField]
        private GameplayAttributeModifierType m_Type;
        [SerializeField]
        private float m_Value;

        public event Action OnChange;

        public GameplayAttributeModifierType Type
        {
            get => m_Type;
            set
            {
                if (value != m_Type)
                {
                    m_Type = value;
                    OnChange?.Invoke();
                }
            }
        }
        public float Value
        {
            get => m_Value;
            set
            {
                if (value != m_Value)
                {
                    m_Value = value;
                    OnChange?.Invoke();
                }
            }
        }
    }

    public enum GameplayAttributeModifierType
    {
        Additive = 0,
        Multiplicative = 1,
    }

}
