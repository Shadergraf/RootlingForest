using Manatea;
using Manatea.GameplaySystem;
using Manatea.RootlingForest;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class TestOverlap : MonoBehaviour
{
    [SerializeField]
    private Fetched<Rigidbody> m_Rigidbody = new(FetchingType.InParents);
    [SerializeField]
    private Collider m_Trigger;
    [SerializeField]
    private float m_TickTime = 1;
    [SerializeField]
    private QueryTriggerInteraction m_TriggerInteration;
    [SerializeField]
    private GameplayTagFilter m_Filter;
    [SerializeField]
    private List<Collider> m_IgnoreCollider;
    [SerializeField]
    private List<GameplayTagOwner> m_IgnoreTagOwner;
    [Space]
    [SerializeField]
    private UnityEvent m_OverlapDetected;
    [SerializeField]
    private UnityEvent m_NoOverlapDetected;

    private TriggerEventSender m_TriggerEventSender;
    private List<Collider> m_TriggeredColliders = new List<Collider>();


    private void Awake()
    {
        m_Rigidbody.FetchFrom(gameObject);

        if (m_Rigidbody.value)
        {
            m_TriggerEventSender = m_Rigidbody.value.gameObject.AddComponent<TriggerEventSender>();
        }
    }
    private void OnEnable()
    {
        if (!m_Rigidbody.value)
        {
            Debug.LogWarning("No Rigidbody was fetched from object!", gameObject);
            enabled = false;
            return;
        }

        m_TriggerEventSender.OnTriggerEnterEvent += OnTriggerEnterEvent;
        StartCoroutine(TickLoop());
    }
    private void OnDisable()
    {
        if (!m_Rigidbody.value)
        {
            return;
        }

        StopAllCoroutines();
        m_TriggerEventSender.OnTriggerEnterEvent -= OnTriggerEnterEvent;
    }
    private void OnDestroy()
    {
        if (m_TriggerEventSender)
        {
            Destroy(m_TriggerEventSender);
        }
    }

    private void OnTriggerEnterEvent(Collider other)
    {
        m_TriggeredColliders.Add(other);
    }

    private IEnumerator TickLoop()
    {
        while (true)
        {
            m_Rigidbody.value.detectCollisions = true;
            m_TriggeredColliders.Clear();
            yield return new WaitForFixedUpdate();
            m_Rigidbody.value.detectCollisions = false;

            bool detected = Test();
            if (detected)
                m_OverlapDetected.Invoke();
            else
                m_NoOverlapDetected.Invoke();

            yield return new WaitForSeconds(m_TickTime - Time.fixedDeltaTime);
        }
    }

    private bool Test()
    {
        foreach (Collider collider in m_TriggeredColliders)
        {
            if (m_IgnoreCollider.Contains(collider))
                continue;

            if (m_Filter.IsEmpty)
                return true;

            var colliderTagOwner = collider.GetComponentInParent<GameplayTagOwner>();
            if (!colliderTagOwner)
                continue;
            if (m_IgnoreTagOwner.Contains(colliderTagOwner))
                continue;
            bool filterMatching = colliderTagOwner.SatisfiesTagFilter(m_Filter);
            if (filterMatching)
                return true;
        }

        return false;
    }

    private class TriggerEventSender : MonoBehaviour
    {
        public event Action<Collider> OnTriggerEnterEvent;

        private void OnTriggerEnter(Collider collider)
        {
            OnTriggerEnterEvent?.Invoke(collider);
        }
    }
}
