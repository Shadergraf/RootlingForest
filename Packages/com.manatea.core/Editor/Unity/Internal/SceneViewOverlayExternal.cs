using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    public static class SceneViewOverlayExternal
    {
        public delegate void WindowFunction(UnityEngine.Object target, SceneView sceneView);

        public enum WindowDisplayOption
        {
            MultipleWindowsPerTarget,
            OneWindowPerTarget,
            OneWindowPerTitle,
        }

        readonly static Type windowFunctionType;
        readonly static Type windowDisplayOptionType;
        readonly static Type sceneViewOverlayType;
        readonly static MethodInfo windowMethod;

        static SceneViewOverlayExternal()
        {
            var assembly = typeof(Editor).Assembly;
            sceneViewOverlayType = assembly.GetType("UnityEditor.SceneViewOverlay");
            windowFunctionType = sceneViewOverlayType.GetNestedType("WindowFunction");
            windowDisplayOptionType = sceneViewOverlayType.GetNestedType("WindowDisplayOption");
            windowMethod = sceneViewOverlayType.GetMethod("Window",
            new[] {
            typeof(GUIContent),
            windowFunctionType,
            typeof(int),
            windowDisplayOptionType,
            });
        }

        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, WindowDisplayOption option)
        {
            var sceneViewFuncInstance = Cast(sceneViewFunc, windowFunctionType);
            var optionInstance = Enum.ToObject(windowDisplayOptionType, (int)option);
            windowMethod.Invoke(null,
            new object[] {
            title,
            sceneViewFuncInstance,
            order,
            optionInstance,
            });
        }

        static Delegate Cast(Delegate source, Type type)
        {
            if (source == null)
            {
                return null;
            }

            var sources = source.GetInvocationList();
            if (sources.Length == 1)
            {
                return Delegate.CreateDelegate(type, sources[0].Target, sources[0].Method);
            }

            var destinations = new Delegate[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                destinations[i] = Delegate.CreateDelegate(type,
                    sources[i].Target, sources[i].Method);
            }

            return Delegate.Combine(destinations);
        }
    }
}
