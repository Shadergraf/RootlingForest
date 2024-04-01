using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions
{

    [Category("✫ Utility")]
    public class WaitRandom : ActionTask
    {

        public BBParameter<float> waitTimeMin = 1f;
        public BBParameter<float> waitTimeMax = 2f;
        public CompactStatus finishStatus = CompactStatus.Success;

        private float waitTime;

        protected override string info {
            get { return string.Format("Wait {0}-{1} sec.", waitTimeMin, waitTimeMax); }
        }

        protected override void OnExecute()
        {
            waitTime = Mathf.Lerp(waitTimeMin.value, waitTimeMax.value, Random.value);
        }

        protected override void OnUpdate() {
            if ( elapsedTime >= waitTime ) {
                EndAction(finishStatus == CompactStatus.Success ? true : false);
            }
        }
    }
}