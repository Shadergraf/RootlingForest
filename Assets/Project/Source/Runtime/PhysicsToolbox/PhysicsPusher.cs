using UnityEngine;

public class PhysicsPusher : MonoBehaviour
{
    private Collider m_Collider;

    private void OnTriggerStay(Collider other)
    {
        //Physics.ComputePenetration(Collider, other.transform.position, other.transform.rotation, )
    }
}
