using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manatea.CommandSystem
{
    public static class RuntimeUtilityCommands
    {
        [Command("Slomo")]
        public static void SetTimeScale(float scale)
        {
            scale = Mathf.Max(0, scale);
            Time.timeScale = scale;
        }

        [Command]
        public static void SetFixedDeltaTime(float scale)
        {
            scale = Mathf.Max(0.001f, scale);
            Time.fixedDeltaTime = scale;
        }
    }
}
