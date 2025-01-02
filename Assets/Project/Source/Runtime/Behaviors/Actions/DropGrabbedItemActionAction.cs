using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Manatea.RootlingForest;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DropGrabbedItemAction", story: "[Agent] drops grabbed item", category: "Action", id: "d7b1847033671d1840634f815d3d3da4")]
public partial class DropGrabbedItemActionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        var ability = Agent.Value.GetComponentInChildren<GrabAbility>();
        if (!ability)
            return Status.Failure;

        ability.Drop();

        return Status.Success;
    }
}

