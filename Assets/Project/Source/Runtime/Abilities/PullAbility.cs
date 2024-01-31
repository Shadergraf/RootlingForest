using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using Manatea.AdventureRoots;

public class PullAbility : MonoBehaviour
{
    public Rigidbody Target;
    public float SpringForce = 1000;
    public float MaxDamper = 50;
    public float MinDamper = 2;
    public float DamperSpeed = 2;
    public Vector3 HandPosition = new Vector3(0, .8f, .8f);
    public Vector3 TargetPosition = new Vector3(0, 0, 0);
    public bool AutoTargetPosition;
    public float ThrowForce = 5;
    public Vector3 ThrowDir = new Vector3(0, 1, 1);
    public float BreakForce = 10;
    public float BreakTorque = 10;
    public float LinearLimit = 0;
    public bool EnableCollision = true;

    public float MinRotationForce = 10;
    public float MaxRotationForce = 10;

    private Joint m_Joint;


    private void OnEnable()
    {
        m_Joint = gameObject.AddComponent<ConfigurableJoint>();
        m_Joint.connectedBody = Target;
        m_Joint.autoConfigureConnectedAnchor = false;
        m_Joint.enableCollision = EnableCollision;
        m_Joint.breakForce = BreakForce;
        m_Joint.breakTorque = BreakTorque;

        // Find closest point
        var colliders = Target.GetComponents<Collider>();
        Vector3 closestPoint = Vector3.one * 10000;
        for (int i  = 0; i < colliders.Length; i++)
        {
            Vector3 newPoint = colliders[i].ClosestPoint(transform.position);
            if (Vector3.Distance(transform.position, newPoint) < Vector3.Distance(transform.position, closestPoint))
                closestPoint = newPoint;
        }
        closestPoint = Target.transform.InverseTransformPoint(closestPoint);

        m_Joint.anchor = HandPosition;
        if (AutoTargetPosition)
        {
            // TODO ClosestPointOnBounds uses the bounding box instead of the actual collider. This is imprecise
            if (Target.TryGetComponent(out PullSettings pullSettings))
            {
                switch (pullSettings.PullLocation)
                {
                    case PullLocation.Center:
                        m_Joint.connectedAnchor = Vector3.zero;
                        break;
                    case PullLocation.Bounds:
                        m_Joint.connectedAnchor = Target.transform.InverseTransformPoint(Target.ClosestPointOnBounds(transform.TransformPoint(HandPosition)));
                        break;
                }
            }
        }
        else
        {
            m_Joint.connectedAnchor = TargetPosition;
        }
        //m_Joint.anchor = transform.InverseTransformPoint(Target.transform.TransformPoint(m_Joint.connectedAnchor));

        if (m_Joint is SpringJoint)
        {
            (m_Joint as SpringJoint).spring = SpringForce;
            (m_Joint as SpringJoint).damper = MaxDamper;
        }
        if (m_Joint is ConfigurableJoint)
        {
            ConfigurableJoint configurableJoint = (ConfigurableJoint)m_Joint;

            if (Target.TryGetComponent(out GrabPreferences grabPrefs))
            {
                grabPrefs.EstablishGrab(configurableJoint);
            }

            configurableJoint.linearLimit = new SoftJointLimit() { limit = LinearLimit, contactDistance = 0.1f };
            configurableJoint.linearLimitSpring = new SoftJointLimitSpring() { spring = SpringForce, damper = MaxDamper };
        }

        // TODO try ignoring all collision stuff
        Physics.IgnoreCollision(GetComponent<Collider>(), Target.GetComponentInChildren<Collider>(), true);
    }
    private void OnDisable()
    {
        if (Target != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), Target.GetComponentInChildren<Collider>(), false);
        }

        Target = null;

        if (m_Joint != null)
        {
            Destroy(m_Joint);
        }
        m_Joint = null;

        if (TryGetComponent(out CharacterMovement movement))
        {
            movement.SetRotationMult(1);
        }
    }

    private void Update()
    {
        if (m_Joint == null ||
            m_Joint.connectedBody == null)
        {
            enabled = false;
            return;
        }

        DebugHelper.DrawWireSphere(m_Joint.transform.TransformPoint(m_Joint.anchor), 0.1f, Color.green, iterations: 8);
        DebugHelper.DrawWireSphere(m_Joint.connectedBody.transform.TransformPoint(m_Joint.connectedAnchor), 0.1f, Color.red, iterations: 8);
    }
    private void FixedUpdate()
    {
        if (m_Joint == null)
        {
            enabled = false;
            return;
        }

        if (TryGetComponent(out CharacterMovement movement))
        {
            movement.SetRotationMult(MMath.RemapClamped(MinRotationForce, MaxRotationForce, 1, 0, m_Joint.currentForce.magnitude));
        }

        if (m_Joint is SpringJoint)
        {
            (m_Joint as SpringJoint).damper = MMath.Damp((m_Joint as SpringJoint).damper, MinDamper, DamperSpeed, Time.fixedDeltaTime);
        }

        if (m_Joint is ConfigurableJoint)
        {
            SoftJointLimitSpring linearLimitSpring = (m_Joint as ConfigurableJoint).linearLimitSpring;
            linearLimitSpring.damper = MMath.Damp(linearLimitSpring.damper, MinDamper, DamperSpeed, Time.fixedDeltaTime);
            (m_Joint as ConfigurableJoint).linearLimitSpring = linearLimitSpring;

            if (MMath.Abs((m_Joint as ConfigurableJoint).linearLimitSpring.damper - MinDamper) < 0.5f)
            {
                (m_Joint as ConfigurableJoint).linearLimit = new SoftJointLimit() { limit = 0, contactDistance = 0.1f };
                (m_Joint as ConfigurableJoint).linearLimitSpring = new SoftJointLimitSpring() { spring = SpringForce, damper = MinDamper };
            }
        }
    }

    public void Throw()
    {
        Debug.Assert(enabled, "Ability is not active!", gameObject);
        Destroy(m_Joint);

        float throwForce = ThrowForce / MMath.Max(Target.mass, 1);
        Vector3 throwDir = transform.right * ThrowDir.x + transform.up * ThrowDir.y + transform.forward * ThrowDir.z;
        Target.velocity += throwDir * throwForce;

        enabled = false;
    }
}
