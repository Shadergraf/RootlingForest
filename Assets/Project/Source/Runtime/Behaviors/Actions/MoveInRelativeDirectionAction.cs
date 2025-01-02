using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Manatea.RootlingForest;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveInRelativeDirection", story: "[Agent] moves in relative direction [direction] for [Seconds] s", category: "Action", id: "9c53ccc032434530fd9fb4487730ffdc")]
public partial class MoveInRelativeDirectionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> Direction = new BlackboardVariable<Vector3>(Vector3.forward);
    [SerializeReference] public BlackboardVariable<float> Seconds = new BlackboardVariable<float>(1);
    [SerializeReference] public BlackboardVariable<bool> RotateTowardsMove = new BlackboardVariable<bool>(true);

    private CharacterMovement m_CharacterMovement;
    private float m_Timer;


    protected override Status OnStart()
    {
        m_CharacterMovement = Agent.Value.GetComponentInChildren<CharacterMovement>();
        if (!m_CharacterMovement)
        {
            Debug.Assert(m_CharacterMovement, "No CharacterMovement component found!", Agent.Value);
            return Status.Failure;
        }

        m_Timer = Seconds.Value;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        m_CharacterMovement.Move(m_CharacterMovement.transform.TransformDirection(Direction.Value), RotateTowardsMove.Value);

        m_Timer -= Time.deltaTime;
        if (m_Timer <= 0)
        {
            m_CharacterMovement.Move(Vector3.zero, RotateTowardsMove.Value);
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        m_CharacterMovement.Move(Vector3.zero, RotateTowardsMove.Value);
    }
}

