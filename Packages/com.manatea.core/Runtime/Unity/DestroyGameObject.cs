using UnityEngine;
using System.Collections;

namespace Manatea
{
    public class DestroyGameObject : MonoBehaviour
    {
        public GameObject objectToDestroy;


        public void Destroy()
        {
            Destroy(objectToDestroy);
        }
    }
}