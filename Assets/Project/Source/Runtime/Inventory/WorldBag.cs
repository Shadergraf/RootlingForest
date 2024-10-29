using Manatea;
using Manatea.RootlingForest;
using System;
using UnityEngine;

public class WorldBag : MonoBehaviour
{
    [SerializeField]
    private Rigidbody m_Rigidbody;
    [SerializeField]
    private Camera m_Camera;
    [SerializeField]
    private Transform m_ItemContainer;
    [SerializeField]
    private GameObject m_ItemConstraintPrefab;
    [SerializeField]
    private Collider m_GrabTrigger;

    public Rigidbody Rigidbody => m_Rigidbody;
    public Camera Camera => m_Camera;
    public Transform ItemContainer => m_ItemContainer;
    public GameObject ItemConstraintPrefab => m_ItemConstraintPrefab;

    [NonSerialized]
    public WorldBagInventory Inventory;


    public void OpenInventory()
    {
        m_GrabTrigger.isTrigger = true;
    }
    public void CloseInventory()
    {
        m_GrabTrigger.isTrigger = false;
    }
}
