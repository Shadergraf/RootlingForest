using Manatea.GameplaySystem;
using System.Collections;
using UnityEngine;

public class GasParticle : MonoBehaviour
{
    [SerializeField]
    private float m_GasPushForce = 1;
    [SerializeField]
    private float m_OtherPushForce = 1;
    [SerializeField]
    private float m_OtherInheritVelocity = 1;
    [SerializeField]
    private float m_NeighborTickDuration = 0.5f;
    [SerializeField]
    private float m_NeighborTestRadius = 2;
    [SerializeField]
    private GameplayEffect m_MissingNeighborEffect;
    
    private Rigidbody m_Rigidbody;
    private Collider m_Collider;
    private GameplayEffectOwner m_EffectOwner;


    private void Awake()
    {
        m_Rigidbody = GetComponentInParent<Rigidbody>();
        m_Collider = GetComponentInParent<Collider>();
        m_EffectOwner = GetComponentInParent<GameplayEffectOwner>();
    }

    private void OnEnable()
    {
        StartCoroutine(Tick_GasNeighbors());
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!enabled)
            return;

        Debug.Log("Trigger Stay!");
        if (Physics.ComputePenetration(m_Collider, transform.position, transform.rotation, other, other.transform.position, other.transform.rotation, out Vector3 dir, out float dist))
        {
            float pushForce = m_GasPushForce;
            if (m_Collider.gameObject.layer != other.gameObject.layer)
                pushForce = m_OtherPushForce;
            m_Rigidbody.AddForce(dir * pushForce, ForceMode.Acceleration);


            if (m_Collider.gameObject.layer != other.gameObject.layer)
            {
                m_Rigidbody.AddForce(other.attachedRigidbody.linearVelocity * m_OtherInheritVelocity, ForceMode.Acceleration);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, m_NeighborTestRadius);
    }


    private static Collider[] s_OverlapCollisions = new Collider[3];
    private IEnumerator Tick_GasNeighbors()
    {
        LayerMask mask = 1 << gameObject.layer;

        while (true)
        {
            yield return new WaitForSeconds(m_NeighborTickDuration);

            int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, m_NeighborTestRadius, s_OverlapCollisions, mask, QueryTriggerInteraction.Collide);
            if (overlapCount <= 2)
            {
                m_EffectOwner.AddEffect(m_MissingNeighborEffect);
            }
        }
    }
}
