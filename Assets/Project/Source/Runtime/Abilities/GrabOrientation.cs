using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabOrientation : MonoBehaviour
{
    [SerializeField]
    private float m_Weight = 1;

    public float Weight
    { get { return m_Weight; } set { m_Weight = value; } }
}
