using System;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;


namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Blackboard")]
    [Description("Max a blackboard int variable")]
    public class MaxInt : ActionTask
    {

        [BlackboardOnly]
        public BBParameter<int> valueA;
        public BBParameter<int> valueB;

        protected override string info {
            get { return string.Format("Set {0} as max of {1} and {2}", valueA, valueA, valueB); }
        }

        protected override void OnExecute()
        {
            valueA.value = Math.Max(valueA.value, valueB.value);
            EndAction(true);
        }
    }
}