using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [DisallowMultipleComponent]
    public class GameplayEventReceiver : MonoBehaviour
    {
        private Dictionary<GameplayEvent, List<Action<object>>> m_EventListeners = new();

        private static WaitForSeconds m_WaitOneFrame = new WaitForSeconds(0);

        public void RegisterListener(GameplayEvent gameplayEvent, Action<object> listener)
        {
            if (!m_EventListeners.ContainsKey(gameplayEvent))
                m_EventListeners.Add(gameplayEvent, new List<Action<object>>());

            m_EventListeners[gameplayEvent].Add(listener);
        }
        public bool UnregisterListener(GameplayEvent gameplayEvent, Action<object> listener)
        {
            if (!m_EventListeners.ContainsKey(gameplayEvent))
                return false;

            m_EventListeners[gameplayEvent].Remove(listener);
            if (m_EventListeners[gameplayEvent].Count == 0)
                m_EventListeners.Remove(gameplayEvent);

            return true;
        }

        public void SendEventImmediate(GameplayEvent gameplayEvent)
        {
            if (!m_EventListeners.ContainsKey(gameplayEvent))
                return;

            for (int i = 0; i < m_EventListeners[gameplayEvent].Count; i++)
            {
                m_EventListeners[gameplayEvent][i].Invoke(null);
            }
        }
        public void SendEventImmediate(GameplayEvent gameplayEvent, object payload)
        {
            if (!m_EventListeners.ContainsKey(gameplayEvent))
                return;

            for (int i = 0; i < m_EventListeners[gameplayEvent].Count; i++)
            {
                m_EventListeners[gameplayEvent][i].Invoke(payload);
            }
        }
        public void SendEventDelayed(GameplayEvent gameplayEvent)
        {
            StartCoroutine(DelayedSendEvent(gameplayEvent, null));
        }
        public void SendEventDelayed(GameplayEvent gameplayEvent, object payload)
        {
            StartCoroutine(DelayedSendEvent(gameplayEvent, payload));
        }

        protected IEnumerator DelayedSendEvent(GameplayEvent gameplayEvent, object payload)
        {
            yield return m_WaitOneFrame;

            SendEventImmediate(gameplayEvent, payload);
        }
    }
}
