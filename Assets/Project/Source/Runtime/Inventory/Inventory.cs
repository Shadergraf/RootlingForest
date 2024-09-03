using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.Graphs;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField]
    [Min(0)]
    private int m_InitialSize;
    [SerializeField]
    private bool m_DisableItems = true;
    [SerializeField]
    private Transform[] m_ItemLocations;
    [SerializeField]
    private GameplayTagFilter m_TagFilter;

    public int Size => m_ItemSlots.Count;


    private List<GameObject> m_ItemSlots = new List<GameObject>();
    public GameObject GetItemAt(int slot) => m_ItemSlots[slot];


    private void Start()
    {
        if (m_InitialSize == 0)
            Debug.LogWarning("Inventory is empty.", this);
        Resize(m_InitialSize);
    }

    public bool AddItem(GameObject item)
    {
        var tagOwner = item.GetComponentInParent<GameplayTagOwner>();
        if (tagOwner && !tagOwner.Tags.SatisfiesTagFilter(m_TagFilter))
            return false;

        for (int i = 0; i < m_ItemSlots.Count; i++)
        {
            if (m_ItemSlots[i] != null)
                continue;

            AddItem_Internal(item, i);
            return true;
        }
        return false;
    }
    public bool AddItem(GameObject item, int slot)
    {
        if (m_ItemSlots[slot] != null)
            return false;

        var tagOwner = item.GetComponentInParent<GameplayTagOwner>();
        if (tagOwner && !tagOwner.Tags.SatisfiesTagFilter(m_TagFilter))
            return false;

        AddItem_Internal(item, slot);
        return true;
    }

    private void AddItem_Internal(GameObject item, int slot)
    {
        m_ItemSlots[slot] = item;
        if (!item)
            return;

        Transform itemParent = transform;
        if (m_ItemLocations.Length != 0)
            itemParent = m_ItemLocations[slot % m_ItemLocations.Length];
        item.transform.SetParent(itemParent);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (m_DisableItems)
        {
            item.SetActive(false);
        }
    }

    public bool RemoveItem(int slot)
    {
        GameObject item = m_ItemSlots[slot];
        if (!item)
            return false;

        bool discarded = RemoveItem_Internal(slot);
        return discarded;
    }
    public bool RemoveItem(GameObject item)
    {
        int slot = m_ItemSlots.IndexOf(item);
        if (slot != -1)
            return false;

        bool discarded = RemoveItem_Internal(slot);
        return discarded;
    }
    private GameObject RemoveItem_Internal(int slot)
    {
        GameObject item = m_ItemSlots[slot];
        if (!item)
            return null;
        m_ItemSlots[slot] = null;

        item.transform.SetParent(null, true);
        if (m_DisableItems)
        {
            item.SetActive(true);
        }

        return item;
    }

    public List<GameObject> Resize(int newSize)
    {
        while (newSize > m_ItemSlots.Count)
        {
            m_ItemSlots.Add(null);
        }

        List<GameObject> removedItems = new List<GameObject>();
        while (newSize < m_ItemSlots.Count)
        {
            int id = m_ItemSlots.Count - 1;
            GameObject itemToRemove = m_ItemSlots[id];
            if (itemToRemove)
            {
                RemoveItem_Internal(id);
            }
            else
            {
                removedItems.Add(itemToRemove);
                m_ItemSlots.RemoveAt(id);
            }
        }

        return removedItems;
    }
    public void SwapItems(int slotA, int slotB)
    {
        if (slotA == slotB)
            return;

        GameObject itemA = m_ItemSlots[slotA];
        GameObject itemB = m_ItemSlots[slotB];
        RemoveItem_Internal(slotA);
        RemoveItem_Internal(slotB);
        AddItem_Internal(itemB, slotA);
        AddItem_Internal(itemA, slotB);
    }
}
