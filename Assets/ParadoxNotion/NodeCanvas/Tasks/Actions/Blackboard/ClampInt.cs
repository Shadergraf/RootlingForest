using System;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;


namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Blackboard")]
    [Description("Clamp a blackboard int variable")]
    public class ClampInt : ActionTask
    {

        [BlackboardOnly]
        public BBParameter<int> value;
        public BBParameter<int> clampValueA;
        public BBParameter<int> clampValueB;

        protected override string info {
            get { return string.Format("Clamp {0} between {1} and {2}", value, Math.Min(clampValueA.value, clampValueB.value), Math.Max(clampValueA.value, clampValueB.value)); }
        }

        protected override void OnExecute()
        {
            int min = Math.Min(clampValueA.value, clampValueB.value);
            int max = Math.Max(clampValueA.value, clampValueB.value);
            value.value = Math.Max(Math.Min(value.value, max), min);
            EndAction(true);
        }
    }
}