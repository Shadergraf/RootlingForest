using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.GameplaySystem
{
    public class GameplayEventListener : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayEventReceiver> m_EventReceiver = new(FetchingType.InParents);
        [SerializeField]
        private GameplayEvent m_ListenToEvent;
        [SerializeField]
        private UnityEvent m_Response;
        [SerializeField]
        private UnityEvent<object> m_PayloadResponse;

        private void Awake()
        {
            m_EventReceiver.FetchFrom(gameObject);
        }

        private void OnEnable()
        {
            if (!m_EventReceiver.value)
            {
                enabled = false;
                return;
            }

            m_EventReceiver.value.RegisterListener(m_ListenToEvent, EventReceived);
        }
        private void OnDisable()
        {
            if (!m_EventReceiver.value)
                return;

            m_EventReceiver.value.UnregisterListener(m_ListenToEvent, EventReceived);
        }


        private void EventReceived(object payload)
        {
            m_Response.Invoke();
            m_PayloadResponse.Invoke(payload);
        }
    }
}
