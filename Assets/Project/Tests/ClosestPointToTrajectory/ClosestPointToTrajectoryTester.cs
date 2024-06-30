using Manatea;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;

public class ClosestPointToTrajectoryTester : MonoBehaviour
{

    public Vector3 StartVelocity;
    public Transform TargetPoint;

    private Vector3 m_CurrentPosition;
    private Vector3 m_CurrentVelocity;


    private void OnDrawGizmos()
    {
        m_CurrentPosition = transform.position;
        m_CurrentVelocity = StartVelocity;

        float dt = Time.fixedDeltaTime / 8;
        for (int i = 0; i < 1000; i++)
        {
            m_CurrentVelocity += Physics.gravity * dt;
            Vector3 newPos = m_CurrentPosition + m_CurrentVelocity * dt;
            Debug.DrawLine(m_CurrentPosition, newPos, Color.red);
            m_CurrentPosition = newPos;
        }


        var coeffs = CalculateParabola(StartVelocity, Physics.gravity.y);
        Vector2 closestPoint = GetClosestPointOnParabola(coeffs.a, coeffs.b, TargetPoint.position);
        Debug.DrawLine(TargetPoint.position, closestPoint, Color.green);
    }

    static float Parabola(float a, float b, float x)
    {
        return a * x * x + b * x;
    }
    private static (float a, float b) CalculateParabola(Vector2 velocity, float gravity)
    {
        float a = gravity / (2 * velocity.x * velocity.x);
        float b = velocity.y / velocity.x;
        return (a, b);
    }

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
}
