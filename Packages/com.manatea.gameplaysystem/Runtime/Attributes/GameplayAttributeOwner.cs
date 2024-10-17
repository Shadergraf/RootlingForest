using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.GameplaySystem
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-99)]
    public class GameplayAttributeOwner : MonoBehaviour
    {
        [SerializeField]
        private GameplayAttributeDefinition[] m_InitialAttributes;

        private List<GameplayAttributeInstance> m_AttributeInstances = new List<GameplayAttributeInstance>();
        private bool m_Initialized;

        private void Awake()
        {
            m_AttributeInstances.Capacity = m_InitialAttributes.Length;
            m_Initialized = true;

            for (int i = 0; m_InitialAttributes.Length > i; i++)
            {
                AddAttribute(m_InitialAttributes[i].Attribute, m_InitialAttributes[i].BaseValue, m_InitialAttributes[i].PostProcessors);
            }
        }


        public GameplayAttributeInstance AddAttribute(GameplayAttribute attribute, float baseValue = 0, IGameplayAttributePostProcessor[] postProcessors = null)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
                if (m_AttributeInstances[i].Attribute == attribute)
                    return null;

            var attributeInst = new GameplayAttributeInstance(this, attribute, baseValue, postProcessors);
            m_AttributeInstances.Add(attributeInst);

            return attributeInst;
        }
        public bool HasAttribute(GameplayAttribute attribute)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
                if (m_AttributeInstances[i].Attribute == attribute)
                    return true;
            return false;
        }

        public bool SetAttributeBaseValue(GameplayAttribute attribute, float baseValue)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    m_AttributeInstances[i].BaseValue = baseValue;
                    return true;
                }
            }
            return false;
        }
        public bool ChangeAttributeBaseValue(GameplayAttribute attribute, Func<float, float> function)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    m_AttributeInstances[i].BaseValue = function(m_AttributeInstances[i].BaseValue);
                    return true;
                }
            }
            return false;
        }
        public bool AddAttributeModifier(GameplayAttribute attribute, GameplayAttributeModifierInstance modifier)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    m_AttributeInstances[i].AddModifier(modifier);
                    return true;
                }
            }
            return false;
        }
        public bool RemoveAttributeModifier(GameplayAttribute attribute, GameplayAttributeModifierInstance modifier)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    return m_AttributeInstances[i].RemoveModifier(modifier);
                }
            }
            return false;
        }

        public GameplayAttributeInstance GetAttributeInstance(GameplayAttribute attribute)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    return m_AttributeInstances[i];
                }
            }
            return null;
        }

        public bool TryGetAttributeBaseValue(GameplayAttribute attribute, out float baseValue)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            baseValue = 0;
            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    baseValue = m_AttributeInstances[i].BaseValue;
                    return true;
                }
            }
            return false;
        }
        public bool TryGetAttributeEvaluatedValue(GameplayAttribute attribute, out float evaluatedValue)
        {
            Debug.Assert(m_Initialized, "Attribute owner was not initialized!", gameObject);

            evaluatedValue = 0;
            for (int i = 0; i < m_AttributeInstances.Count; i++)
            {
                if (m_AttributeInstances[i].Attribute == attribute)
                {
                    evaluatedValue = m_AttributeInstances[i].EvaluatedValue;
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public struct GameplayAttributeDefinition
    {
        public GameplayAttribute Attribute;
        public float BaseValue;
        [SubclassSelector]
        [SerializeReference]
        public IGameplayAttributePostProcessor[] PostProcessors;
    }
}
