using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using UnityEngine.Events;

public class ForceDetector : MonoBehaviour
{
    [SerializeField]
    private float m_ImpulseMagnitude = 5;
    [SerializeField]
    private GameObject[] m_SpawnObjects;
    [SerializeField]
    private bool m_DestroyObject;
    [SerializeField]
    private float m_Force;
    [SerializeField, Range(0, 1)]
    private float m_RadialForce = 1;
    [SerializeField]
    private bool m_UseMass;

    [SerializeField]
    private UnityEvent m_ForceDetected;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude > m_ImpulseMagnitude)
        {
            ForceDetected();
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.impulse.magnitude > m_ImpulseMagnitude)
        {
            ForceDetected();
        }
    }

    public void ForceDetected()
    {
        m_ForceDetected.Invoke();

        Rigidbody sourceRigid = GetComponent<Rigidbody>();
        for (int i = 0; i < m_SpawnObjects.Length; ++i)
        {
            m_SpawnObjects[i].SetActive(true);
            m_SpawnObjects[i].transform.SetParent(transform.parent);
            Rigidbody rigid = m_SpawnObjects[i].GetComponent<Rigidbody>();
            rigid.velocity = sourceRigid.velocity;
            rigid.angularVelocity = sourceRigid.angularVelocity;

            Vector3 randomDir = Random.onUnitSphere;
            Vector3 radialDir = rigid.position - sourceRigid.position;
            if (radialDir.magnitude < 0.001f)
            {
                radialDir = randomDir;
            }
            Vector3 force = Vector3.Lerp(randomDir, radialDir, m_RadialForce).normalized;
            //rigid.AddForce(force * m_Force, m_UseMass ? ForceMode.Impulse : ForceMode.VelocityChange);
            rigid.velocity += force * m_Force;
        }

        if (m_DestroyObject)
        {
            Destroy(gameObject);
        }
    }
}
