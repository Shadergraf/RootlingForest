using UnityEngine;

public class GasSpawner : MonoBehaviour
{
    [SerializeField]
    private int m_SpawnCount;
    [SerializeField]
    private GameObject m_Prefab;

    private void OnEnable()
    {
        for (int i = 0; i < m_SpawnCount; i++)
        {
            Instantiate(m_Prefab, transform.position + Random.insideUnitSphere * .1f, Quaternion.identity);
        }
    }
}
