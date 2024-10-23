using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class ElasticCollisionTester : MonoBehaviour
    {
        const float c_ForceThreshold = 1.5f;

        private void OnCollisionEnter(Collision collision)
        {
            Rigidbody selfRigid = GetComponent<Rigidbody>();
            Rigidbody otherRigid = collision.rigidbody;

            Vector3 vel1 = selfRigid.linearVelocity;
            Vector3 vel2 = otherRigid.linearVelocity;

            Debug.DrawLine(selfRigid.position, selfRigid.position + collision.impulse.normalized * 5, Color.grey, 10, false);
            Vector3 thresholdPos = selfRigid.position + collision.impulse.normalized * c_ForceThreshold;
            Vector3 thresholdCross = Vector3.Cross(collision.impulse, Vector3.forward).normalized;
            Debug.DrawLine(thresholdPos + thresholdCross * 0.2f, thresholdPos - thresholdCross * 0.2f, Color.black, 10, false);
            Debug.DrawLine(selfRigid.position, selfRigid.position + collision.impulse, Color.red, 10, false);

            //Debug.DrawLine(selfRigid.position, selfRigid.position + collision.relativeVelocity, Color.blue, 10, false);
            //Debug.DrawLine(selfRigid.position, selfRigid.position + selfRigid.velocity, Color.green, 10, false);

            //GUI.Label(MGUI.GetWorldRect(selfRigid.position, new Rect(20, 20, 200, 100)), selfRigid.mass.ToString());
            //GUI.Label(MGUI.GetWorldRect(collision.rigidbody.position, new Rect(20, 20, 200, 100)), collision.rigidbody.mass.ToString());
        }

        private void OnGUI()
        {

        }
    }
}
