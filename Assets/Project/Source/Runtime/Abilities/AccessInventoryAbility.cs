using Manatea.RootlingForest;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccessInventoryAbility : MonoBehaviour
{
    [SerializeField]
    private GrabAbility m_GrabAbility;
    [SerializeField]
    private Inventory m_Inventory;


    private void OnEnable()
    {
        if (m_GrabAbility.Target)
        {
            GameObject item = m_GrabAbility.Target.gameObject;
            if (item && m_Inventory.AddItem(item))
            {
                m_GrabAbility.enabled = false;
            }
        }

        enabled = false;
    }
}
