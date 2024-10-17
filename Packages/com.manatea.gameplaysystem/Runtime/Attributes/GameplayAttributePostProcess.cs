using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public interface IGameplayAttributePostProcessor
    {
        public void Process(GameplayAttributeOwner attributeOwner, GameplayAttribute attribute, ref float value);
    }

    [Serializable]
    public class ValuePostProcessor : IGameplayAttributePostProcessor
    {
        public GameplayAttributePostProcessorLimiter Limiter;
        public float Value;
        public void Process(GameplayAttributeOwner attributeOwner, GameplayAttribute attribute, ref float value)
        {
            switch (Limiter)
            {
                case GameplayAttributePostProcessorLimiter.Min:
                    value = MMath.Min(value, Value);
                    return;
                case GameplayAttributePostProcessorLimiter.Max:
                    value = MMath.Max(value, Value);
                    return;
            }
            throw new Exception("Unsupported operation Exception");
        }
    }

    [Serializable]
    public class AttributePostProcessor : IGameplayAttributePostProcessor
    {
        public GameplayAttributePostProcessorLimiter Limiter;
        public GameplayAttribute Attribute;

#if UNITY_EDITOR
        private static int s_RecursionCount = 0;
#endif

        public void Process(GameplayAttributeOwner attributeOwner, GameplayAttribute attribute, ref float value)
        {
            float attributeValue = value;
            Debug.Assert(attribute != Attribute, "Attribute PostProcess cannot be dependent on same attribute");

#if UNITY_EDITOR
            if (s_RecursionCount > 32)
            {
                Debug.LogError(string.Format("Attribute PostProcessor recursion detected!", attribute), attributeOwner.gameObject);
                return;
            }
            s_RecursionCount++;
#endif
            attributeOwner.TryGetAttributeEvaluatedValue(Attribute, out attributeValue);
#if UNITY_EDITOR
            s_RecursionCount--;
#endif

            switch (Limiter)
            {
                case GameplayAttributePostProcessorLimiter.Min:
                    value = MMath.Min(value, attributeValue);
                    return;
                case GameplayAttributePostProcessorLimiter.Max:
                    value = MMath.Max(value, attributeValue);
                    return;
            }
            throw new Exception("Unsupported operation Exception");
        }
    }

    public enum GameplayAttributePostProcessorLimiter
    {
        Max = 1,
        Min = 0,
    }
}
