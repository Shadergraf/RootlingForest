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

        private void Awake()
        {
            if (!m_EventReceiver.hasValue)
            {
                m_EventReceiver.value = GetComponentInParent<GameplayEventReceiver>();
            }

            m_EventReceiver.value.RegisterListener(m_ListenToEvent, EventReceived);
        }

        private void EventReceived(object obj)
        {
            m_Response.Invoke();
        }
    }
}
