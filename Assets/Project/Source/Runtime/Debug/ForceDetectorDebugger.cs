using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [DefaultExecutionOrder(10000)]
    public class ForceDetectorDebugger : MonoBehaviour
    {
        public bool m_ShowDebugWindow = true;
        public BaseForceDetector m_DebugForceDetector;

        // TODO make this debugging stuff editor only
        private const int c_CaptureSamples = 800;
        private int m_CurrentSample = 0;
        private List<float> m_VelocityGraph = new List<float>();
        private List<float> m_AccelerationGraph = new List<float>();
        private List<float> m_JerkGraph = new List<float>();
        private List<float> m_AccumulatedForcesGraph = new List<float>();
        private List<float> m_ContactImpulseGraph = new List<float>();
        private List<float> m_ContactVelocityGraph = new List<float>();
        private float m_VelocityGraphZoom = 0;
        private float m_AccelerationGraphZoom = 0;
        private float m_JerkGraphZoom = 0;
        private float m_AccumulatedForcesGraphZoom = 0;
        private float m_ContactImpulseGraphZoom = 0;
        private float m_ContactVelocityGraphZoom = 0;

        private void OnEnable()
        {
            for (int i = 0; i < c_CaptureSamples; i++)
            {
                m_VelocityGraph.Add(0);
                m_AccelerationGraph.Add(0);
                m_JerkGraph.Add(0);
                m_AccumulatedForcesGraph.Add(0);
                m_ContactImpulseGraph.Add(0);
                m_ContactVelocityGraph.Add(0);
            }
        }
        private void OnDisable()
        {
            m_VelocityGraph.Clear();
            m_AccelerationGraph.Clear();
            m_JerkGraph.Clear();
            m_AccumulatedForcesGraph.Clear();
            m_ContactImpulseGraph.Clear();
            m_ContactVelocityGraph.Clear();
        }

        private void FixedUpdate()
        {
            if (!m_DebugForceDetector || !m_ShowDebugWindow)
            {
                return;
            }

            m_CurrentSample++;
            m_CurrentSample %= c_CaptureSamples;

            m_VelocityGraph[m_CurrentSample] = m_DebugForceDetector.Velocity.magnitude;
            m_AccelerationGraph[m_CurrentSample] = m_DebugForceDetector.Acceleration.magnitude;
            m_JerkGraph[m_CurrentSample] = m_DebugForceDetector.Jerk.magnitude;

            m_ContactImpulseGraph[m_CurrentSample] = m_DebugForceDetector.ContactImpulse.magnitude;
            m_ContactVelocityGraph[m_CurrentSample] = m_DebugForceDetector.ContactVelocity.magnitude;

            m_AccumulatedForcesGraph[m_CurrentSample] = m_DebugForceDetector.FinalForce.magnitude;
        }

        public void OnGUI()
        {
            if (!m_DebugForceDetector || !m_ShowDebugWindow)
            {
                return;
            }
            if (!Application.isPlaying)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 200, 0, 200, 400));

            m_VelocityGraphZoom = GUILayout.HorizontalSlider(m_VelocityGraphZoom, 0, 0.1f);
            m_AccelerationGraphZoom = GUILayout.HorizontalSlider(m_AccelerationGraphZoom, 0, 0.01f);
            m_JerkGraphZoom = GUILayout.HorizontalSlider(m_JerkGraphZoom, 0, 0.001f);
            GUILayout.Space(10);
            m_ContactImpulseGraphZoom = GUILayout.HorizontalSlider(m_ContactImpulseGraphZoom, 0, 0.0001f);
            m_ContactVelocityGraphZoom = GUILayout.HorizontalSlider(m_ContactVelocityGraphZoom, 0, 0.001f);
            GUILayout.Space(10);
            m_AccumulatedForcesGraphZoom = GUILayout.HorizontalSlider(m_AccumulatedForcesGraphZoom, 0, 0.0001f);

            GUILayout.EndArea();
        }
        public void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (!m_DebugForceDetector || !m_ShowDebugWindow)
            {
                return;
            }

            Camera cam = Camera.main;

            const float xPos = 0.1f;
            const float yPos = 0.15f;
            const float zPos = 0.1f;

            const float time = 0.2f * 0.005f;

            // Draw graph
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(xPos, 0.15f, zPos)),
                cam.ViewportToWorldPoint(new Vector3(0.1f, 0.85f, zPos)));
            Debug.DrawLine(
                cam.ViewportToWorldPoint(new Vector3(xPos, yPos, zPos)),
                cam.ViewportToWorldPoint(new Vector3(0.9f, yPos, zPos)));

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

                // Graph accumulated forces
                Debug.DrawLine(
                    cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_ContactImpulseGraph[i] * m_ContactImpulseGraphZoom, zPos)),
                    cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_ContactImpulseGraph[i + 1] * m_ContactImpulseGraphZoom, zPos)),
                    Color.black);
                // Graph accumulated forces
                Debug.DrawLine(
                    cam.ViewportToWorldPoint(new Vector3(xPos + i * time, yPos + m_ContactVelocityGraph[i] * m_ContactVelocityGraphZoom, zPos)),
                    cam.ViewportToWorldPoint(new Vector3(xPos + (i + 1) * time, yPos + m_ContactVelocityGraph[i + 1] * m_ContactVelocityGraphZoom, zPos)),
                    Color.white);
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
                cam.ViewportToWorldPoint(new Vector3(xPos, yPos + m_DebugForceDetector.Config.MinImpulseMagnitude * m_AccumulatedForcesGraphZoom, zPos)),
                cam.ViewportToWorldPoint(new Vector3(0.9f, yPos + m_DebugForceDetector.Config.MinImpulseMagnitude * m_AccumulatedForcesGraphZoom, zPos)),
                m_DebugForceDetector.DisableDetection ? Color.red : Color.green);

        }
    }
}
