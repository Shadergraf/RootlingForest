using Manatea.RootlingForest;
using UnityEngine;

public class RemoveFromWorldItemBag : MonoBehaviour
{
    [SerializeField]
    private WorldBag m_WorldBag;

    private void OnTriggerEnter(Collider other)
    {
        GameObject item = other.attachedRigidbody.gameObject;
        if (m_WorldBag.Inventory.Contains(item))
        {
            // TODO diiiiiiirty
            FindFirstObjectByType<PhysicsDebugger>().DestroyConnection();

            m_WorldBag.Inventory.RemoveItem(item);
        }
    }
}
