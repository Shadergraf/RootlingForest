using Manatea;
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
        public Optional<GameplayAttributeOwner> AttributeOwner;
        public GameplayAttribute Attribute = null;
        public AnimationCurve MappingCurve;
        public GameplayAttributeValueMode ValueMode = GameplayAttributeValueMode.EvaluatedValue;

        public override bool IsValid(VisualEffect component)
        {
            return Attribute;
        }

        protected override void Awake()
        {
            base.Awake();
            
            if (!AttributeOwner.hasValue)
            {
                AttributeOwner.value = GetComponentInParent<GameplayAttributeOwner>();
            }
        }

        public override void UpdateBinding(VisualEffect component)
        {
            float value = 0;
            if (ValueMode == GameplayAttributeValueMode.EvaluatedValue)
                AttributeOwner.value.TryGetAttributeEvaluatedValue(Attribute, out value);
            if (ValueMode == GameplayAttributeValueMode.BaseValue)
                AttributeOwner.value.TryGetAttributeBaseValue(Attribute, out value);

            component.SetFloat(m_Property, MappingCurve.Evaluate(value));
        }

        public override string ToString()
        {
            return string.Format("Value : '{0}' -> {1}", m_Property, Attribute == null ? "(null)" : Attribute.name);
        }
    }
}
