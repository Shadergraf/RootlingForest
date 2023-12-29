using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea
{
    public class OnTriggerEnterCallback : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent<Collider> m_OnTriggerEnter;

        private void OnTriggerEnter(Collider other)
        {
            m_OnTriggerEnter.Invoke(other);
        }
    }
}
