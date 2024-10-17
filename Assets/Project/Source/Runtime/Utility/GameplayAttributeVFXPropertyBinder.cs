using Manatea.GameplaySystem;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/GameplayAttribute Binder")]
    [VFXBinder("Gameplay/Attribute")]
    public class VFXGameplayAttributeBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }


        [VFXPropertyBinding("System.Single"), SerializeField]
        protected ExposedProperty m_Property = "Attribute";
        public GameplayAttributeOwner AttributeOwner = null;
        public GameplayAttribute Attribute = null;
        public AnimationCurve MappingCurve;
        public GameplayAttributeValueMode ValueMode = GameplayAttributeValueMode.EvaluatedValue;

        public override bool IsValid(VisualEffect component)
        {
            return AttributeOwner && Attribute;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            float value = 0;
            if (ValueMode == GameplayAttributeValueMode.EvaluatedValue)
                AttributeOwner.TryGetAttributeEvaluatedValue(Attribute, out value);
            if (ValueMode == GameplayAttributeValueMode.BaseValue)
                AttributeOwner.TryGetAttributeBaseValue(Attribute, out value);

            component.SetFloat(m_Property, MappingCurve.Evaluate(value));
        }

        public override string ToString()
        {
            return string.Format("Value : '{0}' -> {1}", m_Property, Attribute == null ? "(null)" : Attribute.name);
        }
    }
}
