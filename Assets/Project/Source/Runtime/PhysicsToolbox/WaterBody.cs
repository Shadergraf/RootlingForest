using Manatea.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBody : MonoBehaviour
{
    [SerializeField]
    private float m_WaterLine = 0;
    [SerializeField]
    private float m_WaterDensity = 1;
    [SerializeField]
    private float m_LinearDrag = 0;
    [SerializeField]
    private float m_AngularDrag = 0;
    
    private void FixedUpdate()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.attachedRigidbody || other.attachedRigidbody.isKinematic)
        {
            return;
        }

        float waterLine = transform.TransformPoint(Vector3.up *  m_WaterLine).y;
        Vector3[] corners = other.bounds.GetCorners();
        for (int i = 0; i < corners.Length; i++)
        {
            other.attachedRigidbody.AddForceAtPosition(Vector3.up * (waterLine - corners[i].y) * m_WaterDensity, corners[i], ForceMode.Acceleration);
        }
        other.attachedRigidbody.AddForceAtPosition(Vector3.up * (waterLine - other.attachedRigidbody.position.y) * m_WaterDensity, other.attachedRigidbody.position, ForceMode.Acceleration);


        other.attachedRigidbody.velocity *= Mathf.Clamp01(1 - m_LinearDrag * Time.fixedDeltaTime);
        other.attachedRigidbody.angularVelocity *= Mathf.Clamp01(1 - m_AngularDrag * Time.fixedDeltaTime);
    }
}
