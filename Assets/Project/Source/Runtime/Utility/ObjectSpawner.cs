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
        private GameObject m_Prefab;


        public GameObject Spawn()
        {
            GameObject go = Instantiate(m_Prefab, m_SpawnTransform.position, m_SpawnTransform.rotation);
            Shoot(go);
            return go;
        }

        public void SpawnSilent()
        {
            GameObject go = Instantiate(m_Prefab, m_SpawnTransform.position, m_SpawnTransform.rotation);
            Shoot(go);
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
