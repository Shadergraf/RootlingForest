using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class AnimationCallback : MonoBehaviour
    {
        public UnityEvent[] events;

        public void InvokeEvent(int index)
        {
            if (events.Length < index || events[index] == null)
            {
                Debug.LogError("Event of index " + index + " not set up.", this);
                return;
            }

            events[index].Invoke();
        }
    }
}