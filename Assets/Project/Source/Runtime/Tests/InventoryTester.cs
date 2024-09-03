using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatea.RootlingForest
{
    public class InventoryTester : MonoBehaviour
    {
        [SerializeField]
        private GrabAbility m_GrabAbility;
        [SerializeField]
        private Inventory m_Inventory;

        public void Update()
        {
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                GameObject item = m_GrabAbility.Target.gameObject;
                if (m_Inventory.AddItem(item))
                {
                    m_GrabAbility.enabled = false;
                }
            }
        }
    }
}
