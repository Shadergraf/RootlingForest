using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using UnityEngine.Events;
using UnityEditor.PackageManager.UI;
using UnityEngine.Profiling;
using Manatea.GameplaySystem;
using UnityEngine.Serialization;
using JetBrains.Annotations;

public class ForceDetector : MonoBehaviour
{
    #region Serialized Vars

    [SerializeField]
    private ForceDetectorConfig m_Config;
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
    private bool m_EnableDebugGraphs;
    [SerializeField]
    private bool m_OnlyBreakWhenBeingHit;
    // Events
    [SerializeField]
    private UnityEvent m_ForceDetected;

    #endregion

    #region Public Vars

    public ForceDetectorConfig Config => m_Config;
    public bool DisableDetection => m_DisableDetection;
    public Vector3 Velocity => m_Rigidbody.velocity;
    public Vector3 Acceleration => m_Acceleration;
    public Vector3 Jerk => m_Jerk;
    public Vector3 FinalForce => m_FinalForce;
    public Vector3 ContactImpulse => m_ContactImpulse;
    public Vector3 ContactVelocity => m_ContactVelocity;

    #endregion

    #region Private Vars

    private Rigidbody m_Rigidbody;
    private GameplayAttributeOwner m_AttributeOwner;
    private GameplayTagOwner m_TagOwner;

    private Vector3 m_LastVelocity;
    private Vector3 m_Acceleration;
    private Vector3 m_LastAcceleration;
    private Vector3 m_Jerk;
    private Vector3 m_ContactImpulse;
    private Vector3 m_ContactVelocity;
    private Vector3 m_FinalForce;

    private Vector3 m_AccumulatedForces;

    private bool m_DamageTimeout;
    private bool m_ImpactRecordedThisFrame;

    #endregion


    #region Unity Events

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AttributeOwner = GetComponent<GameplayAttributeOwner>();
        m_TagOwner = GetComponent<GameplayTagOwner>();
    }
    private void OnEnable()
    {
        m_LastVelocity = m_Rigidbody.velocity;
        m_Acceleration = Vector3.zero;
    }


    private void OnCollisionEnter(Collision collision)
    {
        ContactResponse(collision);
    }
    //private void OnCollisionStay(Collision collision)
    //{
    //    ContactResponse(collision);
    //}

    private void FixedUpdate()
    {
        m_Acceleration = (m_Rigidbody.velocity - m_LastVelocity) / Time.fixedDeltaTime;
        m_Jerk = (m_Acceleration - m_LastAcceleration) / Time.fixedDeltaTime;

        m_LastVelocity = m_Rigidbody.velocity;
        m_LastAcceleration = m_Acceleration;

        m_AccumulatedForces = MMath.Damp(m_AccumulatedForces, Vector3.zero, m_Config.ImpulseTimeFalloff, Time.fixedDeltaTime);
        m_AccumulatedForces += m_Jerk * m_Config.JerkInfluence;
        m_AccumulatedForces += m_ContactImpulse;
        m_AccumulatedForces += m_ContactVelocity;

        // Persist the contact variables for one fixedUpdate step
        if (m_ImpactRecordedThisFrame)
        {
            m_ImpactRecordedThisFrame = false;
        }
        else
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

        if (!m_DisableDetection && m_FinalForce.magnitude > m_Config.ImpulseMagnitude)
        {
            ForceDetected();
        }
    }

    #endregion

    private void ContactResponse(Collision collision)
    {
        if (m_Config.ContactImpulseInfluence != 0 || m_Config.ContactVelocityInfluence != 0)
        {
            Vector3 relativeVelocity = collision.relativeVelocity;
            if (m_OnlyBreakWhenBeingHit)
            {
                relativeVelocity += m_LastVelocity;
            }

            float mult = 1;

            GameplayTagOwner tagOwner = collision.gameObject.GetComponentInParent<GameplayTagOwner>();
            bool validBasedOnTags = !tagOwner || ((!tagOwner.Tags.Contains(m_Config.SoftTag) && tagOwner.Tags.Contains(m_Config.ToughTag)) || m_TagOwner.Tags.Contains(m_Config.SoftTag));
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
        }
    }

    private void ForceDetected()
    {
        if (m_DamageTimeout)
        {
            return;
        }

        m_ForceDetected.Invoke();

        if (m_Config.HealthAttribute && m_AttributeOwner)
        {
            m_AttributeOwner.ChangeAttributeBaseValue(m_Config.HealthAttribute, v => v - 1);

            if (m_AttributeOwner.TryGetAttributeEvaluatedValue(m_Config.HealthAttribute, out float health) && health > 0)
            {
                StartCoroutine(CO_Timeout());
                return;
            }
        }

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

    private IEnumerator CO_Timeout()
    {
        m_DamageTimeout = true;
        yield return new WaitForSeconds(0.4f);
        m_DamageTimeout = false;
    }
}
