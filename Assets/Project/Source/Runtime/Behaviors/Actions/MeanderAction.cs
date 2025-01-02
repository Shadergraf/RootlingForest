using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Manatea.RootlingForest;

[System.Serializable, GeneratePropertyBag]
[NodeDescription(name: "Meander", story: "[Agent] meanders for [Duration] seconds", category: "Action", id: "6f222e7bbf347a9338cc0f9e31a3de7e")]
public partial class MeanderAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> Duration = new(1);
    [SerializeReference] public BlackboardVariable<float> DurationVariance = new(0);
    [SerializeReference] public BlackboardVariable<bool> RotateTowardsMove = new(true);
    [SerializeReference] public BlackboardVariable<float> Rotation = new(0);
    [SerializeReference] public BlackboardVariable<float> RotationVariance = new(1);

    private CharacterMovement m_CharacterMovement;
    private float m_Timer;

    private float m_RotationRate;


    protected override Status OnStart()
    {
        m_CharacterMovement = Agent.Value.GetComponentInChildren<CharacterMovement>();
        if (!m_CharacterMovement)
        {
            Debug.Assert(m_CharacterMovement, "No CharacterMovement component found!", Agent.Value);
            return Status.Failure;
        }

        m_Timer = Duration.Value + DurationVariance.Value * Random.Range(-1, 1);

        m_RotationRate = Rotation.Value + RotationVariance.Value * Random.Range(-1, 1);
        m_RotationRate *= 360;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Vector3 direction = Quaternion.Euler(0, m_RotationRate * Time.deltaTime, 0) * m_CharacterMovement.transform.forward;
        m_CharacterMovement.Move(direction, RotateTowardsMove.Value);

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

