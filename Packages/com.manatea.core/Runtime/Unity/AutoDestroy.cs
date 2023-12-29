using UnityEngine;
using System.Collections;

namespace Manatea
{
    public class AutoDestroy : MonoBehaviour
    {
        public float time = 1;
        public UpdateCycle updateCycle = UpdateCycle.Update;

        private float timer = 0;

        private void Update()
        {
            if (updateCycle == UpdateCycle.Update)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.UpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (updateCycle == UpdateCycle.LateUpdate)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.LateUpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (updateCycle == UpdateCycle.FixedUpdate)
                timer += Time.deltaTime;
            if (updateCycle == UpdateCycle.FixedUpdateUnscaled)
                timer += Time.unscaledDeltaTime;

            if (timer >= time)
                Destroy(gameObject);
        }
    }
}