using System.Collections;
using UnityEngine;
using Manatea.GameplaySystem;

namespace Manatea.RootlingForest
{
    public class BaseForceDetector : MonoBehaviour
    {
        #region Serialized Vars

        [SerializeField]
        private ForceDetectorConfig m_Config;
        [SerializeField]
        private bool m_DisableDetection = false;
        [SerializeField]
        private GameplayTagFilter m_Filter;

        #endregion

        #region Public Vars

        public ForceDetectorConfig Config => m_Config;
        public bool DisableDetection => m_DisableDetection;
        public Vector3 Velocity => m_Rigidbody.linearVelocity;
        public Vector3 Acceleration => m_Acceleration;
        public Vector3 Jerk => m_Jerk;
        public Vector3 FinalForce => m_FinalForce;
        public Vector3 ContactImpulse => m_ContactImpulse;
        public Vector3 ContactVelocity => m_ContactVelocity;

        #endregion

        #region Protected Vars

        protected Rigidbody Rigidbody => m_Rigidbody;
        protected GameplayAttributeOwner AttributeOwner => m_AttributeOwner;
        protected GameplayTagOwner TagOwner => m_TagOwner;
        protected GameplayEffectOwner EffectOwner => m_EffectOwner;
        protected GameplayEventReceiver EventReceiver => m_EventReceiver;

        protected Collision LastCollision => m_LastCollision;
        protected bool ImpactRecordedThisFrame => m_ImpactRecordedThisFrame;

        #endregion

        #region Private Vars

        private Rigidbody m_Rigidbody;
        private OnCollisionEnterCallbackComponent m_CollisionEventSender;

        private GameplayAttributeOwner m_AttributeOwner;
        private GameplayTagOwner m_TagOwner;
        private GameplayEffectOwner m_EffectOwner;
        private GameplayEventReceiver m_EventReceiver;

        private Vector3 m_LastVelocity;
        private Vector3 m_Acceleration;
        private Vector3 m_LastAcceleration;
        private Vector3 m_Jerk;
        private Vector3 m_ContactImpulse;
        private Vector3 m_ContactVelocity;
        private Vector3 m_FinalForce;

        private Vector3 m_AccumulatedForces;

        private Collision m_LastCollision;

        private bool m_DamageTimeout;
        private bool m_ImpactRecordedThisFrame;

        #endregion


        #region Unity Events

        protected virtual void Awake()
        {
            m_Rigidbody = GetComponentInParent<Rigidbody>();
            m_AttributeOwner = GetComponentInParent<GameplayAttributeOwner>();
            m_TagOwner = GetComponentInParent<GameplayTagOwner>();
            m_EffectOwner = GetComponentInParent<GameplayEffectOwner>();
            m_EventReceiver = GetComponentInParent<GameplayEventReceiver>();

            m_CollisionEventSender = m_Rigidbody.gameObject.AddComponent<OnCollisionEnterCallbackComponent>();
        }
        protected virtual void OnEnable()
        {
            m_LastVelocity = m_Rigidbody.linearVelocity;
            m_Acceleration = Vector3.zero;

            Debug.Assert(m_CollisionEventSender, "CollisionEventSender needs to be present in parent!", gameObject);
            m_CollisionEventSender.OnCollisionEnterEvent += OnCollisionEnterEvent;
        }
        protected virtual void OnDisable()
        {
            if (m_CollisionEventSender)
                m_CollisionEventSender.OnCollisionEnterEvent -= OnCollisionEnterEvent;
        }
        private void OnDestroy()
        {
            if (m_CollisionEventSender)
                Destroy(m_CollisionEventSender);
        }


        protected virtual void OnCollisionEnterEvent(Collision collision)
        {
            ContactResponse(collision);
        }
        //private void OnCollisionStay(Collision collision)
        //{
        //    ContactResponse(collision);
        //}

        protected virtual void FixedUpdate()
        {
            m_Acceleration = (m_Rigidbody.linearVelocity - m_LastVelocity) / Time.fixedDeltaTime;
            m_Jerk = (m_Acceleration - m_LastAcceleration) / Time.fixedDeltaTime;

            m_LastVelocity = m_Rigidbody.linearVelocity;
            m_LastAcceleration = m_Acceleration;

            m_AccumulatedForces = MMath.Damp(m_AccumulatedForces, Vector3.zero, m_Config.ImpulseTimeFalloff, Time.fixedDeltaTime);
            m_AccumulatedForces += m_Jerk * m_Config.JerkInfluence;
            m_AccumulatedForces += m_ContactImpulse;
            m_AccumulatedForces += m_ContactVelocity;

            // Persist the contact variables for one fixedUpdate step
            if (!m_ImpactRecordedThisFrame)
            {
                m_ContactImpulse = Vector3.zero;
                m_ContactVelocity = Vector3.zero;
            }

            m_FinalForce = m_AccumulatedForces;
            if (m_AttributeOwner)
            {
                if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_Config.ForceDetectionMultiplier, out float val))
                {
                    m_FinalForce *= val;
                }
            }

            if (!m_DisableDetection && m_FinalForce.magnitude >= m_Config.MinImpulseMagnitude && m_FinalForce.magnitude < m_Config.MaxImpulseMagnitude)
            {
                HandleForceDetected(m_FinalForce, m_Config.Timeout);
            }

            if (m_ImpactRecordedThisFrame)
            {
                m_ImpactRecordedThisFrame = false;
            }
        }

        #endregion

        private void ContactResponse(Collision collision)
        {
            if (m_Config.ContactImpulseInfluence != 0 || m_Config.ContactVelocityInfluence != 0)
            {
                Vector3 relativeVelocity = collision.relativeVelocity;
                // TODO I dont know why we are doing this
                //if (m_OnlyBreakWhenBeingHit)
                //{
                //    relativeVelocity += m_LastVelocity;
                //}

                float mult = 1;

                GameplayTagOwner tagOwner = collision.gameObject.GetComponentInParent<GameplayTagOwner>();
                bool validBasedOnTags = !tagOwner || ((!tagOwner.HasTag(m_Config.SoftTag) && tagOwner.HasTag(m_Config.ToughTag)) || m_TagOwner.HasTag(m_Config.SoftTag));
                bool validBasedOnRigidbody = !collision.collider.attachedRigidbody || (collision.collider.attachedRigidbody.isKinematic && collision.collider.attachedRigidbody.mass >= 0.5f);
                if (!validBasedOnTags && !validBasedOnRigidbody)
                {
                    return;
                }

                // Other collider attributes
                GameplayAttributeOwner attributeOwner = collision.gameObject.GetComponentInParent<GameplayAttributeOwner>();
                if (attributeOwner && attributeOwner.TryGetAttributeEvaluatedValue(m_Config.ForceDetectionMultiplier, out float val))
                {
                    mult *= val;
                }

                // This collider attributes
                attributeOwner = m_AttributeOwner;
                if (attributeOwner && attributeOwner.TryGetAttributeEvaluatedValue(m_Config.ForceDetectionMultiplier, out val))
                {
                    mult *= val;
                }

                Vector3 newImpulseVelocity = collision.impulse * mult * m_Config.ContactImpulseInfluence * 1900;
                if (newImpulseVelocity.magnitude > m_ContactImpulse.magnitude)
                {
                    m_ContactImpulse = newImpulseVelocity;
                }
                Vector3 newContactVelocity = relativeVelocity * mult * m_Config.ContactVelocityInfluence * 100;
                if (newContactVelocity.magnitude > m_ContactVelocity.magnitude)
                {
                    m_ContactVelocity = newContactVelocity;
                }
                m_ImpactRecordedThisFrame = true;

                m_LastCollision = collision;
            }
        }

        private void HandleForceDetected(Vector3 force, float timeout)
        {
            if (m_DamageTimeout)
                return;

            if (Config.OnlyTriggerOnCollision && !m_ImpactRecordedThisFrame)
                return;

            if (!m_Filter.IsEmpty && !m_TagOwner.SatisfiesTagFilter(m_Filter))
                return;

            StartCoroutine(CO_Timeout(timeout));
            ForceDetected(force);
        }

        protected virtual void ForceDetected(Vector3 force)
        {

        }

        private IEnumerator CO_Timeout(float timeout)
        {
            m_DamageTimeout = true;
            yield return new WaitForSeconds(timeout);
            m_DamageTimeout = false;
        }
    }
}
