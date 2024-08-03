using Manatea;
using Manatea.AdventureRoots;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TrajectoryAdjustmentMovementAbility : MonoBehaviour, ICharacterMover
{
    [SerializeField]
    private CharacterMovement m_CharacterMovement;

    [Space]
    [FormerlySerializedAs("m_GroundMagnetismRadiusStart")]
    [SerializeField]
    private float m_TraceRadiusStart = 0.5f;
    [SerializeField]
    [FormerlySerializedAs("m_GroundMagnetismRadiusEnd")]
    private float m_TraceRadiusEnd = 1.5f;
    [SerializeField]
    [FormerlySerializedAs("m_GroundMagnetismForce")]
    private float m_AdjustmentForce = 50;
    [SerializeField]
    [FormerlySerializedAs("m_GroundMagnetismTrejectoryIterations")]
    private int m_TrejectoryIterations = 4;
    [SerializeField]
    [FormerlySerializedAs("m_GroundMagnetismStepSize")]
    private float m_TrajectoryStepSize = 0.5f;

    [Space]
    [SerializeField]
    private bool m_Debug;

    private RaycastHit[] m_GroundHits = new RaycastHit[32];

    private struct GroundMagnetismSample
    {
        public Vector3 ClosestPointOnTrajectory;
        public RaycastHit Hit;
    }


    private void OnEnable()
    {
        m_CharacterMovement.RegisterMover(this);
    }


    private void OnDisable()
    {
        m_CharacterMovement.UnregisterMover(this);
    }


    public void PreMovement(CharacterMovement.MovementSimulationState sim)
    {
        bool groundMagnetFound = DetectGroundMagnetism(sim, out GroundMagnetismSample groundMagnet);
        if (groundMagnetFound && !sim.m_IsStableGrounded)
        {
            // Add enough force to transform the current trajectory into one that hits the ground magnetism point
            if (Ballistics.CalculateInitialVelocity(sim.Movement.FeetPos, groundMagnet.Hit.point, sim.Movement.Rigidbody.velocity.magnitude, Physics.gravity, out Vector3 velA, out Vector3 velB))
            {
                Vector3 targetVel = velA;
                if (Vector3.Distance(velB, sim.Movement.Rigidbody.velocity) < Vector3.Distance(velA, sim.Movement.Rigidbody.velocity))
                {
                    targetVel = velB;
                }
                targetVel = targetVel - sim.Movement.Rigidbody.velocity;
                targetVel *= m_AdjustmentForce;
                targetVel *= MMath.Sqrt(MMath.InverseLerp(0, 0.3f, sim.m_AirborneTimer));
                sim.Movement.Rigidbody.AddForce(targetVel, ForceMode.Force);
            }
        }
    }


    private bool DetectGroundMagnetism(CharacterMovement.MovementSimulationState sim, out GroundMagnetismSample groundMagnet)
    {
        int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

        // Trajectory tests
        Vector2 vel2D = new Vector2(sim.Movement.Rigidbody.velocity.XZ().magnitude, sim.Movement.Rigidbody.velocity.y);
        (float a, float b) trajectoryParams = Ballistics.CalculateParabola(vel2D, Physics.gravity.y);

        bool groundFound = false;
        groundMagnet = new GroundMagnetismSample();
        groundMagnet.Hit.distance = float.PositiveInfinity;
        groundMagnet.Hit.point = Vector3.positiveInfinity;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i <= m_TrejectoryIterations; i++)
        {
            float px1 = i * m_TrajectoryStepSize;
            float px2 = (i + 1) * m_TrajectoryStepSize;
            float py1 = Ballistics.Parabola(trajectoryParams.a, trajectoryParams.b, px1);
            float py2 = Ballistics.Parabola(trajectoryParams.a, trajectoryParams.b, px2);
            Vector3 p1 = sim.Movement.FeetPos + sim.Movement.Rigidbody.velocity.FlattenY().normalized * px1 + Vector3.up * py1;
            Vector3 p2 = sim.Movement.FeetPos + sim.Movement.Rigidbody.velocity.FlattenY().normalized * px2 + Vector3.up * py2;

            for (int j = 0; j < 2; j++)
            {
                float radius = MMath.Lerp(m_TraceRadiusStart, m_TraceRadiusEnd, i / (float)m_TrejectoryIterations);
                radius *= j;
                Vector3 pp1 = p1 + (p1 - p2).normalized * radius * 1.25f;
                int hitCount = Physics.SphereCastNonAlloc(pp1, radius, (p2 - pp1).normalized, m_GroundHits, (p2 - pp1).magnitude, layerMask, QueryTriggerInteraction.Ignore);

                if (m_Debug)
                {
                    Debug.DrawLine(p1, p2, Color.blue);
                    DebugHelper.DrawWireCircle(p2, 0.4f, Vector3.up, Color.blue);
                    DebugHelper.DrawWireCapsule(pp1, p2, radius, Color.grey);
                }

                for (int k = 0; k < hitCount; k++)
                {
                    // Discard overlaps
                    if (m_GroundHits[k].distance == 0)
                        continue;
                    // Discard self collisions
                    if (m_GroundHits[k].collider.transform == sim.Movement.Rigidbody.transform)
                        continue;
                    if (m_GroundHits[k].collider.transform.IsChildOf(sim.Movement.Rigidbody.transform))
                        continue;
                    if (m_GroundHits[k].normal.y <= 0)
                        continue;
                    if (m_GroundHits[k].point.y > sim.Movement.FeetPos.y)
                        continue;
                    if (Vector3.Dot(m_GroundHits[k].point - sim.Movement.FeetPos, sim.Movement.Rigidbody.velocity) < 0)
                        continue;
                    if (!sim.Movement.IsRaycastHitWalkable(m_GroundHits[k]))
                        continue;

                    // TODO correctly transform the 3D contact point so that the closest distance can be calculated
                    Vector3 pointFeetSpace = m_GroundHits[k].point - sim.Movement.FeetPos;
                    Vector2 point2D = new Vector2(pointFeetSpace.XZ().magnitude, pointFeetSpace.y);
                    Vector2 sampledPoint = Ballistics.GetClosestPointOnParabola(trajectoryParams.a, trajectoryParams.b, point2D);
                    Vector3 pointOnTrajectory = sim.Movement.FeetPos + sim.Movement.Rigidbody.velocity.FlattenY().normalized * sampledPoint.x + Vector3.up * sampledPoint.y;
                    float distanceHeuristic = Vector3.Distance(m_GroundHits[k].point, pointOnTrajectory) * 2.5f + Vector3.Distance(m_GroundHits[k].point, sim.Movement.FeetPos);
                    if (m_Debug)
                    {
                        Debug.DrawLine(m_GroundHits[k].point, pointOnTrajectory, Color.black);
                        DebugHelper.DrawWireSphere(m_GroundHits[k].point, 0.2f, Color.red);
                        Debug.DrawLine(m_GroundHits[k].point, m_GroundHits[k].point + m_GroundHits[k].normal * 0.3f, Color.red);
                    }
                    if (distanceHeuristic > closestDistance)
                        continue;

                    groundMagnet.ClosestPointOnTrajectory = pointOnTrajectory;
                    groundMagnet.Hit = m_GroundHits[k];
                    closestDistance = distanceHeuristic;
                    groundFound = true;
                }
            }
        }

        if (m_Debug)
        {
            DebugHelper.DrawWireSphere(groundMagnet.Hit.point, 0.2f, Color.green);
            Debug.DrawLine(groundMagnet.Hit.point, groundMagnet.Hit.point + groundMagnet.Hit.normal * 0.3f, Color.green);
        }

        return groundFound;
    }
}
