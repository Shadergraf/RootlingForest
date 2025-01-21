using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class ContinuousForce : MonoBehaviour
    {
        [SerializeField]
        private Vector3 m_Direction = Vector3.forward;
        [SerializeField]
        private float m_Strength = 1;
        [SerializeField]
        private ForceMode m_ForceMode = ForceMode.Force;
        [SerializeField]
        private Space m_Space = Space.Self;

        public Vector3 Direction
        { get { return m_Direction; } set { m_Direction = value; } }
        public float Strength
        { get { return m_Strength; } set { m_Strength = value; } }
        public ForceMode ForceMode
        { get { return m_ForceMode; } set { m_ForceMode = value; } }

        private Rigidbody m_Rigidbody;


        private void OnEnable()
        {
            m_Rigidbody = GetComponentInParent<Rigidbody>();

            if (m_Strength != 0)
            {
                if (m_ForceMode == ForceMode.Impulse || m_ForceMode == ForceMode.VelocityChange)
                {
                    m_Rigidbody.AddForce(GetForce(), m_ForceMode);
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_Strength == 0)
            {
                return;
            }

            if (m_ForceMode == ForceMode.Force || m_ForceMode == ForceMode.Acceleration)
            {
                m_Rigidbody.AddForce(GetForce(), m_ForceMode);
            }
        }

        private Vector3 GetForce()
        {
            Vector3 force = m_Direction * m_Strength;
            if (m_Space == Space.Self)
            {
                force = transform.TransformVector(force);
            }
            return force;
        }
    }
}
