#if ENABLE_INPUT_SYSTEM

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatea.CommandSystem
{
    public static class InputSystemUtility
    {
        private static bool consoleOpened;
        private static List<InputDevice> enabledDevices;

        [RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
#endif
        public static void SetInputSystem()
        {

            ConsoleGUI.onConsoleOpened += () =>
            {
                enabledDevices = new List<InputDevice>(InputSystem.devices);
                foreach (var device in enabledDevices)
                    InputSystem.DisableDevice(device);
                consoleOpened = true;
            };

            ConsoleGUI.onConsoleClosed += () =>
            {
                consoleOpened = false;
                InputSystem.Update();
                foreach (var device in enabledDevices)
                    InputSystem.EnableDevice(device);
            };

            InputSystem.onDeviceChange += (device, deviceChange) =>
            {
                if (!consoleOpened)
                    return;

                if (deviceChange == InputDeviceChange.Enabled)
                {
                    InputSystem.DisableDevice(device);
                    if (!enabledDevices.Contains(device))
                        enabledDevices.Add(device);
                }
                if (deviceChange == InputDeviceChange.Disabled)
                {
                    InputSystem.DisableDevice(device);
                    if (enabledDevices.Contains(device))
                        enabledDevices.Remove(device);
                }
            };
        }
    }
}

#endif
