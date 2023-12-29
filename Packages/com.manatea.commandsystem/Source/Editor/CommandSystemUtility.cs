using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Manatea.CommandSystem
{
    public static class CommandSystemUtility
    {
        [DidReloadScripts]
        private static void RefetchCommand()
        {
            Type t = typeof(CommandManager);
            t.InvokeMember("Init", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, null);
        }
    }
}