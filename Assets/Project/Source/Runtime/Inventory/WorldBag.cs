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

    [Space]
    [SerializeField]
    private float m_SpringStrength = 10;
    [SerializeField]
    private float m_InheritVelocityMultiplier = 1;
    [SerializeField]
    private float m_WobbleSpeed = 2;
    [SerializeField]
    private float m_WobbleStrength = 1;
    [SerializeField]
    private float m_BobbingSpeed = 2;
    [SerializeField]
    private float m_BobbingStrength = 1;

    public Rigidbody Rigidbody => m_Rigidbody;
    public Camera Camera => m_Camera;
    public Transform ItemContainer => m_ItemContainer;
    public GameObject ItemConstraintPrefab => m_ItemConstraintPrefab;

    [NonSerialized]
    public WorldBagInventory Inventory;

    private Rigidbody m_OwnerRigidbody;
    private Vector3 m_TargetPosition;
    private float m_WalkDistance;


    private void Start()
    {
        m_OwnerRigidbody = Inventory.GetComponentInParent<Rigidbody>();

        m_TargetPosition = m_Rigidbody.position;
    }
    private void FixedUpdate()
    {
        // Inherited velocity
        Vector3 velocity = m_OwnerRigidbody.linearVelocity;
        Vector3 projectedVelocity = Inventory.BagSpawnPoint.transform.TransformVector(velocity);
        m_Rigidbody.AddForce(projectedVelocity * m_InheritVelocityMultiplier, ForceMode.Acceleration);

        // Walk bobbing
        m_WalkDistance += velocity.FlattenY().magnitude * Time.fixedDeltaTime;
        m_Rigidbody.AddForce(Vector3.up * MMath.Sin(m_WalkDistance * m_BobbingSpeed) * m_BobbingStrength, ForceMode.Acceleration);

        // Soft wobble
        Vector3 wobble = new Vector3(Mathf.PerlinNoise1D(Time.time * m_WobbleSpeed), Mathf.PerlinNoise1D(Time.time * m_WobbleSpeed - 100), 0) * 2 - (Vector3)Vector2.one;
        m_Rigidbody.AddForce(wobble * m_WobbleStrength, ForceMode.Acceleration);

        // Spring back to start position
        Vector3 springForce = m_TargetPosition - m_Rigidbody.position;
        m_Rigidbody.AddForce(springForce * m_SpringStrength, ForceMode.Acceleration);
    }


    public void OpenInventory()
    {
        m_GrabTrigger.isTrigger = true;
    }
    public void CloseInventory()
    {
        m_GrabTrigger.isTrigger = false;
    }
}
