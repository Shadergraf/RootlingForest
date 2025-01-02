using Manatea.GameplaySystem;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "WaitForGameplayEvent", story: "[Agent] waits for [GameplayEvent]", category: "Events", id: "43430e55da1af2f0a066eff92bf7890d")]
public partial class WaitForGameplayEventAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameplayEvent> GameplayEvent;

    private bool m_EventReceived;

    protected override Status OnStart()
    {
        var eventReceiver = Agent.Value.GetComponent<GameplayEventReceiver>();
        if (!eventReceiver)
            return Status.Failure;

        eventReceiver.RegisterListener(GameplayEvent.Value, EventCallback);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (m_EventReceived)
            return Status.Success;

        return Status.Running;
    }

    private void EventCallback(object payload)
    {
        m_EventReceived = true;
    }
}

