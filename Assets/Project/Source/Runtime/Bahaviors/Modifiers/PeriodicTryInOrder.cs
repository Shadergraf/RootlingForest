using Manatea.RootlingForest;
using System;
using Unity.Properties;
using UnityEngine;
using Manatea;

namespace Unity.Behavior
{
    /// <summary>
    /// Executes branches in order until one succeeds.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Periodic Try In Order",
        description: "Executes branches in order until one succeeds. Periodically checks all children for a better candidate",
        story: "Re-check with [Period]",
        icon: "Icons/selector",
        category: "Flow",
        id: "a13d7b463ba070866dbec12a6eb698a3")]
    internal partial class PeriodicSelectorComposite : Composite
    {
        [CreateProperty] private int m_CurrentChild;
        [SerializeReference] public BlackboardVariable<float> Period = new BlackboardVariable<float>(0.5f);

        [CreateProperty] private float m_Time;

        protected override Status OnStart()
        {
            m_Time = 0;
            m_CurrentChild = 0;

            if (Children.Count == 0)
                return Status.Success;

            var status = StartNode(Children[m_CurrentChild]);
            if (status == Status.Success)
                return Status.Success;
            if (status == Status.Failure)
                return Status.Running;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            m_Time += Time.deltaTime;

            if (m_Time >= Period.Value)
            {
                m_Time = 0;

                for (int i = 0; i < MMath.Min(Children.Count, m_CurrentChild); i++)
                {
                    var status = StartNode(Children[i]);
                    if (status != Status.Failure)
                    {
                        EndNode(Children[m_CurrentChild]);
                        m_CurrentChild = i;
                        break;
                    }
                }
            }

            if (m_CurrentChild >= Children.Count)
                return Status.Success;

            Status childStatus = Children[m_CurrentChild].CurrentStatus;
            if (childStatus == Status.Success)
            {
                ++m_CurrentChild;
                return Status.Success;
            }
            else if (childStatus == Status.Failure)
            {
                if (++m_CurrentChild >= Children.Count)
                    return Status.Failure;

                var status = StartNode(Children[m_CurrentChild]);
                if (status == Status.Success)
                    return Status.Success;
                if (status == Status.Failure)
                    return Status.Running;
            }
            return Status.Running;
        }
    }
}
