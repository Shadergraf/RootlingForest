using Manatea;
using Manatea.RootlingForest;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeAbility : MonoBehaviour
{
    [SerializeField]
    private GrabAbility m_GrabAbility;
    [SerializeField]
    private float m_Speed = 10;
    [SerializeField]
    private float m_Amount = 0.01f;

    private float m_Time;


    private void OnEnable()
    {
        if (!m_GrabAbility.enabled)
        {
            enabled = false;
            return;
        }

        m_Time = 0;
    }

    private void OnDisable()
    {
        
    }

    private void FixedUpdate()
    {
        m_Time += Time.fixedDeltaTime;
        m_GrabAbility.Joint.anchor += Vector3.up * MMath.Sin(m_Time * m_Speed) * m_Amount;
    }
}
