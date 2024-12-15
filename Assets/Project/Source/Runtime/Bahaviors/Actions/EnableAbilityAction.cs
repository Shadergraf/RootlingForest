using Manatea.RootlingForest;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EnableAbility", story: "[Agent] enables [Ability]", category: "Action", id: "a64d7e06c45ff4f2392403a5ca0b8707")]
public partial class EnableAbilityAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GrabAbility> Ability;

    protected override Status OnStart()
    {
        var ability = Agent.Value.GetComponentInChildren<GrabAbility>();
        if (!ability)
            return Status.Failure;

        ability.enabled = true;

        if (ability.enabled)
            return Status.Success;
        else
            return Status.Failure;
    }
}

