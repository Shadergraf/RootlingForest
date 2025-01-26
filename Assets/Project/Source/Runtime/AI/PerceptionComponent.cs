using Manatea.RootlingForest;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerceptionComponent : MonoBehaviour
{
    [SerializeField]
    private MemoryComponent m_Memory;

    private HashSet<Collider> m_Colliders;
    private IEnumerator m_HashSetEnumerator;


    private void Awake()
    {
        m_Colliders = new HashSet<Collider>();
        m_HashSetEnumerator = m_Colliders.GetEnumerator();
    }
    private void OnEnable()
    {
        StartCoroutine(Tick());
    }

    IEnumerator Tick()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);

            foreach (Collider coll in m_Colliders)
            {
                Perceive(coll);
            }
            m_Colliders.Clear();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        Perceive(other);
    }
    private void OnTriggerStay(Collider other)
    {
        m_Colliders.Add(other);
    }

    public void Perceive(Collider collider)
    {
        // Can be null if objects are destroyed between perception ticks
        if (collider && collider.attachedRigidbody)
            m_Memory.Remember(collider.attachedRigidbody.gameObject);
    }
}
