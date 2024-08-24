using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeRotation : MonoBehaviour
{
    [SerializeField]
    private float m_RandomAmount = 0.01f;

    private Quaternion m_InitialRotation;


    private void OnEnable()
    {
        m_InitialRotation = transform.rotation;
    }

    private void Update()
    {
        transform.rotation = m_InitialRotation * Quaternion.Slerp(Quaternion.identity, Random.rotation, m_RandomAmount);
    }
}
