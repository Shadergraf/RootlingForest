using UnityEngine;
using UnityEditor;

namespace Manatea.Unity
{
    public static class SnapGameObjectsUtility
    {
        private const float k_SnapTolerance = 0.001f;
        private const string k_Undo_RaycastToMouse = "Raycast object to mouse";
        private const string k_Undo_RaycastToGround = "Raycast object to ground";


        [InitializeOnLoadMethod]
        private static void Init()
        {
            SceneView.beforeSceneGui += BeforeSceneGui;
        }

        private static void BeforeSceneGui(SceneView sceneView) => HandleUtlity(sceneView.camera);

        private static void HandleUtlity(Camera camera)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.End)
            {
                if (Event.current.control)
                {
                    SnapObjectsToGround(Selection.gameObjects);
                }
                else
                {
                    RaycastObjectsToGround(Selection.gameObjects);
                }
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.G)
            {
                if (Event.current.control)
                {
                    SnapObjectsToMouse(Selection.gameObjects, camera);
                }
                else
                {
                    RaycastObjectsToMouse(Selection.gameObjects, camera);
                }
            }
        }


        private static Ray GetMouseRay(Camera camera)
        {
            Vector3 mousePos = Event.current.mousePosition;
            Ray mouseRay = camera.ScreenPointToRay(mousePos);
            mouseRay.origin = camera.transform.position + Vector3.Reflect(mouseRay.origin - camera.transform.position, camera.transform.up);
            mouseRay.direction = Vector3.Reflect(mouseRay.direction, camera.transform.up);
            return mouseRay;
        }
        private static bool CanTraceComplex(GameObject gameObject)
        {
            var colliders = gameObject.GetComponentsInChildren<Collider>();
            for (int i = 0; i <= colliders.Length; i++)
            {
                if (i == colliders.Length)
                {
                    return false;
                }
                if (colliders[i].gameObject.activeInHierarchy && colliders[i].enabled && !colliders[i].isTrigger &&
                    (!(colliders[i] is MeshCollider) || ((MeshCollider)colliders[i]).convex))
                {
                    break;
                }
            }

            return true;
        }


        public static void RaycastObjectsToGround(GameObject[] gameObjects)
        {
            for (int i = 0; i < gameObjects.Length; i++)
                RaycastObjectToGround(gameObjects[i]);
        }
        public static void RaycastObjectToGround(GameObject gameObject)
        {
            if (!CanTraceComplex(gameObject))
            {
                SnapObjectToGround(gameObject);
                return;
            }

            var rigidbody = gameObject.GetComponent<Rigidbody>();
            bool shouldRemoveRigidbody = false;
            if (!rigidbody)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
                shouldRemoveRigidbody = true;
            }

            if (rigidbody.SweepTest(Vector3.down, out RaycastHit hit))
            {
                Undo.RecordObject(gameObject.transform, k_Undo_RaycastToGround);
                gameObject.transform.position += MMath.Max(0, hit.distance - k_SnapTolerance) * Vector3.down;
            }

            if (shouldRemoveRigidbody)
            {
                Object.DestroyImmediate(rigidbody);
            }
        }

        public static void SnapObjectsToGround(GameObject[] gameObjects)
        {
            for (int i = 0; i < gameObjects.Length; i++)
                SnapObjectToGround(gameObjects[i]);
        }
        public static void SnapObjectToGround(GameObject gameObject)
        {
            Vector3 snapPosition = gameObject.transform.position;

            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);
            if (Physics.Raycast(gameObject.transform.position, Vector3.down, out RaycastHit hitInfo, float.PositiveInfinity, layerMask, QueryTriggerInteraction.Ignore))
                snapPosition += Vector3.down * MMath.Max(0, hitInfo.distance - k_SnapTolerance);

            Undo.RecordObject(gameObject.transform, k_Undo_RaycastToGround);
            gameObject.transform.position = snapPosition;
        }

        public static void RaycastObjectsToMouse(GameObject[] gameObjects, Camera camera)
        {
            for (int i = 0; i < gameObjects.Length; i++)
                RaycastObjectToMouse(gameObjects[i], camera);
        }
        public static void RaycastObjectToMouse(GameObject gameObject, Camera camera)
        {
            if (!CanTraceComplex(gameObject))
            {
                SnapObjectToMouse(gameObject, camera);
                return;
            }

            var rigidbody = gameObject.GetComponent<Rigidbody>();
            bool shouldRemoveRigidbody = false;
            if (!rigidbody)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
                shouldRemoveRigidbody = true;
            }

            Ray mouseRay = GetMouseRay(camera);
            Vector3 oldPos = gameObject.transform.position;
            rigidbody.position = mouseRay.origin;

            Undo.RecordObject(gameObject.transform, k_Undo_RaycastToMouse);
            if (rigidbody.SweepTest(mouseRay.direction, out RaycastHit hitInfo))
            {
                gameObject.transform.position = mouseRay.GetPoint(hitInfo.distance) + hitInfo.normal * k_SnapTolerance;
                Physics.SyncTransforms();
            }
            else
            {
                gameObject.transform.position = oldPos;
            }


            if (shouldRemoveRigidbody)
            {
                Object.DestroyImmediate(rigidbody);
            }
        }

        public static void SnapObjectsToMouse(GameObject[] gameObjects, Camera camera)
        {
            for (int i = 0; i < gameObjects.Length; i++)
                SnapObjectToMouse(gameObjects[i], camera);
        }
        public static void SnapObjectToMouse(GameObject gameObject, Camera camera)
        {
            Vector3 snapPosition = gameObject.transform.position;
            Ray mouseRay = GetMouseRay(camera);

            // TODO ignore self collisions

            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);
            if (Physics.Raycast(mouseRay.origin, mouseRay.direction, out RaycastHit hitInfo, float.PositiveInfinity, layerMask, QueryTriggerInteraction.Ignore))
                snapPosition = mouseRay.GetPoint(hitInfo.distance) + hitInfo.normal * k_SnapTolerance;

            Undo.RecordObject(gameObject.transform, k_Undo_RaycastToMouse);
            gameObject.transform.position = snapPosition;
        }
    }
}
