using Manatea;
using Manatea.GameplaySystem;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/GameplayTagFilter Binder")]
    [VFXBinder("Gameplay/TagFilter")]
    public class VFXGameplayTagFilterBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }


        [VFXPropertyBinding("System.Single"), SerializeField]
        protected ExposedProperty m_Property = "Attribute";
        public Fetched<GameplayTagOwner> TagOwner = new(FetchingType.InParents);
        public GameplayTagFilter TagFilter;
        public bool InvertCondition;

        public override bool IsValid(VisualEffect component)
        {
            return TagFilter != null;
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

            bool condition = TagOwner.value.SatisfiesTagFilter(TagFilter);
            if (InvertCondition)
                condition = !condition;
            component.SetBool(m_Property, condition);
        }

        public override string ToString()
        {
            return string.Format("Value : '{0}' -> {1}", m_Property, "TagFilter");
        }
    }
}
