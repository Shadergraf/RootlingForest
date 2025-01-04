using System;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [RequireComponent(typeof(Rigidbody))]
    public class CollisionEventSender : MonoBehaviour
    {
        public event Action<Collision> OnCollisionEnterEvent;
        public event Action<Collision> OnCollisionExitEvent;
        public event Action<Collision> OnCollisionStayEvent;

        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionEnterEvent?.Invoke(collision);
        }
        private void OnCollisionExit(Collision collision)
        {
            OnCollisionExitEvent?.Invoke(collision);
        }
        private void OnCollisionStay(Collision collision)
        {
            OnCollisionStayEvent?.Invoke(collision);
        }
    }
}
