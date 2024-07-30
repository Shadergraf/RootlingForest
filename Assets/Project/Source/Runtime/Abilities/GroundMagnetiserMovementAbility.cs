using Manatea;
using Manatea.AdventureRoots;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalancingMovementAbility : MonoBehaviour, ICharacterMover
{
    [SerializeField]
    private CharacterMovement m_CharacterMovement;

    [SerializeField]
    private float m_GroundMagnetismRadiusStart = 0.5f;
    [SerializeField]
    private float m_GroundMagnetismRadiusEnd = 1.5f;
    [SerializeField]
    private float m_GroundMagnetismDepth = 0.5f;
    [SerializeField]
    private float m_GroundMagnetismForce = 50;
    [SerializeField]
    private int m_GroundMagnetismTrejectoryIterations = 4;
    [SerializeField]
    private float m_GroundMagnetismStepSize = 0.5f;
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
            if (Ballistics.CalculateInitialVelocity(sim.Movement.FeetPos, groundMagnet.Hit.point, sim.Rigidbody.velocity.magnitude, Physics.gravity, out Vector3 velA, out Vector3 velB))
            {
                Vector3 targetVel = velA;
                if (Vector3.Distance(velB, sim.Rigidbody.velocity) < Vector3.Distance(velA, sim.Rigidbody.velocity))
                {
                    targetVel = velB;
                }
                targetVel = targetVel - sim.Rigidbody.velocity;
                targetVel *= m_GroundMagnetismForce;
                targetVel *= MMath.Sqrt(MMath.InverseLerp(0, 0.3f, sim.m_AirborneTimer));
                sim.Rigidbody.AddForce(targetVel, ForceMode.Force);
            }
        }
    }


    private bool DetectGroundMagnetism(CharacterMovement.MovementSimulationState sim, out GroundMagnetismSample groundMagnet)
    {
        int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);


        // Trajectory tests
        Vector2 vel2D = new Vector2(sim.Rigidbody.velocity.XZ().magnitude, sim.Rigidbody.velocity.y);
        (float a, float b) trajectoryParams = CalculateParabola(vel2D, Physics.gravity.y);

        bool groundFound = false;
        groundMagnet = new GroundMagnetismSample();
        groundMagnet.Hit.distance = float.PositiveInfinity;
        groundMagnet.Hit.point = Vector3.positiveInfinity;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i <= m_GroundMagnetismTrejectoryIterations; i++)
        {
            float px1 = i * m_GroundMagnetismStepSize;
            float px2 = (i + 1) * m_GroundMagnetismStepSize;
            float py1 = Parabola(trajectoryParams.a, trajectoryParams.b, px1);
            float py2 = Parabola(trajectoryParams.a, trajectoryParams.b, px2);
            Vector3 p1 = sim.Movement.FeetPos + sim.Rigidbody.velocity.FlattenY().normalized * px1 + Vector3.up * py1;
            Vector3 p2 = sim.Movement.FeetPos + sim.Rigidbody.velocity.FlattenY().normalized * px2 + Vector3.up * py2;

            for (int j = 0; j < 2; j++)
            {
                float radius = MMath.Lerp(m_GroundMagnetismRadiusStart, m_GroundMagnetismRadiusEnd, i / (float)m_GroundMagnetismTrejectoryIterations);
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
                    if (m_GroundHits[k].collider.transform == sim.Rigidbody.transform)
                        continue;
                    if (m_GroundHits[k].collider.transform.IsChildOf(sim.Rigidbody.transform))
                        continue;
                    if (m_GroundHits[k].normal.y <= 0)
                        continue;
                    if (m_GroundHits[k].point.y > sim.Movement.FeetPos.y)
                        continue;
                    if (Vector3.Dot(m_GroundHits[k].point - sim.Movement.FeetPos, sim.Rigidbody.velocity) < 0)
                        continue;
                    if (!sim.Movement.IsRaycastHitWalkable(m_GroundHits[k]))
                        continue;

                    // TODO correctly transform the 3D contact point so that the closest distance can be calculated
                    Vector3 pointFeetSpace = m_GroundHits[k].point - sim.Movement.FeetPos;
                    Vector2 point2D = new Vector2(pointFeetSpace.XZ().magnitude, pointFeetSpace.y);
                    Vector2 sampledPoint = GetClosestPointOnParabola(trajectoryParams.a, trajectoryParams.b, point2D);
                    Vector3 pointOnTrajectory = sim.Movement.FeetPos + sim.Rigidbody.velocity.FlattenY().normalized * sampledPoint.x + Vector3.up * sampledPoint.y;
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


    #region Parabolic Math Helper

    /// <summary>
    /// Defines a 2D parabola of form y=ax²+bx that starts in (0,0)
    /// </summary>
    static float Parabola(float a, float b, float x)
    {
        return a * x * x + b * x;
    }
    /// <summary>
    /// Calculates the parabolic coefficients a and b that define a parabola matching a projectile path with initial velocity and signed gravity
    /// </summary>
    private static (float a, float b) CalculateParabola(Vector2 velocity, float gravity)
    {
        float a = gravity / (2 * velocity.x * velocity.x);
        float b = velocity.y / velocity.x;
        return (a, b);
    }
    /// <summary>
    /// Calculates the closest location on a 2D parabola defined by a and b that starts in (0,0) to a specific point
    /// </summary>
    private static Vector2 GetClosestPointOnParabola(float a, float b, Vector2 point)
    {
        // ChatGPT to the rescue

        // TODO return separate solutions for a = 0

        // Coefficients of the cubic equation in the form Ax^3 + Bx^2 + Cx + D = 0
        float A = 2 * a * a;
        float B = 3 * a * b;
        float C = -2 * a * point.y + b * b + 1;
        float D = -b * point.y - point.x;

        // Convert to a depressed cubic t^3 + pt + q = 0 using the substitution x = t - B/(3A)
        float p = (3 * A * C - B * B) / (3 * A * A);
        float q = (2 * B * B * B - 9 * A * B * C + 27 * A * A * D) / (27 * A * A * A);

        // Calculate the discriminant
        float discriminant = q * q / 4 + p * p * p / 27;

        float[] roots;
        if (discriminant > 0)
        {
            // One real root and two complex roots
            float sqrtDiscriminant = MMath.Sqrt(discriminant);
            float u = MMath.Cbrt(-q / 2 + sqrtDiscriminant);
            float v = MMath.Cbrt(-q / 2 - sqrtDiscriminant);
            float root1 = u + v - B / (3 * A);
            roots = new float[] { root1 };
        }
        else if (discriminant == 0)
        {
            // All roots are real and at least two are equal
            float u = MMath.Cbrt(-q / 2);
            float root1 = 2 * u - B / (3 * A);
            float root2 = -u - B / (3 * A);
            roots = new float[] { root1, root2 };
        }
        else
        {
            // Three distinct real roots
            float rho = MMath.Sqrt(-p * p * p / 27);
            float theta = MMath.Acos(-q / (2 * rho));
            float rhoCbrt = MMath.Cbrt(rho);
            float root1 = 2 * rhoCbrt * MMath.Cos(theta / 3) - B / (3 * A);
            float root2 = 2 * rhoCbrt * MMath.Cos((theta + 2 * MMath.PI) / 3) - B / (3 * A);
            float root3 = 2 * rhoCbrt * MMath.Cos((theta + 4 * MMath.PI) / 3) - B / (3 * A);
            roots = new float[] { root1, root2, root3 };
        }

        // Only return the closest point
        float closestX = roots[0];
        float closestY = Parabola(a, b, closestX);
        for (int i = 0; i < roots.Length; i++)
        {
            float py = Parabola(a, b, roots[i]);
            if (Vector2.Distance(new Vector2(closestX, closestY), point) > Vector2.Distance(new Vector2(roots[i], py), point))
            {
                closestX = roots[i];
                closestY = py;
            }
        }

        return new Vector2(closestX, closestY);
    }

    #endregion
}
