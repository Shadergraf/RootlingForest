using System;
using System.Reflection;
using UnityEngine;

namespace Manatea
{
    /// <summary>
    /// A ScriptableObject that automatically sets DontUnloadUnusedAsset to keep itself in memory
    /// </summary>
    public abstract class PersistentScriptableObject : ScriptableObject
    {
        protected virtual void OnEnable()
        {
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }
    }
}