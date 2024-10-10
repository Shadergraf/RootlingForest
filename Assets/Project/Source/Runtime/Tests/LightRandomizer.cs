using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRandomizer : MonoBehaviour
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
        float x = Random.value;
        float y = Random.value;
        transform.rotation = m_InitialRotation * Quaternion.Euler(0, 0, x * 360) * Quaternion.Euler(MMath.Sqrt(y) * m_RandomAmount, 0, 0);
    }
}
