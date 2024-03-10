using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsDebugger : MonoBehaviour
{
    private Rigidbody m_GrabObject;
    private Vector3 m_GrabPosLocal;
    private Vector3 m_InitialGrabPosGlobal;
    private Vector3 m_CurrentGrabPosGlobal;

    private SpringJoint m_Spring;

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                m_GrabObject = hit.collider.GetComponentInParent<Rigidbody>();
                if (m_GrabObject != null)
                {
                    m_GrabPosLocal = m_GrabObject.transform.InverseTransformPoint(hit.point);
                    m_InitialGrabPosGlobal = hit.point;
                    m_CurrentGrabPosGlobal = m_InitialGrabPosGlobal;

                    GameObject springGO = new GameObject("DebugPhysicsSpring");
                    springGO.transform.position = hit.point;
                    m_Spring = springGO.AddComponent<SpringJoint>();
                    m_Spring.connectedBody = m_GrabObject;
                    m_Spring.autoConfigureConnectedAnchor = false;
                    m_Spring.anchor = Vector3.zero;
                    m_Spring.connectedAnchor = m_GrabPosLocal;

                    m_Spring.spring = 100;
                    m_Spring.damper = 40;

                    m_Spring.GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }
        if (Input.GetMouseButtonUp(2))
        {
            if (m_Spring)
            {
                Destroy(m_Spring.gameObject);
            }

            m_GrabObject = null;
            m_Spring = null;
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetMouseButton(2) && m_GrabObject)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 planeNormal = Camera.main.transform.forward.FlattenY().normalized;
            if (Input.GetKey(KeyCode.LeftControl))
                planeNormal = Vector3.up;
            Plane plane = new Plane(planeNormal, m_CurrentGrabPosGlobal);
            if (plane.Raycast(ray, out float dist))
            {
                m_CurrentGrabPosGlobal = ray.GetPoint(dist);
                DebugHelper.DrawWireSphere(m_CurrentGrabPosGlobal, 0.1f, Color.blue, Time.fixedDeltaTime);
                DebugHelper.DrawWireSphere(m_Spring.connectedBody.transform.TransformPoint(m_Spring.connectedAnchor), 0.1f, Color.red, Time.fixedDeltaTime);
                DebugHelper.DrawWireSphere(m_Spring.transform.TransformPoint(m_Spring.anchor), 0.1f, Color.green, Time.fixedDeltaTime);

                m_Spring.transform.position = m_CurrentGrabPosGlobal;
            }

            // TODO use a spawned gameobject to create a spring joint
        }
    }




    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(2))
    //    {
    //        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //        if (Physics.Raycast(ray, out RaycastHit hit))
    //        {
    //            m_GrabObject = hit.collider.GetComponentInParent<Rigidbody>();
    //            if (m_GrabObject != null)
    //            {
    //                m_GrabPosLocal = m_GrabObject.transform.InverseTransformPoint(hit.point);
    //                m_InitialGrabPosGlobal = hit.point;
    //            }
    //        }
    //    }
    //    if (Input.GetMouseButtonUp(2))
    //    {
    //    }
    //}

    //private void FixedUpdate()
    //{
    //    if (Input.GetMouseButton(2) && m_GrabObject)
    //    {
    //        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //        Plane plane = new Plane(Camera.main.transform.forward.FlattenY().normalized, m_InitialGrabPosGlobal);
    //        if (plane.Raycast(ray, out float dist))
    //        {
    //            Vector3 targetPoint = ray.GetPoint(dist);
    //            DebugHelper.DrawWireSphere(targetPoint, 0.1f, Color.red, Time.fixedDeltaTime);
    //            Vector3 startPoint = m_GrabObject.transform.TransformPoint(m_GrabPosLocal);
    //            DebugHelper.DrawWireSphere(startPoint, 0.1f, Color.green, Time.fixedDeltaTime);
    //            Vector3 force = targetPoint - startPoint;

    //            m_GrabObject.AddForceAtPosition(force * 5, m_GrabObject.transform.TransformPoint(m_GrabPosLocal));
    //        }


    //        // TODO use a spawned gameobject to create a spring joint
    //    }
    //}
}
