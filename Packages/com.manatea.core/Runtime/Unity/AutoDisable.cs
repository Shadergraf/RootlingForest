using UnityEngine;
using System.Collections;

namespace Manatea
{
    public class AutoDisable : MonoBehaviour
    {
        public float time = 1;
        public UpdateCycle updateCycle = UpdateCycle.Update;

        private float timer;


        private void OnEnable()
        {
            timer = 0;
        }

        private void Update()
        {
            if (updateCycle == UpdateCycle.Update)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.UpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (updateCycle == UpdateCycle.LateUpdate)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.LateUpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (updateCycle == UpdateCycle.FixedUpdate)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.FixedUpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                gameObject.SetActive(false);
        }
    }
}