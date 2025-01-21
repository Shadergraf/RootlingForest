using UnityEngine;
using System.Collections;

namespace Manatea
{
    public class SetGameObjectParent : MonoBehaviour
    {
        public GameObject Object;
        public Transform NewParent;


        private void OnEnable()
        {
            SetParent();
        }


        public void SetParent()
        {
            GameObject go = gameObject;
            if (Object)
                go = Object;
            go.transform.SetParent(NewParent);
        }
    }
}