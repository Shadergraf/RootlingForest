using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ForceIncrementor : MonoBehaviour
{
    [SerializeField]
    private float m_MinImpulseMagnitude = 5;
    [SerializeField]
    private float m_ImpulseToBatteryMult = 0.1f;
    [SerializeField]
    private float m_BatteryDrain = 0.1f;

    [SerializeField]
    private UnityEvent<float> m_BatteryStateChanged;
    [SerializeField]
    private AnimationCurve m_ModulationCurve = new AnimationCurve(new Keyframe(0, 1));
    [SerializeField]
    private UnityEvent<float> m_ModulatedBatteryStateChanged;


    [SerializeField]
    [Range(0, 1)]
    private float m_Battery;


    private void FixedUpdate()
    {
        m_Battery -= m_BatteryDrain * Time.fixedDeltaTime;
        m_Battery = MMath.Clamp01(m_Battery);
        m_BatteryStateChanged.Invoke(m_Battery);
        m_ModulatedBatteryStateChanged.Invoke(m_ModulationCurve.Evaluate(m_Battery));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude > m_MinImpulseMagnitude)
        {
            ForceDetected(collision.impulse.magnitude);
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.impulse.magnitude > m_MinImpulseMagnitude)
        {
            ForceDetected(collision.impulse.magnitude);
        }
    }

    private void ForceDetected(float magnitude)
    {
        m_Battery += m_ImpulseToBatteryMult * magnitude;
        m_Battery = MMath.Clamp01(m_Battery);
    }


    private void OnGUI()
    {
        if (m_Battery != 0)
        {
            MGUI.DrawWorldProgressBar(transform.position, new Rect(0, 0, 25, 5), m_Battery, Color.yellow, Color.black);
        }
    }
}
