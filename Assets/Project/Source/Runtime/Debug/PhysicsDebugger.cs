using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatea.RootlingForest
{

#if DEBUG

    public class PhysicsDebugger : MonoBehaviour
    {
        [SerializeField]
        public Camera m_InventoryCam;
        [SerializeField]
        public float m_SpringForce = 100;
        [SerializeField]
        public float m_Damper = 40;
        [SerializeField]
        public float m_BreakForce = 1000;

        private Rigidbody m_GrabObject;
        private Vector3 m_GrabPosLocal;
        private Vector3 m_InitialGrabPosGlobal;
        private Vector3 m_CurrentGrabPosGlobal;

        private SpringJoint m_Spring;

        private Camera m_CurrentCamera;


        private void Start()
        {

        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(2))
            {
                if (m_InventoryCam && !m_GrabObject)
                {
                    HandleKeyDown(m_InventoryCam);
                    m_CurrentCamera = m_InventoryCam;
                }
                if (!m_GrabObject)
                {
                    HandleKeyDown(Camera.main);
                    m_CurrentCamera = Camera.main;
                }
            }

            if (Input.GetMouseButtonUp(2))
            {
                DestroyConnection();
            }
        }

        private void HandleKeyDown(Camera cam)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject currentTestObj = hit.collider.gameObject;
                while (currentTestObj != null)
                {
                    m_GrabObject = currentTestObj.GetComponentInParent<Rigidbody>();
                    if (m_GrabObject != null && !m_GrabObject.isKinematic)
                    {
                        m_GrabPosLocal = m_GrabObject.transform.InverseTransformPoint(hit.point);
                        m_InitialGrabPosGlobal = hit.point;
                        m_CurrentGrabPosGlobal = m_InitialGrabPosGlobal;

                        GameObject springGO = new GameObject("DebugPhysicsSpring");
                        //springGO.hideFlags = HideFlags.HideAndDontSave;

                        springGO.transform.position = hit.point;
                        m_Spring = springGO.AddComponent<SpringJoint>();
                        m_Spring.connectedBody = m_GrabObject;
                        m_Spring.autoConfigureConnectedAnchor = false;
                        m_Spring.anchor = Vector3.zero;
                        m_Spring.connectedAnchor = m_GrabPosLocal;

                        m_Spring.spring = m_SpringForce;
                        m_Spring.damper = m_Damper;
                        m_Spring.breakForce = m_BreakForce;

                        m_Spring.GetComponent<Rigidbody>().isKinematic = true;
                        break;
                    }

                    if (currentTestObj.transform.parent)
                    {
                        currentTestObj = currentTestObj.transform.parent.gameObject;
                    }
                    else
                    {
                        currentTestObj = null;
                    }
                    if (currentTestObj == null)
                    {
                        m_GrabObject = null;
                        break;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_Spring)
            {
                m_Spring.spring = m_SpringForce;
                m_Spring.damper = m_Damper;
                m_Spring.breakForce = m_BreakForce;
            }    

            if (Input.GetMouseButton(2) && m_GrabObject)
            {
                Ray ray = m_CurrentCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 planeNormal = m_CurrentCamera.transform.forward.FlattenY().normalized;
                if (Input.GetKey(KeyCode.LeftControl))
                    planeNormal = Vector3.up;
                Plane plane = new Plane(planeNormal, m_CurrentGrabPosGlobal);
                if (plane.Raycast(ray, out float dist))
                {
                    m_CurrentGrabPosGlobal = ray.GetPoint(dist);
                    DebugHelper.DrawWireSphere(m_CurrentGrabPosGlobal, 0.1f, Color.blue, Time.fixedDeltaTime);
                    DebugHelper.DrawWireSphere(m_Spring.connectedBody.transform.TransformPoint(m_Spring.connectedAnchor), 0.1f, Color.red, Time.fixedDeltaTime);
                    DebugHelper.DrawWireSphere(m_Spring.transform.TransformPoint(m_Spring.anchor), 0.1f, Color.green, Time.fixedDeltaTime);

                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        m_Spring.transform.position = m_CurrentGrabPosGlobal;
                    }
                }

                // TODO use a spawned gameobject to create a spring joint
            }
        }

        public void DestroyConnection()
        {
            if (m_Spring)
            {
                Destroy(m_Spring.gameObject);
            }

            m_GrabObject = null;
            m_Spring = null;
        }
    }

#endif
}
