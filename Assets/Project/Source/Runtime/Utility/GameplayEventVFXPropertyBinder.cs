using Manatea;
using Manatea.GameplaySystem;
using System.Diagnostics.Tracing;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    public class VFXGameplayEventBinder : MonoBehaviour
    {
        [SerializeField]
        protected VisualEffect m_VisualEffect;
        public string VFXEventName = "Event";

        [SerializeField]
        protected Optional<GameplayEventReceiver> m_GameplayEventReceiver;
        [SerializeField]
        protected GameplayEvent m_GameplayEvent;

        [SerializeField, HideInInspector]
        protected VFXEventAttribute eventAttribute;


        private void Awake()
        {
            if (!m_GameplayEventReceiver.hasValue)
            {
                m_GameplayEventReceiver.value = GetComponentInParent<GameplayEventReceiver>();
            }
        }

        protected virtual void OnEnable()
        {
            UpdateCacheEventAttribute();

            m_GameplayEventReceiver.value.RegisterListener(m_GameplayEvent, SendEventToVisualEffect);
        }
        protected virtual void OnDisable()
        {
            m_GameplayEventReceiver.value.UnregisterListener(m_GameplayEvent, SendEventToVisualEffect);
        }

        private void OnValidate()
        {
            UpdateCacheEventAttribute();
        }

        private void UpdateCacheEventAttribute()
        {
            if (m_VisualEffect != null)
                eventAttribute = m_VisualEffect.CreateVFXEventAttribute();
            else
                eventAttribute = null;
        }

        protected void SendEventToVisualEffect(object payload)
        {
            if (!m_VisualEffect)
                return;

            m_VisualEffect.SendEvent(VFXEventName, eventAttribute);
        }
    }
}
