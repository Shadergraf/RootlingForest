using Manatea;
using Manatea.GameplaySystem;
using Manatea.RootlingForest;
using System;
using UnityEngine;

public class ForceDetectionBounce : MonoBehaviour
{
    [SerializeField]
    private Fetched<GameplayEventReceiver> m_EventReceiver = new(FetchingType.InParents);
    [SerializeField]
    private GameplayEvent m_ForceDetectionEvent;
    [SerializeField]
    private float m_BounceStrength = 1;
    [SerializeField]
    private float m_ScaleVelocity = 1;


    private void Awake()
    {
        m_EventReceiver.FetchFrom(gameObject);
    }
    private void OnEnable()
    {
        m_EventReceiver.value.RegisterListener(m_ForceDetectionEvent, Bounce);
    }


    public void Bounce(object payload)
    {
        if (payload is not ForceDetectorPayload)
        {
            Debug.LogWarning("Event payload is not of type " + nameof(ForceDetectorPayload) + "!", gameObject);
            return;
        }

        var forceDetector = (ForceDetectorPayload)payload;

        Rigidbody rigid = GetComponentInParent<Rigidbody>();
        rigid.linearVelocity = rigid.linearVelocity * m_ScaleVelocity;
        rigid.AddForce(forceDetector.Collision.GetContact(0).normal * m_BounceStrength, ForceMode.VelocityChange);
    }
}
