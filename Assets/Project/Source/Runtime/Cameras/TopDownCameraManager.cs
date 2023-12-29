using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.AdventureRoots.Cameras
{
    public class TopDownCameraManager : MonoBehaviour
    {
        public GameObject Target;
        public float Speed = 1;

        private Vector3 m_LastTarget;


        private void OnEnable()
        {
            m_LastTarget = transform.position;
        }

        private void Update()
        {
            if (Target)
                m_LastTarget = Target.transform.position;

            transform.position = MMath.Damp(transform.position, m_LastTarget, Speed, Time.deltaTime);
        }
    }
}
