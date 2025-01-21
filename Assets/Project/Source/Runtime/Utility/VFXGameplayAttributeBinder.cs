using Manatea;
using Manatea.GameplaySystem;
using System.ComponentModel;
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
        public Fetched<GameplayAttributeOwner> AttributeOwner = new(FetchingType.InParents);
        public GameplayAttribute Attribute = null;
        public AnimationCurve MappingCurve;
        public GameplayAttributeValueMode ValueMode = GameplayAttributeValueMode.EvaluatedValue;
        public float ValueSmoothing = float.PositiveInfinity;

        private float m_OldValue;


        public override bool IsValid(VisualEffect component)
        {
            return Attribute;
        }

        protected override void Awake()
        {
            base.Awake();

            AttributeOwner.FetchFrom(gameObject);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (!AttributeOwner.value)
                return;

            float value = 0;

            if (ValueMode == GameplayAttributeValueMode.EvaluatedValue)
                AttributeOwner.value.TryGetAttributeEvaluatedValue(Attribute, out value);
            if (ValueMode == GameplayAttributeValueMode.BaseValue)
                AttributeOwner.value.TryGetAttributeBaseValue(Attribute, out value);

            float smoothedValue = MMath.Damp(m_OldValue, value, ValueSmoothing, Time.deltaTime);
            m_OldValue = smoothedValue;

            component.SetFloat(m_Property, MappingCurve.Evaluate(smoothedValue));
        }

        public override string ToString()
        {
            return string.Format("Value : '{0}' -> {1}", m_Property, Attribute == null ? "(null)" : Attribute.name);
        }
    }
}
