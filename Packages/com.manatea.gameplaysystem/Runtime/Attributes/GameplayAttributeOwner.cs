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
        public class GameplayAttributeInstance
        {
            [SerializeField]
            private GameplayAttribute m_Attribute;
            [SerializeField]
            private float m_BaseValue;
            [SubclassSelector]
            [SerializeReference]
            private IGameplayAttributePostProcessor[] m_PostProcessors;
            [SerializeField]
            private List<GameplayAttributeModifierInstance> m_Modifiers = new();

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

            public GameplayAttributeInstance(GameplayAttribute attribute, float baseValue = 0)
            {
                m_Attribute = attribute;
                BaseValue = baseValue;
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

        [SerializeField]
        private List<GameplayAttributeInstance> m_Attributes;


        public bool AddAttribute(GameplayAttribute attribute, float baseValue = 0)
        {
            for (int i = 0; i < m_Attributes.Count; i++)
                if (m_Attributes[i].Attribute == attribute)
                    return false;

            m_Attributes.Add(new GameplayAttributeInstance(attribute, baseValue));

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
        public bool AddAttributeModifier(GameplayAttribute attribute, GameplayAttributeModifierInstance modifier)
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
        public bool RemoveAttributeModifier(GameplayAttribute attribute, GameplayAttributeModifierInstance modifier)
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

        public GameplayAttributeInstance GetValuedAttribute(GameplayAttribute attribute)
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
    public struct GameplayAttributeModification
    {
        public GameplayAttribute Attribute;
        public GameplayAttributeModifierType Type;
        public float Value;
    }

    [Serializable]
    public struct GameplayAttributeModifier
    {
        public GameplayAttributeModifierType Type;
        public float Value;
    }

    [Serializable]
    public class GameplayAttributeModifierInstance
    {
        public event Action OnChange;

        private GameplayAttributeModifier m_Modifier;


        public GameplayAttributeModifierInstance()
        { }
        public GameplayAttributeModifierInstance(GameplayAttributeModifier modifier)
        {
            m_Modifier = modifier;
        }
        public GameplayAttributeModifierInstance(GameplayAttributeModifierType type, float value)
        {
            m_Modifier = new GameplayAttributeModifier()
            {
                Type = type,
                Value = value,
            };
        }

        public GameplayAttributeModifierType Type
        {
            get => m_Modifier.Type;
            set
            {
                if (value != m_Modifier.Type)
                {
                    m_Modifier.Type = value;
                    OnChange?.Invoke();
                }
            }
        }
        public float Value
        {
            get => m_Modifier.Value;
            set
            {
                if (value != m_Modifier.Value)
                {
                    m_Modifier.Value = value;
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

    public interface IGameplayAttributePostProcessor
    {
        public float Process() => 0;
    }
    [Serializable]
    public class ValuePostProcessor : IGameplayAttributePostProcessor
    {
        public GameplayAttributePostProcessorLimiter Limiter;
        public float Value;
        public float Process() => Value;
    }
    [Serializable]
    public class AttributePostProcessor : IGameplayAttributePostProcessor
    {
        public GameplayAttributePostProcessorLimiter Limiter;
        public GameplayAttribute Attribute;
        public float Process() => 0;
    }

    public enum GameplayAttributePostProcessorLimiter
    {
        Max = 1,
        Min = 0,
    }
}
