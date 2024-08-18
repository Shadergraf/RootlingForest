using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    [ExecuteInEditMode]
    public class TopDownCameraManager : MonoBehaviour
    {
        [FormerlySerializedAs("Target")]
        [SerializeField]
        public GameObject m_Target;
        [FormerlySerializedAs("Speed")]
        [SerializeField]
        public float m_Speed = 1;

        private Vector3 m_LastTarget;


        private void OnEnable()
        {
            m_LastTarget = transform.position;

            if (m_Target)
            {
                m_LastTarget = m_Target.transform.position;
            }
            transform.position = m_LastTarget;
        }

        private void Update()
        {
            if (m_Target)
                m_LastTarget = m_Target.transform.position;

            if (m_Speed > 0)
            {
                transform.position = MMath.Damp(transform.position, m_LastTarget, m_Speed, Time.deltaTime);
            }
            else
            {
                transform.position = m_LastTarget;
            }

            if (!Application.isPlaying)
            {
                transform.position = m_LastTarget;
            }
        }
    }
}
