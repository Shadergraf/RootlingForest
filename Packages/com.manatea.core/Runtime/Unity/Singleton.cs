using System;
using System.Reflection;
using UnityEngine;

namespace Manatea
{
    // TODO this creates more problems than it solves...

    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T s_Instance;
        public static T Instance
        {
            get
            {
                if (!s_Instance)
                {
                    if (!GetSetup("LazyInstantiation"))
                        return null;
                    GameObject go = new GameObject(typeof(T).Name);
                    s_Instance = go.AddComponent<T>();
                }
                return s_Instance;
            }
        }


        /// <summary>
        /// Should this singleton be created on the fly?
        /// </summary>
        /// <remarks>Hide this default property with the <i>new</i> keyword in a subclass.</remarks>
        public static bool LazyInstantiation => false;

        /// <summary>
        /// Should duplicate instantiations of this singleton automatically be destroyed?
        /// </summary>
        /// <remarks>Hide this default property with the <i>new</i> keyword in a subclass.</remarks>
        public static bool DestroyDuplicates => false;

        /// <summary>
        /// Should duplicate instantiation attempts be logged to the console?
        /// </summary>
        /// <remarks>Hide this default property with the <i>new</i> keyword in a subclass.</remarks>
        public static bool LogDuplicatesError => false;

        /// <summary>
        /// Should this singleton be marked DontDestroyOnLoad?
        /// </summary>
        /// <remarks>Hide this default property with the <i>new</i> keyword in a subclass.</remarks>
        public static bool MarkDontDestroyOnLoad => false;


        public bool IsValid => Instance;
        public bool EnsureValidity() => Instance;

        private static bool m_ApplicationQuit;


        protected virtual void Awake()
        {
            // Duplicate instance
            if (s_Instance)
            {
                if (GetSetup("DestroyDuplicates"))
                    Destroy(gameObject);
                if (GetSetup("LogDuplicatesError"))
                    Debug.LogError("A singleton already exists for " + GetType().FullName, this);
                return;
            }

            // Generic type doesn't match
            if (!(this is T))
            {
                Debug.LogError("Singleton instance does not match it's generic type!", this);
                return;
            }

            if (GetSetup("MarkDontDestroyOnLoad"))
                DontDestroyOnLoad(gameObject);

            s_Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }


        private static bool GetSetup(string property)
        {
            Type t = typeof(T);
            var prop = t.GetProperty(property);
            if (prop == null)
                return false;
            return (bool)prop.GetValue(null);
        }
    }
}