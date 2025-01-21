using System.Collections;
using UnityEngine;

namespace Manatea.GameplaySystem.Modifiers
{
    public class AttributeRandomizer : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayAttributeOwner> m_AttributeOwner = new(FetchingType.InParents);
        [SerializeField]
        private GameplayAttribute m_Attribute;
        [SerializeField]
        private GameplayAttributeModifierType m_ModificationType;
        [SerializeField]
        private float m_MinModification = 1;
        [SerializeField]
        private float m_MaxModification = 1;
        [SerializeField]
        private bool m_ModifyBaseValue;

        private GameplayAttributeModifierInstance m_ModifierInst;


        private void Awake()
        {
            m_AttributeOwner.FetchFrom(gameObject);

            m_ModifierInst = new GameplayAttributeModifierInstance();
        }
        private void OnEnable()
        {
            if (m_ModifyBaseValue)
            {
                if (m_AttributeOwner.value.TryGetAttributeBaseValue(m_Attribute, out float baseValue))
                {
                    switch (m_ModificationType)
                    {
                        case GameplayAttributeModifierType.Additive:
                            baseValue += MMath.Lerp(m_MinModification, m_MaxModification, Random.value);
                            break;
                        case GameplayAttributeModifierType.Multiplicative:
                            baseValue *= MMath.Lerp(m_MinModification, m_MaxModification, Random.value);
                            break;
                    }
                    m_AttributeOwner.value.SetAttributeBaseValue(m_Attribute, baseValue);
                }
            }
            else
            {
                m_AttributeOwner.value.AddAttributeModifier(m_Attribute, m_ModifierInst);
                UpdateModifier();
            }
        }
        private void OnDisable()
        {
            if (!m_ModifyBaseValue)
            {
                m_AttributeOwner.value.RemoveAttributeModifier(m_Attribute, m_ModifierInst);
            }
        }

        private void UpdateModifier()
        {
            m_ModifierInst.Type = m_ModificationType;
            m_ModifierInst.Value = MMath.Lerp(m_MinModification, m_MaxModification, Random.value);
        }
    }
}