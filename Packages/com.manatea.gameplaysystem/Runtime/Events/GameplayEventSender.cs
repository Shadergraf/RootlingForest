using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class GameplayEventSender : MonoBehaviour
    {
        [SerializeField]
        private Optional<GameplayEventReceiver> m_EventReceiver;
        [SerializeField]
        private GameplayEvent m_Event;
        [SerializeField]
        private bool m_SendDelayed;
        [SerializeField]
        private bool m_SendOnEnable;
        [SerializeField]
        private bool m_SendOnDisable;
        [SerializeField]
        private bool m_SendOnStart;


        private void Awake()
        {
            if (!m_EventReceiver.hasValue)
                m_EventReceiver.value = GetComponentInParent<GameplayEventReceiver>();
        }

        private void Start()
        {
            if (!m_SendOnStart)
                return;

            if (m_SendDelayed)
                SendDelayed();
            else
                SendImmediate();
        }
        private void OnEnable()
        {
            if (!m_SendOnEnable)
                return;

            if (m_SendDelayed)
                SendDelayed();
            else
                SendImmediate();
        }
        private void OnDisable()
        {
            if (!m_SendOnDisable)
                return;

            if (m_SendDelayed)
                SendDelayed();
            else
                SendImmediate();
        }

        public void SendImmediate()
        {
            m_EventReceiver.value.SendEventImmediate(m_Event);
        }
        public void SendDelayed()
        {
            m_EventReceiver.value.SendEventDelayed(m_Event);
        }
    }
}
