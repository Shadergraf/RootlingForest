using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        private Transform m_SpawnTransform;
        [SerializeField]
        private int m_MinSpawn = 1;
        [SerializeField]
        private int m_MaxSpawn = 1;
        [SerializeField]
        private GameObject m_Prefab;
        [SerializeField]
        [Range(0, 1)]
        private float m_ForwardVsSpherical;
        [SerializeField]
        private float m_Speed = 1;


        public void SpawnSilent()
        {
            Spawn();
        }

        public List<GameObject> Spawn()
        {
            List<GameObject> list = new List<GameObject>();
            int spawnCount = Random.Range(m_MinSpawn, m_MaxSpawn);
            for (int i = 0; i < spawnCount; i++)
            {
                Transform spawnTransform = m_SpawnTransform ? m_SpawnTransform : transform;
                GameObject go = Instantiate(m_Prefab, spawnTransform.position, spawnTransform.rotation);
                Rigidbody rigid = go.GetComponent<Rigidbody>();
                if (rigid)
                {
                    rigid.linearVelocity = Vector3.Slerp(spawnTransform.forward, Random.onUnitSphere, m_ForwardVsSpherical) * m_Speed;
                }
                list.Add(go);
            }
            return list;
        }

        private void Shoot(GameObject go)
        {
            Rigidbody selfRigid = GetComponent<Rigidbody>();
            Rigidbody spawnedRigid = go.GetComponent<Rigidbody>();
            spawnedRigid.linearVelocity += Vector3.ProjectOnPlane(selfRigid.linearVelocity, Vector3.up);
            selfRigid.AddForce(spawnedRigid.transform.forward * -1 * 200, ForceMode.Force);
        }
    }
}
