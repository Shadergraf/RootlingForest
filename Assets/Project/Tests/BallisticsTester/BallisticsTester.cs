using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;

public class BallisticsTester : MonoBehaviour
{
    public Transform m_TargetPoint;
    public float m_TargetSpeed;

    private void OnDrawGizmos()
    {
        bool result = Ballistics.CalculateInitialVelocity(transform.position, m_TargetPoint.position, m_TargetSpeed, Physics.gravity, out Vector3 targetVelocity1, out Vector3 targetVelocity2);


        DrawBallisticCurve(targetVelocity1, Color.green);
        DrawBallisticCurve(targetVelocity2, Color.blue);
    }

    void DrawBallisticCurve(Vector3 velocity, Color color)
    {
        Vector3 currentPos = transform.position;
        Vector3 currentVel = velocity;
        for (int i = 0; i < 200; i++)
        {
            currentVel += Time.fixedDeltaTime * 0.2f * Physics.gravity;
            Vector3 newPos = currentPos + currentVel * Time.fixedDeltaTime * 0.2f;
            Debug.DrawLine(currentPos, newPos, color);
            currentPos = newPos;
        }
    }
}
