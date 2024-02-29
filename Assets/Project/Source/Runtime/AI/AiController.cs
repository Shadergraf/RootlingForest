using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AiController : CharacterController
{
    public Vector3 m_MoveDir;

    public float m_Radius = 0.5f;
    public float m_TickRate = 0.1f;

    private NavMeshPath m_CurrentPath;


    private void Awake()
    {
        m_CurrentPath = new NavMeshPath();
    }

    private void OnEnable()
    {
        StartCoroutine(UpdateTickV2());
    }


    public void Update()
    {
        CharacterMovement.Move(m_MoveDir);
    }

    private IEnumerator UpdateTick()
    {
        yield return new WaitForSeconds(Random.value);

        while (true)
        {
            m_MoveDir = transform.forward;

            Vector3 newTarget = transform.forward * m_Radius * 0.95f;
            newTarget += transform.right * MMath.Pow(Random.value, 4) * MMath.Sign(Random.value - 0.5f) * 0.6f;
            const float c_SearchRadius = 0.5f;
            if (NavMesh.SamplePosition(transform.position + newTarget, out NavMeshHit hit, c_SearchRadius, -1))
            {
                DebugHelper.DrawWireSphere(transform.position + newTarget, c_SearchRadius, Color.blue, m_TickRate);
                DebugHelper.DrawWireSphere(hit.position, 0.1f, Color.green, m_TickRate);
                Vector3 move = (hit.position - transform.position).FlattenY() * 2;
                m_MoveDir = move.ClampMagnitude(0, 1);

                if (m_MoveDir.magnitude <= 0.1f)
                {
                    m_MoveDir = -transform.forward;
                    Debug.Log("Turn back!");
                }
            }
            else
            {
                m_MoveDir = -transform.forward;
                Debug.Log("Turn back!");
            }

            yield return new WaitForSeconds(m_TickRate);
        }
    }

    private IEnumerator UpdateTickV2()
    {
        yield return new WaitForSeconds(Random.value);

        while (true)
        {
            m_MoveDir = transform.forward;

            Vector3 newTarget = transform.forward * m_Radius * 2.0f;
            newTarget += transform.right * MMath.Pow(Random.value, 4) * MMath.Sign(Random.value - 0.5f) * 1.0f;
            const float c_SearchRadius = 0.5f;

            bool moveWorked = true;
            for (int i = 0; i <= 3; i++)
            {
                if (i == 3)
                {
                    moveWorked = false;
                    break;
                }
                if (NavMesh.CalculatePath(transform.position, transform.position + newTarget * (3 - i) / 3f, -1, m_CurrentPath))
                {
                    Vector3 endPos = m_CurrentPath.corners[m_CurrentPath.corners.Length - 1];
                    DebugHelper.DrawWireSphere(transform.position + newTarget, c_SearchRadius, Color.blue, m_TickRate);
                    DebugHelper.DrawWireSphere(endPos, 0.1f, Color.green, m_TickRate);
                    Vector3 move = (endPos - transform.position).FlattenY() * 2;
                    m_MoveDir = move.ClampMagnitude(0, 1);

                    if (m_MoveDir.magnitude < 0.1f)
                    {
                        moveWorked = false;
                    }
                    break;
                }
            }

            if (!moveWorked)
            {
                m_MoveDir = -transform.forward;
                Debug.Log("Turn back!");
            }

            yield return new WaitForSeconds(m_TickRate);
        }
    }
}
