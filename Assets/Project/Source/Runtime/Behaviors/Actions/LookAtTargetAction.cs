using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Manatea.RootlingForest;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Agent looks at Target", story: "[Agent] looks at [Target]", category: "Action", id: "6626458aabdce4ee6f092372616cd11e")]
public partial class LookAtTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnStart()
    {
        if (!Agent.Value || !Target.Value)
            return Status.Failure;

        var charMove = Agent.Value.GetComponent<CharacterMovement>();
        if (!charMove)
            return Status.Failure;

        charMove.SetTargetRotation(Target.Value.transform.position - Agent.Value.transform.position);

        return Status.Success;
    }
}

