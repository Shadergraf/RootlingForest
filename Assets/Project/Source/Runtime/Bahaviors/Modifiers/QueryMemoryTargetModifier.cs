using Manatea.RootlingForest;
using System;
using Unity.Behavior;
using UnityEngine;
using Modifier = Unity.Behavior.Modifier;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "QueryMemoryTarget",
    story: "[Self] remembers [Target] that fits [Query]",
    category: "Flow/Conditional",
    id: "70985f1ac2b24d5436031f0aec6e082d")]
public partial class QueryMemoryTargetModifier : Modifier
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<MemoryQuery> Query;

    protected override Status OnStart()
    {
        if (Child == null)
        {
            return Status.Failure;
        }

        bool success = TestQuery();
        if (!success)
        {
            return Status.Failure;
        }

        var status = StartNode(Child);
        if (status == Status.Failure || status == Status.Success)
            return Status.Running;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        bool success = TestQuery();
        if (!success)
        {
            return Status.Failure;
        }

        Status status = Child.CurrentStatus;
        if (status == Status.Failure || status == Status.Success)
        {
            var newStatus = StartNode(Child);
            if (newStatus == Status.Failure || newStatus == Status.Success)
                return Status.Running;
        }
        return Status.Running;
    }

    private bool TestQuery()
    {
        if (!Self.Value)
        {
            return false;
        }
        MemoryComponent memory = Self.Value.GetComponentInChildren<MemoryComponent>();
        if (!memory)
        {
            return false;
        }

        bool success = memory.Query(Query.Value, out MemoryMemento memento);
        if (!success || !memento.AssociatedObject)
        {
            return false;
        }

        Target.Value = memento.AssociatedObject;

        return true;
    }
}

