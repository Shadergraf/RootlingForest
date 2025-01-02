using Manatea.GameplaySystem;
using Manatea.RootlingForest;
using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasMemoryOf", story: "[Agent] has memory of [Query]", category: "Conditions", id: "9aa66390b085642437480faae0c758b4")]
public partial class AccessMemoryCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<MemoryQuery> Query;

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
        //Agent.Value.Get
    }

    public override void OnEnd()
    {
    }
}
