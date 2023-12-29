using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea
{
    [ExecuteAlways]
    public class LockRotation : MonoBehaviour
    {
        public Vector3 EulerAngles;

        private void LateUpdate()
        {
            transform.rotation = Quaternion.identity * Quaternion.Euler(EulerAngles);
        }
    }
}
