using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Manatea
{
    public class DelayedEvent : MonoBehaviour
    {
        [SerializeField]
        [Min(0)]
        private float m_Delay = 1;
        [SerializeField]
        private UnityEvent m_Event;


        private void OnEnable()
        {
            StartCoroutine(EventDelayed());
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }


        private IEnumerator EventDelayed()
        {
            yield return new WaitForSeconds(m_Delay);
            m_Event.Invoke();
        }
    }
}