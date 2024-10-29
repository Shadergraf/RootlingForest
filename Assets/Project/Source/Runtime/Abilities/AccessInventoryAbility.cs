using Manatea.RootlingForest;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AccessInventoryAbility : MonoBehaviour
{
    [SerializeField]
    private WorldBagInventory m_Inventory;

    public WorldBagInventory Inventory => m_Inventory;


    private void OnValidate()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(m_Inventory.WorldBag.Camera);
        FindFirstObjectByType<PhysicsDebugger>().m_InventoryCam = m_Inventory.WorldBag.Camera;

        m_Inventory.WorldBag.OpenInventory();
    }

    private void OnDisable()
    {
        var camera = Camera.main;
#if UNITY_EDITOR
        // Exit playmode workaround
        if (!camera)
            return;
#endif
        camera.GetUniversalAdditionalCameraData().cameraStack.Remove(m_Inventory.WorldBag.Camera);
        FindFirstObjectByType<PhysicsDebugger>().m_InventoryCam = null;

        m_Inventory.WorldBag.CloseInventory();
    }
}
