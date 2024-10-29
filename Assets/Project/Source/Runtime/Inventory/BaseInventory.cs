using Manatea.GameplaySystem;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseInventory : MonoBehaviour
{
    [SerializeField]
    private bool m_DisableItems = true;
    [SerializeField]
    private GameplayTagFilter m_TagFilter;
    [SerializeField]
    private GameplayEffect m_InventoryEffect;

    public int ItemCount => m_Items.Count;


    private Dictionary<GameObject, ItemData> m_Items = new();



    public virtual bool AddItem(GameObject item)
    {
        var tagOwner = item.GetComponentInParent<GameplayTagOwner>();
        if (tagOwner && !tagOwner.SatisfiesTagFilter(m_TagFilter))
            return false;

        if (m_Items.ContainsKey(item))
            return false;

        ItemData data = GenerateItemData(item);
        if (data == null)
            return false;
        CommitItemAddition(item, data);
        AddItem_Internal(item, data);
        return true;
    }

    protected virtual ItemData GenerateItemData(GameObject item)
    {
        ItemData itemData = new ItemData();

        itemData.EnabledState = item.activeSelf;

        if (m_InventoryEffect)
        {
            itemData.AppliedEffect = new GameplayEffectInstance(m_InventoryEffect);
        }

        return itemData;
    }

    protected virtual void CommitItemAddition(GameObject item, ItemData itemData)
    {
        if (m_DisableItems)
        {
            item.SetActive(false);
        }

        if (itemData.AppliedEffect != null)
        {
            GameplayEffectOwner effectOwner = item.GetComponent<GameplayEffectOwner>();
            effectOwner.AddEffect(itemData.AppliedEffect);
        }
    }

    protected virtual void AddItem_Internal(GameObject item, ItemData itemData)
    {
        m_Items.Add(item, itemData);
    }


    public virtual bool RemoveItem(GameObject item)
    {
        if (!m_Items.ContainsKey(item))
            return false;

        RemoveItem_Internal(item, m_Items[item]);
        CommitItemRemoval(item, m_Items[item]);
        return true;
    }

    protected virtual void CommitItemRemoval(GameObject item, ItemData itemData)
    {
        if (m_DisableItems)
        {
            item.SetActive(itemData.EnabledState);
        }

        if (itemData.AppliedEffect != null)
        {
            GameplayEffectOwner effectOwner = item.GetComponent<GameplayEffectOwner>();
            effectOwner.RemoveEffect(itemData.AppliedEffect);
        }
    }

    protected virtual void RemoveItem_Internal(GameObject item, ItemData itemData)
    {
        m_Items.Remove(item);
    }


    public bool Contains(GameObject item)
    {
        return m_Items.ContainsKey(item);
    }

    protected class ItemData
    {
        public GameplayEffectInstance AppliedEffect;
        public bool EnabledState;
    }
}
