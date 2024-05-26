using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using UnityEngine.Events;
using UnityEditor.PackageManager.UI;
using UnityEngine.Profiling;

public class ForceDetector : MonoBehaviour
{
    [SerializeField]
    private float m_ImpulseMagnitude = 5;
    [SerializeField]
    private bool m_DisableDetection = false;
    [SerializeField]
    private GameObject[] m_SpawnObjects;
    [SerializeField]
    private bool m_DestroyObject;
    [SerializeField]
    private float m_Force;
    [SerializeField, Range(0, 1)]
    private float m_RadialForce = 1;
    [SerializeField]
    private float m_ImpulseForceContribution = 0;
    [SerializeField]
    private bool m_UseMass;

    [SerializeField]
    private UnityEvent m_ForceDetected;

    private Rigidbody m_Rigidbody;

    private Vector3 m_LastVelocity;
    private Vector3 m_Acceleration;
    private Vector3 m_LastAcceleration;
    private Vector3 m_Jerk;

    private float m_AccumulatedForces;


    List<float> m_GraphA = new List<float>();
    List<float> m_GraphB = new List<float>();
    List<float> m_GraphC = new List<float>();
    List<float> m_GraphD = new List<float>();
    List<float> m_GraphE = new List<float>();


    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();

        for (int i = 0; i < 800; i++)
        {
            m_GraphA.Add(0);
            m_GraphB.Add(0);
            m_GraphC.Add(0);
            m_GraphD.Add(0);
            m_GraphE.Add(0);
        }
    }
    private void OnEnable()
    {
        m_LastVelocity = m_Rigidbody.velocity;
        m_Acceleration = Vector3.zero;
    }

    public int sample = 0;
    private void FixedUpdate()
    {
        m_Acceleration = m_Rigidbody.velocity - m_LastVelocity;
        m_Jerk = m_Acceleration - m_LastAcceleration;

        m_LastVelocity = m_Rigidbody.velocity;
        m_LastAcceleration = m_Acceleration;

        m_AccumulatedForces = MMath.Damp(m_AccumulatedForces, 0, 12, Time.fixedDeltaTime);
        m_AccumulatedForces += m_Jerk.magnitude;

        sample++;
        if (sample >= 800)
        {
            sample = 0;
        }
        m_GraphA[sample] = m_Rigidbody.position.y * 0.15f;
        m_GraphB[sample] = m_Rigidbody.velocity.y * Time.fixedDeltaTime * 0.15f;
        m_GraphC[sample] = m_Acceleration.y * Time.fixedDeltaTime * 4 * 0.15f;
        m_GraphD[sample] = m_Jerk.y * Time.fixedDeltaTime * 5 * 0.15f;

        m_GraphE[sample] = m_AccumulatedForces * 0.005f;
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
            Vector3 forceDir = Vector3.Lerp(randomDir, radialDir, m_RadialForce).normalized;
            //rigid.AddForce(force * m_Force, m_UseMass ? ForceMode.Impulse : ForceMode.VelocityChange);
            rigid.velocity += forceDir * (m_Force + m_AccumulatedForces * m_ImpulseForceContribution);
        }

        if (m_DestroyObject)
        {
            Destroy(gameObject);
        }
    }

    public void OnDrawGizmosSelected()
    {
        Camera cam = Camera.main;

        float xPos = 0.1f;
        float yPos = 0.5f;
        float zPos = 0.1f;

        float time = 0.2f * 0.005f;

        Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos, 0.15f, zPos)), cam.ViewportToWorldPoint(new Vector3(0.1f, 0.85f, zPos)));
        Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos, 0.5f, zPos)), cam.ViewportToWorldPoint(new Vector3(0.9f, 0.5f, zPos)));

        float maxD = float.NegativeInfinity;
        float maxE = float.NegativeInfinity;

        for (int i = 0; i < m_GraphA.Count - 1; i++)
        {
            if (i == sample)
            {
                continue;
            }

            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_GraphA[i], zPos)), cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_GraphA[i + 1], zPos)), Color.red);

            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_GraphB[i], zPos)), cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_GraphB[i + 1], zPos)), Color.green);

            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_GraphC[i], zPos)), cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_GraphC[i + 1], zPos)), Color.blue);


            maxD = MMath.Max(maxD, MMath.Abs(m_GraphD[i]));
            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_GraphD[i], zPos)), cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_GraphD[i + 1], zPos)), Color.yellow);


            maxE = MMath.Max(maxE, MMath.Abs(m_GraphE[i]));
            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_GraphE[i], zPos)), cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_GraphE[i + 1], zPos)), Color.cyan);
        }

        if (!float.IsInfinity(maxD))
        {
            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(0.1f, yPos + maxD, zPos)), cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + maxD, zPos)), new Color(1, 1, 1, 0.5f));
            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(0.1f, yPos - maxD, zPos)), cam.ViewportToWorldPoint(new Vector3(0.9f, yPos - maxD, zPos)), new Color(1, 1, 1, 0.5f));
        }
        if (!float.IsInfinity(maxE))
        {
            Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(0.1f, yPos + maxE, zPos)), cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + maxE, zPos)), new Color(1, 1, 1, 0.2f));
        }

        Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos, yPos + m_ImpulseMagnitude * 0.005f, zPos)), cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + m_ImpulseMagnitude * 0.005f, zPos)), m_DisableDetection ? Color.red : Color.green);

        Debug.DrawLine(cam.ViewportToWorldPoint(new Vector3(xPos + sample * time, 0.15f, zPos)), cam.ViewportToWorldPoint(new Vector3(xPos + sample * time, 0.85f, zPos)), new Color(0, 0, 0, 0.5f));
    }
}
