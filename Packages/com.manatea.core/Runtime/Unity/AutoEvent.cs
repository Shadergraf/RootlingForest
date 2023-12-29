using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Manatea
{
    public class AutoEvent : MonoBehaviour
    {
        public float time = 1;
        public UpdateCycle updateCycle = UpdateCycle.Update;
        public UnityEvent OnEvent;

        private float timer = 0;
        private bool eventTriggered;

        private void Update()
        {
            if (updateCycle == UpdateCycle.Update)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.UpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                Trigger();
        }

        private void LateUpdate()
        {
            if (updateCycle == UpdateCycle.LateUpdate)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.LateUpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                Trigger();
        }

        private void FixedUpdate()
        {
            if (updateCycle == UpdateCycle.FixedUpdate)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.FixedUpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                Trigger();
        }


        private void Trigger()
        {
            if (eventTriggered)
                return;
            OnEvent.Invoke();
        }
    }
}