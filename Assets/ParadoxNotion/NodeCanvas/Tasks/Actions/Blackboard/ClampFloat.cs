using System;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;


namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Blackboard")]
    [Description("Clamp a blackboard float variable")]
    public class ClampFloat : ActionTask
    {

        [BlackboardOnly]
        public BBParameter<float> value;
        public BBParameter<float> clampValueA;
        public BBParameter<float> clampValueB;

        protected override string info {
            get { return string.Format("Clamp {0} between {1} and {2}", value, Math.Min(clampValueA.value, clampValueB.value), Math.Max(clampValueA.value, clampValueB.value)); }
        }

        protected override void OnExecute()
        {
            float min = Math.Min(clampValueA.value, clampValueB.value);
            float max = Math.Max(clampValueA.value, clampValueB.value);
            value.value = Math.Max(Math.Min(value.value, max), min);
            EndAction(true);
        }
    }
}