using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Manatea.RootlingForest;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ThrowGrabbedItem", story: "[Agent] throws grabbed item", category: "Action", id: "e2b022137b3fe16207f7af0df42efc84")]
public partial class ThrowGrabbedItemAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        var ability = Agent.Value.GetComponentInChildren<GrabAbility>();
        if (!ability)
            return Status.Failure;

        ability.Throw();

        return Status.Success;
    }
}

