using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.AdventureRoots.Cameras
{
    [ExecuteInEditMode]
    public class TopDownCameraManager : MonoBehaviour
    {
        public GameObject Target;
        public float Speed = 1;

        private Vector3 m_LastTarget;


        private void OnEnable()
        {
            m_LastTarget = transform.position;

            if (Target)
            {
                m_LastTarget = Target.transform.position;
            }
            transform.position = m_LastTarget;
        }

        private void Update()
        {
            if (Target)
                m_LastTarget = Target.transform.position;

            if (Speed > 0)
            {
                transform.position = MMath.Damp(transform.position, m_LastTarget, Speed, Time.deltaTime);
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
