using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.RootlingForest
{
    public class ForceIncrementor : MonoBehaviour
    {
        [SerializeField]
        private float m_MinImpulseMagnitude = 5;
        [SerializeField]
        private float m_ImpulseToBatteryMult = 0.1f;
        [SerializeField]
        private float m_MinAccelerationMagnitude = 5;
        [SerializeField]
        private float m_AccelerationToBatteryMult = 0.1f;
        [SerializeField]
        private float m_BatteryDrain = 0.1f;

        [SerializeField]
        private GameplayAttribute m_BatteryAttribute;
        [SerializeField]
        private GameplayAttribute m_ForceMultiplier;

        [SerializeField]
        private UnityEvent<float> m_BatteryStateChanged;
        [SerializeField]
        private AnimationCurve m_ModulationCurve = new AnimationCurve(new Keyframe(0, 1));
        [SerializeField]
        private UnityEvent<float> m_ModulatedBatteryStateChanged;

        [SerializeField]
        [Range(0, 1)]
        private float m_Battery;

        private Rigidbody m_Rigid;
        private Vector3 m_LastVelocity;
        private Vector3 m_Acceleration;
        private Vector3 m_LastAcceleration;
        private Vector3 m_Jerk;


        private void Start()
        {
            m_Rigid = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            m_Battery -= m_BatteryDrain * Time.fixedDeltaTime;
            m_Battery = MMath.Clamp01(m_Battery);
            m_BatteryStateChanged.Invoke(m_Battery);
            m_ModulatedBatteryStateChanged.Invoke(m_ModulationCurve.Evaluate(m_Battery));

            m_Acceleration = (m_Rigid.velocity - m_LastVelocity) / Time.fixedDeltaTime;
            m_Jerk = (m_Acceleration - m_LastAcceleration) / Time.fixedDeltaTime;

            if (m_Jerk.magnitude >= m_MinAccelerationMagnitude)
            {
                ForceDetected(m_Jerk.magnitude * m_AccelerationToBatteryMult * Time.fixedDeltaTime);
            }

            m_LastVelocity = m_Rigid.velocity;
            m_LastAcceleration = m_Acceleration;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > m_MinImpulseMagnitude)
            {
                ForceDetected(collision.impulse.magnitude * m_ImpulseToBatteryMult);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if (collision.impulse.magnitude > m_MinImpulseMagnitude)
            {
                ForceDetected(collision.impulse.magnitude * m_ImpulseToBatteryMult);
            }
        }

        private void ForceDetected(float magnitude)
        {
            m_Battery += magnitude;
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
}
