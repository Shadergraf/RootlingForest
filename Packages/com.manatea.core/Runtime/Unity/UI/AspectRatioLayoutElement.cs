using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/AspectRatio Layout Element", 140)]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class AspectRatioLayoutElement : UIBehaviour, ILayoutElement, ILayoutIgnorer
    {
        /// <summary>
        /// Specifies a mode to use to enforce an aspect ratio.
        /// </summary>
        public enum AspectMode
        {
            /// <summary>
            /// The aspect ratio is not enforced
            /// </summary>
            None,
            /// <summary>
            /// Changes the height of the rectangle to match the aspect ratio.
            /// </summary>
            WidthControlsHeight,
            /// <summary>
            /// Changes the width of the rectangle to match the aspect ratio.
            /// </summary>
            HeightControlsWidth
        }


        [SerializeField] private bool m_IgnoreLayout = false;
        [SerializeField] private AspectMode m_AspectMode = AspectMode.None;
        [SerializeField] private float m_AspectRatio = 1;
        [SerializeField] private int m_LayoutPriority = 1;

        /// <summary>
        /// Should this RectTransform be ignored by the layout system?
        /// </summary>
        /// <remarks>
        /// Setting this property to true will make a parent layout group component not consider this RectTransform part of the group. The RectTransform can then be manually positioned despite being a child GameObject of a layout group.
        /// </remarks>
        public virtual bool ignoreLayout { get { return m_IgnoreLayout; } set { if (SetPropertyUtility.SetStruct(ref m_IgnoreLayout, value)) SetDirty(); } }

        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }

        public virtual float minWidth => m_AspectMode == AspectMode.HeightControlsWidth ? (transform.parent as RectTransform).rect.height * m_AspectRatio : -1;
        public virtual float minHeight => m_AspectMode == AspectMode.WidthControlsHeight ? (transform.parent as RectTransform).rect.width * m_AspectRatio : -1;
        public virtual float preferredWidth => minWidth;
        public virtual float preferredHeight => minHeight;

        public virtual float flexibleWidth => -1;
        public virtual float flexibleHeight => -1;
        public virtual int layoutPriority { get { return m_LayoutPriority; } set { if (SetPropertyUtility.SetStruct(ref m_LayoutPriority, value)) SetDirty(); } }


        protected AspectRatioLayoutElement()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        /// <summary>
        /// Mark the LayoutElement as dirty.
        /// </summary>
        /// <remarks>
        /// This will make the auto layout system process this element on the next layout pass. This method should be called by the LayoutElement whenever a change is made that potentially affects the layout.
        /// </remarks>
        protected void SetDirty()
        {
            if (!IsActive())
                return;
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }

#endif
    }
}