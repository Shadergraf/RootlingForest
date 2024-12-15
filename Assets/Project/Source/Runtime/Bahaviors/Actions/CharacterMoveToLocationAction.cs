using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using Manatea.RootlingForest;
using Manatea;
using Unity.AI.Navigation;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CharacterMoveToObject", story: "[Character] moves to [Object]", category: "Action", id: "6f10b7ae33d8f006f18c1659a12fb92f")]
public partial class CharacterMoveToObjectAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Character;
    [SerializeReference] public BlackboardVariable<GameObject> Object;

    private NavMeshPath m_Path;
    private Vector3[] m_Corners = new Vector3[16];
    private int m_CornerIndex = 0;

    private CharacterMovement m_Movement;

    private Vector3 m_CurrentDir;
    [CreateProperty] private float m_Time;


    protected override Status OnStart()
    {
        if (!Character.Value || !Object.Value)
            return Status.Failure;

        m_Movement = Character.Value.GetComponentInChildren<CharacterMovement>();
        if (!m_Movement)
            return Status.Failure;


        m_Path = new NavMeshPath();
        //NavMesh.CalculatePath(Character.Value.transform.position, Object.Value.transform.position, 1, m_Path);
        bool pathCalculated = CalculatePath();
        if (!pathCalculated || m_Path.status == NavMeshPathStatus.PathInvalid)
        {
            return Status.Failure;
        }
        m_Corners = m_Path.corners;
        m_CornerIndex = 0;
        m_Time = 0;

        m_CurrentDir = (m_Corners[1] - m_Corners[0]).normalized;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        m_Time += Time.deltaTime;
        if (m_Time >= 0.5f)
        {
            //NavMesh.CalculatePath(Character.Value.transform.position, Object.Value.transform.position, 0, m_Path);
            bool pathCalculated = CalculatePath();
            if (!pathCalculated || m_Path.status == NavMeshPathStatus.PathInvalid)
            {
                return Status.Failure;
            }
            m_Corners = m_Path.corners;
            m_CornerIndex = 0;
            m_Time = 0;
        }

        if (Vector3.Distance(Character.Value.transform.position, m_Corners[m_CornerIndex + 1]) < 0.4f && m_CornerIndex < m_Corners.Length - 1)
        {
            m_CornerIndex++;
        }

        m_Movement.Move((m_Corners[m_CornerIndex + 1] - Character.Value.transform.position).FlattenY().normalized);

        for (int i = 0; i < m_Corners.Length - 1; i++)
        {
            Debug.DrawLine(m_Corners[i], m_Corners[i + 1]);
        }
        return Status.Running;
    }

    private bool CalculatePath()
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter();
        filter.agentTypeID = NavMesh.GetSettingsByIndex(2).agentTypeID;
        filter.SetAreaCost(4, 3);
        filter.SetAreaCost(9, 50);
        filter.SetAreaCost(18, 2000);
        filter.SetAreaCost(27, 2000);
        filter.areaMask = 0;
        filter.areaMask += 1 << 0;
        filter.areaMask += 1 << 2;
        filter.areaMask += 1 << 4;
        filter.areaMask += 1 << 9;
        NavMesh.SamplePosition(Character.Value.transform.position, out NavMeshHit startHit, 1.5f, filter);
        NavMesh.SamplePosition(Object.Value.transform.position, out NavMeshHit endHit, 1.5f, filter);

        if (!startHit.hit || !endHit.hit)
        {
            return false;
        }

        NavMesh.CalculatePath(startHit.position, endHit.position, filter, m_Path);

        return m_Path.status == NavMeshPathStatus.PathComplete;
    }

    protected override void OnEnd()
    {
    }
}

