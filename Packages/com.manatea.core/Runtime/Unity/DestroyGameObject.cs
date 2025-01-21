using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

namespace Manatea
{
    public class DestroyGameObject : MonoBehaviour
    {
        [FormerlySerializedAs("objectToDestroy")]
        public GameObject Object;


        private void OnEnable()
        {
            Destroy();
        }


        public void Destroy()
        {
            GameObject go = gameObject;
            if (Object)
                go = Object;
            Destroy(go);
        }
    }
}