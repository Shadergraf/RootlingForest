using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using UnityEngine.Events;
using UnityEditor.PackageManager.UI;
using UnityEngine.Profiling;
using Manatea.GameplaySystem;

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
    private bool m_UseMass;
    [SerializeField]
    private GameplayAttribute m_ForceDetectionMultiplier;
    [SerializeField]
    private bool m_EnableDebugGraphs;

    [SerializeField]
    private UnityEvent m_ForceDetected;

    private Rigidbody m_Rigidbody;
    private GameplayAttributeOwner m_AttributeOwner;

    private Vector3 m_LastVelocity;
    private Vector3 m_Acceleration;
    private Vector3 m_LastAcceleration;
    private Vector3 m_Jerk;

    private float m_AccumulatedForces;


    // TODO make this debugging stuff editor only
    private const int c_CaptureSamples = 800;
    private int m_CurrentSample = 0;
    private List<float> m_VelocityGraph = new List<float>();
    private List<float> m_AccelerationGraph = new List<float>();
    private List<float> m_JerkGraph = new List<float>();
    private List<float> m_AccumulatedForcesGraph = new List<float>();
    private float m_VelocityGraphZoom = 0.15f;
    private float m_AccelerationGraphZoom = 0.6f;
    private float m_JerkGraphZoom = 0.75f;
    private float m_AccumulatedForcesGraphZoom = 0.001f;
    private bool m_GraphsInitialized;


    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AttributeOwner = GetComponent<GameplayAttributeOwner>();
    }
    private void OnEnable()
    {
        m_LastVelocity = m_Rigidbody.velocity;
        m_Acceleration = Vector3.zero;
    }

    private void FixedUpdate()
    {
        m_Acceleration = m_Rigidbody.velocity - m_LastVelocity;
        m_Jerk = m_Acceleration - m_LastAcceleration;

        m_LastVelocity = m_Rigidbody.velocity;
        m_LastAcceleration = m_Acceleration;

        m_AccumulatedForces = MMath.Damp(m_AccumulatedForces, 0, 16, Time.fixedDeltaTime);
        m_AccumulatedForces += MMath.Pow(m_Jerk.magnitude, 2);

        float finalForce = m_AccumulatedForces;
        if (m_AttributeOwner)
        {
            if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_ForceDetectionMultiplier, out float val))
            {
                finalForce *= val;
            }
        }

        if (!m_DisableDetection && finalForce > m_ImpulseMagnitude)
        {
            ForceDetected();
        }

        if (m_EnableDebugGraphs)
        {
            if (!m_GraphsInitialized)
            {
                m_VelocityGraph.Clear();
                m_AccelerationGraph.Clear();
                m_JerkGraph.Clear();
                m_AccumulatedForcesGraph.Clear();
                for (int i = 0; i < c_CaptureSamples; i++)
                {
                    m_VelocityGraph.Add(0);
                    m_AccelerationGraph.Add(0);
                    m_JerkGraph.Add(0);
                    m_AccumulatedForcesGraph.Add(0);
                }
                m_GraphsInitialized = true;
            }

            m_CurrentSample++;
            m_CurrentSample %= c_CaptureSamples;
            m_VelocityGraph[m_CurrentSample] = m_Rigidbody.velocity.magnitude * Time.fixedDeltaTime;
            m_AccelerationGraph[m_CurrentSample] = m_Acceleration.magnitude * Time.fixedDeltaTime;
            m_JerkGraph[m_CurrentSample] = m_Jerk.magnitude * Time.fixedDeltaTime;

            m_AccumulatedForcesGraph[m_CurrentSample] = finalForce;
        }
    }

    //private void OnCollisionStay(Collision collision)
    //{
    //    if (!collision.rigidbody || collision.rigidbody.isKinematic)
    //    {
    //        m_AccumulatedForces += MMath.Pow(collision.relativeVelocity.magnitude, 2);
    //    }
    //}

    private void ForceDetected()
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
            rigid.velocity += forceDir * (m_Force);
        }

        if (m_DestroyObject)
        {
            Destroy(gameObject);
        }
    }

    public void OnGUI()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        if (!m_EnableDebugGraphs)
        {
            return;
        }
        if (!m_GraphsInitialized)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(Screen.width - 200, 0, 200, 400));

        m_VelocityGraphZoom = GUILayout.HorizontalSlider(m_VelocityGraphZoom, 0, 1);
        m_AccelerationGraphZoom = GUILayout.HorizontalSlider(m_AccelerationGraphZoom, 0, 1);
        m_JerkGraphZoom = GUILayout.HorizontalSlider(m_JerkGraphZoom, 0, 1);
        m_AccumulatedForcesGraphZoom = GUILayout.HorizontalSlider(m_AccumulatedForcesGraphZoom, 0, 0.1f);

        GUILayout.EndArea();
    }
    public void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        if (!m_EnableDebugGraphs)
        {
            return;
        }
        if (!m_GraphsInitialized)
        {
            return;
        }

        Camera cam = Camera.main;

        float xPos = 0.1f;
        float yPos = 0.5f;
        float zPos = 0.1f;

        float time = 0.2f * 0.005f;

        // Draw graph
        Debug.DrawLine(
            cam.ViewportToWorldPoint(new Vector3(xPos, 0.15f, zPos)), 
            cam.ViewportToWorldPoint(new Vector3(0.1f, 0.85f, zPos)));
        Debug.DrawLine(
            cam.ViewportToWorldPoint(new Vector3(xPos, 0.5f, zPos)), 
            cam.ViewportToWorldPoint(new Vector3(0.9f, 0.5f, zPos)));

        // Draw current time
        Debug.DrawLine(
            cam.ViewportToWorldPoint(new Vector3(xPos + m_CurrentSample * time, 0.15f, zPos)), 
            cam.ViewportToWorldPoint(new Vector3(xPos + m_CurrentSample * time, 0.85f, zPos)), 
            new Color(0, 0, 0, 0.5f));

        float maxD = float.NegativeInfinity;
        float maxE = float.NegativeInfinity;
        for (int i = 0; i < c_CaptureSamples - 1; i++)
        {
            // Break continous graph on current time sample
            if (i == m_CurrentSample)
            {
                continue;
            }

            // Graph velocity
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_VelocityGraph[i] * m_VelocityGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_VelocityGraph[i + 1] * m_VelocityGraphZoom, zPos)), 
                Color.red);
            // Graph acceleration
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_AccelerationGraph[i] * m_AccelerationGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_AccelerationGraph[i + 1] * m_AccelerationGraphZoom, zPos)), 
                Color.green);

            // Graph jerk
            maxD = MMath.Max(maxD, MMath.Abs(m_JerkGraph[i]));
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_JerkGraph[i] * m_JerkGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_JerkGraph[i + 1] * m_JerkGraphZoom, zPos)), 
                Color.blue);

            // Graph accumulated forces
            maxE = MMath.Max(maxE, MMath.Abs(m_AccumulatedForcesGraph[i]));
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_AccumulatedForcesGraph[i] * m_AccumulatedForcesGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_AccumulatedForcesGraph[i + 1] * m_AccumulatedForcesGraphZoom, zPos)), 
                Color.yellow);
        }

        // Max jerk line
        if (!float.IsInfinity(maxD))
        {
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(0.1f, yPos + maxD * m_JerkGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + maxD * m_JerkGraphZoom, zPos)), 
                new Color(1, 1, 1, 0.5f));
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(0.1f, yPos - maxD * m_JerkGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(0.9f, yPos - maxD * m_JerkGraphZoom, zPos)), 
                new Color(1, 1, 1, 0.5f));
        }
        // Max accumulated forces line
        if (!float.IsInfinity(maxE))
        {
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(0.1f, yPos + maxE * m_AccumulatedForcesGraphZoom, zPos)), 
                cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + maxE * m_AccumulatedForcesGraphZoom, zPos)), 
                new Color(1, 1, 1, 0.2f));
        }

        // Cross this line with accumulated forces to trigger the break response
        Debug.DrawLine(
            cam.ViewportToWorldPoint(new Vector3(xPos, yPos + m_ImpulseMagnitude * m_AccumulatedForcesGraphZoom, zPos)), 
            cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + m_ImpulseMagnitude * m_AccumulatedForcesGraphZoom, zPos)), 
            m_DisableDetection ? Color.red : Color.green);

    }
}
