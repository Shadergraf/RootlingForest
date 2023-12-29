using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{

    public abstract class PropertyGenerator : ScriptableObject
    {
        public static event System.Action<ScriptableObject> OnGeneratorChanged;

        public virtual void OnValidate()
        {
            OnGeneratorChanged?.Invoke(this);
        }
    }

    public abstract class PropertyGenerator<T> : PropertyGenerator
    {
        public abstract T GeneratePropertyValue(int seed);

    }
}