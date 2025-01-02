using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class StorableItemPreferences : GameplayFeaturePreference
    {
        [SerializeField]
        private ItemDescription m_Description;
        [SerializeField]
        private Transform m_Orientation;

        public ItemDescription Description => m_Description;
        public Transform Orientation => m_Orientation;


#if UNITY_EDITOR
        [ContextMenu("Capture Store Orientation")]
        private void CaptureOrientation()
        {
            Debug.Assert(m_Orientation, "No orientation transform set up!", gameObject);
            m_Orientation.transform.localRotation = Quaternion.Inverse(transform.rotation);
        }
        [ContextMenu("Restore Store Orientation")]
        private void RestoreOrientation()
        {
            Debug.Assert(m_Orientation, "No orientation transform set up!", gameObject);
            Rigidbody rigid = GetComponentInParent<Rigidbody>();
            rigid.transform.rotation = Quaternion.Inverse(m_Orientation.transform.localRotation);
        }
#endif
    }
}
