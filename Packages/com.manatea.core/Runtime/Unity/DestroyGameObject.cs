﻿using UnityEngine;
using System.Collections;

namespace Manatea
{
    public class DestroyGameObject : MonoBehaviour
    {
        public GameObject objectToDestroy;


        public void Destroy()
        {
            if (!objectToDestroy)
                Debug.LogWarning("No object was set up for destruction.", gameObject);
            Destroy(objectToDestroy);
        }
    }
}