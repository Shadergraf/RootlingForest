using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Manatea.RootlingForest;

[System.Serializable, GeneratePropertyBag]
[NodeDescription(name: "Look in Direction", story: "[Agent] looks in direction [Direction]", category: "Action", id: "94e54c0a7a9150424cbc00d3540f0ab9")]
public partial class LookInDirectionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> Direction = new(Vector3.forward);
    [SerializeReference] public BlackboardVariable<bool> Relative = new(false);
    [SerializeReference] public BlackboardVariable<Vector3> DirectionVariation = new(Vector3.zero);

    private CharacterMovement m_CharacterMovement;


    protected override Status OnStart()
    {
        m_CharacterMovement = Agent.Value.GetComponentInChildren<CharacterMovement>();
        if (!m_CharacterMovement)
        {
            Debug.Assert(m_CharacterMovement, "No CharacterMovement component found!", Agent.Value);
            return Status.Failure;
        }

        Vector3 targetLookAt = Direction.Value;
        targetLookAt += Vector3.Scale(DirectionVariation.Value, Random.insideUnitSphere);
        targetLookAt.Normalize();
        if (Relative.Value)
            targetLookAt = m_CharacterMovement.transform.TransformDirection(targetLookAt);
        m_CharacterMovement.SetTargetRotation(targetLookAt);

        return Status.Success;
    }
}

