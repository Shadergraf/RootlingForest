using UnityEngine;

public class SpherePackingUtility : MonoBehaviour
{
    [SerializeField] private int numberOfSpheres = 10; // Number of smaller spheres
    [SerializeField] private float outerSphereRadius = 1.0f; // Radius of the containing sphere
    [SerializeField] private int iterations = 1000; // Optimization iterations
    [SerializeField] private float stepSize = 0.01f; // Adjustment step size for movement
    [SerializeField] private float sphereRadiusFactor = 0.1f; // Radius of smaller spheres as a factor of outer sphere radius

    private Vector3[] positions;
    private float smallSphereRadius;

    void Start()
    {
        smallSphereRadius = outerSphereRadius * sphereRadiusFactor;
        positions = PackSpheres(numberOfSpheres, smallSphereRadius, outerSphereRadius, iterations, stepSize);

        // Visualize the packed spheres
        foreach (var position in positions)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.parent = transform;
            sphere.transform.localPosition = position;
            sphere.transform.localScale = Vector3.one * smallSphereRadius * 2; // Diameter
        }
    }

    Vector3[] PackSpheres(int n, float smallRadius, float largeRadius, int maxIterations, float step)
    {
        Vector3[] spheres = new Vector3[n];
        System.Random random = new System.Random();

        // Initialize positions randomly within the outer sphere
        for (int i = 0; i < n; i++)
        {
            spheres[i] = RandomPointInsideSphere(largeRadius - smallRadius, random);
        }

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 force = Vector3.zero;

                // Apply forces to resolve overlaps between spheres
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;

                    Vector3 direction = spheres[i] - spheres[j];
                    float distance = direction.magnitude;
                    float minDistance = 2 * smallRadius;

                    if (distance < minDistance && distance > 0.0001f)
                    {
                        // Push spheres apart based on overlap
                        force += direction.normalized * (minDistance - distance);
                    }
                }

                // Apply boundary constraint to keep spheres inside the larger sphere
                Vector3 toCenter = spheres[i].normalized * (largeRadius - smallRadius);
                if (spheres[i].magnitude + smallRadius > largeRadius)
                {
                    force -= (spheres[i] - toCenter) * 0.5f; // Push sphere back toward the center
                }

                // Move the sphere based on the accumulated force
                spheres[i] += force * step;
            }
        }

        return spheres;
    }

    Vector3 RandomPointInsideSphere(float radius, System.Random random)
    {
        float u = (float)random.NextDouble();
        float v = (float)random.NextDouble();
        float theta = 2 * Mathf.PI * u;
        float phi = Mathf.Acos(2 * v - 1);
        float r = Mathf.Pow((float)random.NextDouble(), 1.0f / 3.0f) * radius;

        float x = r * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = r * Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = r * Mathf.Cos(phi);

        return new Vector3(x, y, z);
    }
}