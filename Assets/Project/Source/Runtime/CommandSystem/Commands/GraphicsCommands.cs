using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manatea.CommandSystem
{
    public static class GraphicsCommands
    {
        [Command]
        public static void ToggleFullscreen()
        {
#if UNITY_EDITOR
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            EditorWindow gameview = EditorWindow.GetWindow(type);
            gameview.maximized = !gameview.maximized;
#else
            Screen.fullScreen = !Screen.fullScreen;
#endif
        }

        [Command]
        public static void SetFullscreenMode(FullScreenMode mode)
        {
            Screen.fullScreenMode = mode;
        }

        [Command]
        public static void SetResolution(int width, int height)
        {
            Screen.SetResolution(width, height, Screen.fullScreen);
        }

        [Command]
        public static void SetVSyncCount(int vSyncCount)
        {
            vSyncCount = Mathf.Clamp(vSyncCount, 0, 2);
            QualitySettings.vSyncCount = vSyncCount;
        }

        [Command]
        public static void SetTargetFrameRate(int framerate)
        {
            framerate = Mathf.Clamp(framerate, 1, 10000);
            Application.targetFrameRate = framerate;
        }

        [Command]
        public static void SetHDREnabled(bool enableHDR)
        {
            HDROutputSettings.main.RequestHDRModeChange(enableHDR);
        }

        [Command]
        public static void SetHDRPaperWhiteNits(int nits)
        {
            HDROutputSettings.main.paperWhiteNits = nits;
        }

        [Command]
        public static void SetQualitySettings(int index)
        {
            QualitySettings.SetQualityLevel(index, true);
        }
    }
}
