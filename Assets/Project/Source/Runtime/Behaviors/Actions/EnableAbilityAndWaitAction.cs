using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EnableAbilityAndWait", story: "[Self] enables [Ability]", category: "Action", id: "039e8d7581253b40318ca10b9795b01f")]
public partial class EnableAbilityAndWaitAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<BaseAbility> Ability;

    protected override Status OnStart()
    {
        if (!Ability.Value)
            return Status.Failure;

        Ability.Value.enabled = true;

        if (Ability.Value.enabled)
            return Status.Running;
        else
            return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Ability.Value.enabled)
            return Status.Running;
        else
            return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
