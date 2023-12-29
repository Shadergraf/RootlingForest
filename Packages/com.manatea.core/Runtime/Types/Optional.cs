using System;
using UnityEngine;

namespace Manatea
{
    [Serializable]
    public struct Optional<T>
    {
        [SerializeField]
        public bool hasValue;
        [SerializeField]
        public T value;

        /// <summary>
        /// Initializes the value without setting the hasValue field
        /// </summary>
        public Optional(T initialValue)
        {
            hasValue = false;
            value = initialValue;
        }
        /// <summary>
        /// Initializes the value and hasValue field
        /// </summary>
        public Optional(T initialValue, bool initialHasValue)
        {
            hasValue = initialHasValue;
            value = initialValue;
        }

        public static implicit operator Optional<T>(T v) => new Optional<T>(v);
    }
}
