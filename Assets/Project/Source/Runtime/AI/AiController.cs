using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Loading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;

namespace Manatea.RootlingForest
{
    public class AiController : CharacterController
    {
        public Vector3 m_MoveDir;

        public float m_Radius = 0.5f;
        public float m_TickRate = 0.1f;
        public Transform[] m_AfraidOf;
        public GameplayAttribute m_MoveSpeedAttribute;
        public GameplayAttribute m_RotationSpeedAttribute;

        private NavMeshPath m_CurrentPath;
        private Vector3[] m_Path;

        private GameplayAttributeOwner m_AttributeOwner;

        private GameplayAttributeModifier m_MoveSpeedMod;
        private GameplayAttributeModifier m_RotationSpeedMod;

        public State CurrentState = State.Idle;

        public enum State
        {
            Idle,       // Normal behavior
            Sleeping,   // Sleepy boy
            Afraid,     // Rund away from something
            Panic,      // Random movement, while in panic
            Hungry,     // Run to food and eat
        }


        private void Awake()
        {
            m_CurrentPath = new NavMeshPath();
        }

        private void OnEnable()
        {
            StartCoroutine(UpdateTick());

            m_AttributeOwner = GetComponent<GameplayAttributeOwner>();

            m_MoveSpeedMod = new GameplayAttributeModifier()
            {
                Type = GameplayAttributeModifierType.Multiplicative,
                Value = 1,
            };
            m_AttributeOwner.AddAttributeModifier(m_MoveSpeedAttribute, m_MoveSpeedMod);

            m_RotationSpeedMod = new GameplayAttributeModifier()
            {
                Type = GameplayAttributeModifierType.Multiplicative,
                Value = 1,
            };
            m_AttributeOwner.AddAttributeModifier(m_RotationSpeedAttribute, m_RotationSpeedMod);
        }
        private void OnDisable()
        {
        }


        public void Update()
        {
            CharacterMovement.Move(m_MoveDir);
        }

        private IEnumerator UpdateTick()
        {
            yield return new WaitForSeconds(Random.value * m_TickRate);

            while (true)
            {
                m_MoveSpeedMod.Value = 1;
                m_MoveDir = Vector3.zero;

                switch (CurrentState)
                {
                    case State.Idle:
                        yield return TickIdle();
                        break;
                    case State.Afraid:
                        yield return TickAfraid();
                        break;
                    case State.Panic:
                        yield return TickPanic();
                        break;
                }

                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator TickIdle()
        {
            Vector3 newTargetDir = transform.forward * m_Radius * 1.5f;
            newTargetDir += transform.right * MMath.Pow(Random.value, 4) * MMath.Sign(Random.value - 0.5f) * 2.0f;

            Vector3 targetPos = GetSensibleNextMove(transform.position, transform.position + newTargetDir);
            m_MoveDir = (targetPos - transform.position).FlattenY().ClampMagnitude(0, 1);

            // Random turn
            if (MPropabilities.TimedProbability(0.15f, m_TickRate, Random.value))
            {
                m_MoveDir = Random.insideUnitCircle.XZtoXYZ();
            }

            // Turn around when can't going forward
            if (m_MoveDir.magnitude < 0.5f)
            {
                m_MoveDir *= -1f;
            }

            yield return new WaitForSeconds(m_TickRate);
        }
        private IEnumerator TickAfraid()
        {
            m_MoveSpeedMod.Value = 4;

            Vector3 newTargetDir = transform.forward * m_Radius * 1.5f;
            newTargetDir += transform.right * 2.5f;
            if (m_AfraidOf.Length > 0)
            {
                newTargetDir = (transform.position - m_AfraidOf[0].position).FlattenY().normalized;

                if (Vector3.Distance(m_AfraidOf[0].position, transform.position) > 6)
                {
                    CurrentState = State.Idle;
                }
            }

            Vector3 targetPos = GetSensibleNextMove(transform.position, transform.position + newTargetDir);
            m_MoveDir = (targetPos - transform.position).FlattenY().normalized;
            m_MoveDir = Vector3.Lerp(m_MoveDir, newTargetDir, 0.75f);
            m_MoveDir = (m_MoveDir + Random.insideUnitCircle.XZtoXYZ() * 0.25f).normalized;

            yield return new WaitForSeconds(0.1f);
        }
        private IEnumerator TickPanic()
        {
            m_MoveSpeedMod.Value = 3.5f;
            m_RotationSpeedMod.Value = 2.5f;

            m_MoveDir = (m_MoveDir * 0.25f + Random.insideUnitCircle.XZtoXYZ()).normalized;

            yield return new WaitForSeconds(0.15f);
        }

        private Vector3 GetSensibleNextMove(Vector3 currentPosition, Vector3 targetPosition)
        {
            // Create sample points on NavMesh
            Vector3[] testPositions = new Vector3[4];
            for (int i = 0; i <= 3; i++)
            {
                Vector3 target = Vector3.Lerp(currentPosition, targetPosition, i / 3f);
                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2, -1))
                {
                    DebugHelper.DrawWireSphere(target, 0.05f, Color.green, m_TickRate, false);
                    DebugHelper.DrawWireSphere(hit.position, 0.05f, Color.blue, m_TickRate, false);
                    testPositions[i] = hit.position;
                }
                else
                {
                    DebugHelper.DrawWireSphere(target, 0.05f, Color.red, m_TickRate, false);
                }
            }

            // Evaluate Sample points
            float expectedDistance = Vector3.Distance(currentPosition, targetPosition) / 3;
            Vector3 bestTarget = testPositions[0];
            for (int i = 0; i < 3; i++)
            {
                float nextDistance = Vector3.Distance(testPositions[i], testPositions[i + 1]);
                if (nextDistance > expectedDistance * 1.3f)
                {
                    break;
                }
                bestTarget = testPositions[i + 1];
            }

            DebugHelper.DrawWireSphere(bestTarget, 0.1f, Color.yellow, m_TickRate, false);
            return bestTarget;
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null && collision.rigidbody.gameObject != gameObject)
            {
                CurrentState = State.Afraid;
                if (m_AfraidOf.Length == 0)
                {
                    m_AfraidOf = new Transform[1];
                }
                m_AfraidOf[0] = collision.rigidbody.transform;
            }
        }
    }
}
