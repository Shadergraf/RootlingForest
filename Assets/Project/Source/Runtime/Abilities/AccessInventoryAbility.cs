using Manatea;
using Manatea.GameplaySystem;
using Manatea.RootlingForest;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Manatea.RootlingForest
{
    public class AccessInventoryAbility : BaseAbility
    {
        [SerializeField]
        private Fetched<GameplayEffectOwner> m_EffectOwner = new(FetchingType.InParents);
        [SerializeField]
        private WorldBagInventory m_Inventory;
        [SerializeField]
        private GameplayEffect m_InventoryEffect;

        public WorldBagInventory Inventory => m_Inventory;

        private GameplayEffectInstance m_InventoryEffectInst;


        private void OnValidate()
        {
            enabled = false;
        }

        private void Awake()
        {
            m_EffectOwner.FetchFrom(gameObject);
        }

        protected override void AbilityEnabled()
        {
            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(m_Inventory.WorldBag.Camera);
            FindFirstObjectByType<PhysicsDebugger>().m_InventoryCam = m_Inventory.WorldBag.Camera;

            m_Inventory.WorldBag.OpenInventory();

            if (m_EffectOwner.value)
            {
                m_InventoryEffectInst = m_EffectOwner.value.AddEffect(m_InventoryEffect);
            }
        }

        protected override void AbilityDisabled()
        {
            var camera = Camera.main;
#if UNITY_EDITOR
            // Exit playmode workaround
            if (!camera)
                return;
#endif
            camera.GetUniversalAdditionalCameraData().cameraStack.Remove(m_Inventory.WorldBag.Camera);

            PhysicsDebugger physicsDebugger = FindFirstObjectByType<PhysicsDebugger>();
#if UNITY_EDITOR
            // Exit playmode workaround
            if (!physicsDebugger)
                return;
#endif
            physicsDebugger.m_InventoryCam = null;

            m_Inventory.WorldBag.CloseInventory();

            if (m_InventoryEffectInst != null)
            {
                m_EffectOwner.value.RemoveEffect(m_InventoryEffectInst);
                m_InventoryEffectInst = null;
            }
        }
    }
}
