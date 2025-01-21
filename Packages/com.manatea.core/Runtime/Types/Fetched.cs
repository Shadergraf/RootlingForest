using System;
using UnityEngine;

namespace Manatea
{
    [Serializable]
    public struct Fetched<T> where T : Component
    {
        [SerializeField]
        public FetchingType fetchingType;
        [SerializeField]
        public T value;

        /// <summary>
        /// Initializes the FetchingType field
        /// </summary>
        public Fetched(FetchingType fetchingType)
        {
            this.fetchingType = fetchingType;
            this.value = null;
        }
        /// <summary>
        /// Initializes the value without setting the FetchingType field
        /// </summary>
        public Fetched(T initialValue)
        {
            this.fetchingType = FetchingType.Manual;
            this.value = initialValue;
        }
        /// <summary>
        /// Initializes the value and FetchingType field
        /// </summary>
        public Fetched(T initialValue, FetchingType fetchingType)
        {
            this.fetchingType = fetchingType;
            this.value = initialValue;
        }

        public static implicit operator T(Fetched<T> wrapper)
        {
            return wrapper.value;
        }

        public bool FetchFrom(GameObject referenceObject)
        {
            switch (fetchingType)
            {
                case FetchingType.Manual: break;
                case FetchingType.OnObject:
                    value = referenceObject.GetComponent<T>();
                    break;
                case FetchingType.InParents:
                    value = referenceObject.GetComponentInParent<T>();
                    break;
                case FetchingType.InChildren:
                    value = referenceObject.GetComponentInChildren<T>();
                    break;
            }

            return value != null;
        }

        public static implicit operator Fetched<T>(T v) => new Fetched<T>(v);

    }
    public enum FetchingType
    {
        Manual = 0,
        OnObject = 1,
        InParents = 2,
        InChildren = 3,
    }
}
