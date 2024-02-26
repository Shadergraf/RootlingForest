using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class RopeGenerator : MonoBehaviour
{
    [SerializeField]
    private float m_Spacing = 0.1f;
    [SerializeField]
    private GameObject m_Prefab;
    [SerializeField]
    private SplineContainer m_SplineContainer;
    [SerializeField]
    private float m_T;

    List<Rigidbody> rbs = new List<Rigidbody>();

    private void Awake()
    {
        for (int j = 0; j < m_SplineContainer.Splines.Count; j++)
        {
            Spline spline = m_SplineContainer.Splines[j];
            float length = spline.GetLength();
            int segments = MMath.CeilToInt(length / m_Spacing);

            Joint firstJoint = null;
            Joint lastJoint = null;
            for (int t = 0; t <= segments; t++)
            {
                Vector3 position = transform.TransformPoint(spline.EvaluatePosition(t / (float)segments));
                Vector3 tangent = transform.TransformDirection(spline.EvaluateTangent(t / (float)segments));

                Joint cachedJoint = lastJoint;
                lastJoint = Instantiate(m_Prefab, position, Quaternion.LookRotation(tangent, Vector3.up)).GetComponent<Joint>();
                Rigidbody rb = lastJoint.GetComponent<Rigidbody>();
                rbs.Add(rb);
                if (t == 0)
                {
                    firstJoint = lastJoint;
                }
                if (cachedJoint != null && lastJoint != null)
                {
                    cachedJoint.connectedBody = lastJoint.GetComponent<Rigidbody>();
                }
                lastJoint.GetComponent<Rigidbody>().detectCollisions = true;
            }
            if (spline.Closed)
            {
                lastJoint.connectedBody = firstJoint.GetComponent<Rigidbody>();
            }
            else
            {
                Destroy(lastJoint);
            }
        }

        //Joint lastJoint = null;
        //for (int i = 0; i < m_Elements; i++)
        //{
        //    Joint cachedJoint = lastJoint;
        //    lastJoint = Instantiate(m_Prefab, transform.position + transform.forward * i * m_Spacing, transform.rotation).GetComponent<Joint>();
        //    Rigidbody rb = lastJoint.GetComponent<Rigidbody>();
        //    rbs.Add(rb);
        //    if (cachedJoint != null && lastJoint != null)
        //    {
        //        cachedJoint.connectedBody = lastJoint.GetComponent<Rigidbody>();
        //    }
        //    lastJoint.GetComponent<Rigidbody>().detectCollisions = true;
        //}
        //Destroy(lastJoint);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(.1f);

        for (int i = 0; i < rbs.Count; i++)
        {
            rbs[i].isKinematic = false;
        }
    }

    private void Update()
    {
    }
}
