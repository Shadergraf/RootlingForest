using UnityEditor;
using UnityEngine;

namespace Manatea
{
    /// <summary>
    /// Put this attribute above any ScriptableObject field
    /// if you want to be able to create an asset of it with the press of a button.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class CreateAssetAttribute : PropertyAttribute
    { }
}