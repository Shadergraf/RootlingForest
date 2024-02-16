using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullOut : MonoBehaviour
{
    [SerializeField]
    private ConfigurableJoint m_Joint;
    [SerializeField]
    private float m_PullOutDistance = 1;


    private void FixedUpdate()
    {
        float pulloutDist = Vector3.Distance(m_Joint.transform.TransformPoint(m_Joint.anchor), m_Joint.connectedBody.transform.TransformPoint(m_Joint.connectedAnchor));
        if (pulloutDist > m_PullOutDistance)
        {
            Destroy(m_Joint);
        }
    }
}
