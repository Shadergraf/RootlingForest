using Manatea;
using Manatea.GameplaySystem;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/GameplayTag Binder")]
    [VFXBinder("Gameplay/Tag")]
    public class VFXGameplayTagBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }


        [VFXPropertyBinding("System.Single"), SerializeField]
        protected ExposedProperty m_Property = "Attribute";
        public Fetched<GameplayTagOwner> TagOwner = new(FetchingType.InParents);
        public GameplayTag Tag = null;
        public bool InvertCondition;

        public override bool IsValid(VisualEffect component)
        {
            return Tag;
        }

        protected override void Awake()
        {
            base.Awake();

            TagOwner.FetchFrom(gameObject);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (!TagOwner.value)
                return;

            bool condition = TagOwner.value.HasTag(Tag);
            if (InvertCondition)
                condition = !condition;
            component.SetBool(m_Property, condition);
        }

        public override string ToString()
        {
            return string.Format("Value : '{0}' -> {1}", m_Property, Tag == null ? "(null)" : Tag.name);
        }
    }
}
