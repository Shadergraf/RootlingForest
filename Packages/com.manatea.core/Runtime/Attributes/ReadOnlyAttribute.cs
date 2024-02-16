using UnityEditor;
using UnityEngine;

namespace Manatea
{
    /// <summary>
    /// Disables this attribute for use in the editor
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyAttribute()
        { }
    }
}