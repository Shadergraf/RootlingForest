using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class DragZone : MonoBehaviour
    {
        [Min(0)]
        [SerializeField]
        private float m_Drag = 0.5f;
        [Min(0)]
        [SerializeField]
        private float m_AngularDrag = 0.5f;

        private Rigidbody m_SelfRigidbody;

        private void Start()
        {
            m_SelfRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.attachedRigidbody)
            {
                return;
            }
            if (other.attachedRigidbody == m_SelfRigidbody)
            {
                return;
            }

            // TODO handle multiple colliders with the same rigidbody in one frame
            if (!other.attachedRigidbody.isKinematic)
            {
                other.attachedRigidbody.linearVelocity = other.attachedRigidbody.linearVelocity * (1 - Time.fixedDeltaTime * m_Drag);
                other.attachedRigidbody.angularVelocity = other.attachedRigidbody.angularVelocity * (1 - Time.fixedDeltaTime * m_AngularDrag);
            }
        }
    }
}
