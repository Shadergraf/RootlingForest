using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.GameplaySystem
{
    public class GameplayEventListener : MonoBehaviour
    {
        [SerializeField]
        private Optional<GameplayEventReceiver> m_EventReceiver;
        [SerializeField]
        private GameplayEvent m_ListenToEvent;
        [SerializeField]
        private UnityEvent m_Response;

        private void OnEnable()
        {
            if (!m_EventReceiver.hasValue)
            {
                m_EventReceiver.value = GetComponentInParent<GameplayEventReceiver>();
            }
            if (!m_EventReceiver.value)
            {
                enabled = false;
                return;
            }

            m_EventReceiver.value.RegisterListener(m_ListenToEvent, EventReceived);
        }
        private void OnDisable()
        {
            if (!m_EventReceiver.hasValue)
                return;

            m_EventReceiver.value.UnregisterListener(m_ListenToEvent, EventReceived);
        }


        private void EventReceived(object obj)
        {
            m_Response.Invoke();
        }
    }
}
