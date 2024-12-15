using System.Collections;
using UnityEngine;

namespace Manatea.GameplaySystem.Modifiers
{
    public class AttributeNoiseModifier : MonoBehaviour
    {
        [SerializeField]
        private Optional<GameplayAttributeOwner> m_AttributeOwner;
        [SerializeField]
        private GameplayAttribute m_Attribute;
        [SerializeField]
        private GameplayAttributeModifierType m_ModificationType;
        [SerializeField]
        private float m_TimeSpeed = 1;
        [SerializeField]
        private float m_Strength = 1;
        [SerializeField]
        private float m_StrengthOffset = 0;
        [SerializeField]
        private float m_TimeOffset = 0;
        [SerializeField]
        private bool m_RandomPhase;

        private float m_Time;
        private float m_Phase;
        private GameplayAttributeModifierInstance m_ModifierInst;


        private void Awake()
        {
            if (!m_AttributeOwner.hasValue)
                m_AttributeOwner.value = GetComponentInParent<GameplayAttributeOwner>();

            m_ModifierInst = new GameplayAttributeModifierInstance(m_ModificationType, 0);

            if (m_RandomPhase)
            {
                m_Phase = Random.Range(-100, 100);
            }
        }
        private void OnEnable()
        {
            m_AttributeOwner.value.AddAttributeModifier(m_Attribute, m_ModifierInst);
            UpdateModifier();
        }
        private void OnDisable()
        {
            m_AttributeOwner.value.RemoveAttributeModifier(m_Attribute, m_ModifierInst);
        }


        private void FixedUpdate()
        {
            m_Time += Time.deltaTime;
            UpdateModifier();
        }

        private void UpdateModifier()
        {
            m_ModifierInst.Value = (Mathf.PerlinNoise(m_Time * m_TimeSpeed + m_TimeOffset, m_Phase) - 0.5f) * 2 * m_Strength + m_StrengthOffset;
        }
    }
}