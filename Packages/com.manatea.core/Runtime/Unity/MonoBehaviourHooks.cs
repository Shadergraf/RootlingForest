using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MonoBehaviourHooks : MonoBehaviour
{
    public UnityEvent m_Awake;
    public UnityEvent m_Start;
    public UnityEvent m_OnEnable;
    public UnityEvent m_OnDisable;

    private void Awake() => m_Awake.Invoke();
    private void Start() => m_Start.Invoke();
    private void OnEnable() => m_OnEnable.Invoke();
    private void OnDisable() => m_OnDisable.Invoke();
}
