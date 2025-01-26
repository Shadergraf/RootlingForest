using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Manatea.GameplaySystem;

namespace Manatea.RootlingForest
{
    public class GenericForceDetector : BaseForceDetector
    {
        [SerializeField]
        private GameplayEffect[] m_EffectsToApply;
        [SerializeField]
        private GameplayEvent[] m_EventsToSend;


        protected override void ForceDetected(Vector3 force)
        {
            if (m_EffectsToApply.Length > 0)
            {
                if (EffectOwner)
                {
                    for (int i = 0; i < m_EffectsToApply.Length; i++)
                    {
                        EffectOwner.AddEffect(m_EffectsToApply[i]);
                    }
                }
                else
                {
                    Debug.LogError("No EffectOwner present on this object!", gameObject);
                }
            }

            if (m_EventsToSend.Length > 0)
            {
                if (EventReceiver)
                {
                    ForceDetectorPayload payload = new ForceDetectorPayload();
                    payload.DetectedForce = force;
                    payload.Config = Config;
                    payload.CausedByCollision = ImpactRecordedThisFrame;
                    payload.Collision = LastCollision;

                    for (int i = 0; i < m_EventsToSend.Length; i++)
                    {
                        EventReceiver.SendEventImmediate(m_EventsToSend[i], payload);
                    }
                }
                else
                {
                    Debug.LogError("No EventReceiver present on this object!", gameObject);
                }
            }
        }
    }

    public struct ForceDetectorPayload
    {
        public Vector3 DetectedForce;
        public ForceDetectorConfig Config;
        public bool CausedByCollision;
        public Collision Collision;
    }
}
