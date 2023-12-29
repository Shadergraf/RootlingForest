using System.IO;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manatea.CommandSystem
{
    public static class UtilityCommands
    {
#if UNITY_EDITOR
        public static string SavedDir => Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/Saved";
        public static string ScreenshotsDir => SavedDir + "/Screenshots/";
#else
        public static string SavedDir => Application.persistentDataPath + "/Saved";
        public static string ScreenshotsDir => SavedDir + "/Screenshots/";
#endif

        [Command]
        public static void Exit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

#if PLATFORM_STANDALONE || UNITY_EDITOR

        [Command]
        public static void CaptureScreenshot()
        {
            CaptureHighResScreenshot(1);
        }
        [Command]
        public static void CaptureHighResScreenshot(int sizeMult)
        {
            if (!Directory.Exists(ScreenshotsDir))
                Directory.CreateDirectory(ScreenshotsDir);

            for (int i = 0; i < 10000; ++i)
            {
                string path = ScreenshotsDir + "Screenshot_" + i.ToString("D4") + ".png";
                if (!File.Exists(path))
                {
                    ScreenCapture.CaptureScreenshot(path, sizeMult);
                    Debug.Log("Screenshot captured at: " + path);
                    return;
                }
            }

            Debug.Log("Screenshot could not be captured.");
        }
        [Command]
        public static void OpenScreenshotFolder()
        {
            if (!Directory.Exists(ScreenshotsDir))
                Directory.CreateDirectory(ScreenshotsDir);

            System.Diagnostics.Process.Start(ScreenshotsDir);
        }

#endif

        [Command]
        private static void Log(string text)
        {
            Debug.Log(text);
        }

        [Command]
        private static void LogWarning(string text)
        {
            Debug.LogWarning(text);
        }

        [Command]
        private static void LogError(string text)
        {
            Debug.LogError(text);
        }


        [Command]
        public static void DumpCommands()
        {
            string directory = SavedDir + "/Temp/";

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            directory = Path.Combine(directory, "CommandDump.txt");
            using (StreamWriter outputFile = new StreamWriter(directory))
            {
                foreach (var kvp in CommandManager.GetCommandList())
                {
                    string line = kvp.Key;
                    foreach (var p in kvp.Value.GetParameters())
                        line += $" { p.Name }({ p.ParameterType.Name })";

                    CommandAttribute command = kvp.Value.GetCustomAttribute<CommandAttribute>();
                    if (command != null && !string.IsNullOrWhiteSpace(command.Description))
                        line += " | " + command.Description;

                    outputFile.WriteLine(line);
                }
            }

            Debug.Log("Commands dumped to: " + directory);
        }
    }
}
