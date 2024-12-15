using Manatea.RootlingForest;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetAbilityTarget", story: "Set [Ability] [Target]", category: "Action", id: "2cc7944e94f472389f9e8d5e81aed266")]
public partial class SetAbilityTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GrabAbility> Ability;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnStart()
    {
        var ability = Ability.Value.GetComponentInChildren<GrabAbility>();
        if (!ability)
            return Status.Failure;

        ability.Target = Target.Value;

        if (ability.Target)
            return Status.Success;
        else
            return Status.Failure;
    }
}

