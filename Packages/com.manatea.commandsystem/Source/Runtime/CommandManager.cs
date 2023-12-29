using System;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Manatea.CommandSystem.Converters;
using UnityEngine;

namespace Manatea.CommandSystem
{
    /// <summary>
    /// The CommandManager is responsible for executing and registering functions based on string identifies.
    /// </summary>
    public static class CommandManager
    {
        private static Dictionary<string, MethodInfo> CommandList = new Dictionary<string, MethodInfo>();
        private static List<MethodInfo> ErrorCommandList = new List<MethodInfo>();
        // TODO this throws NullRefExceptions when reloading domain
        public static event Action OnCommandListUpdated;

        private static BackgroundWorker commandLoaderWorker;
        private static Dictionary<string, MethodInfo> PendingCommands = new Dictionary<string, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Application.isPlaying)
            {
                commandLoaderWorker = new BackgroundWorker();
                commandLoaderWorker.DoWork += FetchCommands;
                commandLoaderWorker.RunWorkerCompleted += (sender, e) => OnCommandListUpdated?.Invoke();
                commandLoaderWorker.RunWorkerAsync();

                RegisterCustomConverters();
            }
        }

        private static void FetchCommands(object sender, DoWorkEventArgs e)
        {
            if (ConsoleGUISettings.LogLevel >= LogLevel.Verbose)
                Debug.Log("Start fetching commands...");

            DateTime time = DateTime.Now;

            const BindingFlags methodBindings = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            Type commandType = typeof(CommandAttribute);

            CommandList.Clear();
            ErrorCommandList.Clear();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (MethodInfo methodInfo in type.GetMethods(methodBindings))
                    {
                        foreach (CustomAttributeData attribute in methodInfo.CustomAttributes)
                        {
                            if (attribute.AttributeType == commandType && !Array.Exists(methodInfo.GetParameters(), p => !IsParameterValid(p)))
                            {
                                string commandKey = methodInfo.Name;
                                string alias = methodInfo.GetCustomAttribute<CommandAttribute>().Alias;
                                if (IsValidCommandAlias(alias))
                                    commandKey = alias;

                                if (!CommandList.ContainsKey(commandKey) && !CommandList.ContainsValue(methodInfo))
                                    CommandList.Add(commandKey, methodInfo);
                                else
                                    ErrorCommandList.Add(methodInfo);
                                break;
                            }
                        }
                    }
                }
            }

            // Add PendingCommands, added through the RegisterCommand functions
            Dictionary<string, MethodInfo> pending = new Dictionary<string, MethodInfo>(PendingCommands);
            PendingCommands.Clear();
            foreach (var kvp in pending)
            {
                if (!CommandList.ContainsKey(kvp.Key) && !CommandList.ContainsValue(kvp.Value))
                    CommandList.Add(kvp.Key, kvp.Value);
                else
                    ErrorCommandList.Add(kvp.Value);
            }
            // TODO we can swallow up commands here, if a new one is registered while the LoaderThread is
            // still running while we have already processed the pending commands.

            int milliseconds = (int)(DateTime.Now - time).TotalMilliseconds;
            if (ConsoleGUISettings.LogLevel >= LogLevel.Info)
                Debug.Log($"Fetched {CommandList.Count} " + (CommandList.Count > 1 ? "commands" : "command") + $" in {milliseconds}ms.");

            if (ErrorCommandList.Count > 0)
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"{ErrorCommandList.Count} " + (ErrorCommandList.Count > 1 ? "commands" : "command") + $" could not be loaded.");

                if (ConsoleGUISettings.LogLevel >= LogLevel.Debug)
                {
                    Debug.Log("Affected command functions:");
                    foreach (var method in ErrorCommandList)
                        Debug.Log($"Function: { method.Name }");
                }
            }
        }

        private static void RegisterCustomConverters()
        {
            TypeDescriptor.AddAttributes(typeof(Vector2), new TypeConverterAttribute(typeof(Vector2Converter)));
            TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(Vector3Converter)));
            TypeDescriptor.AddAttributes(typeof(Vector4), new TypeConverterAttribute(typeof(Vector4Converter)));
        }

        /// <summary>
        /// Get a list of all commands linked to their alias names.
        /// </summary>
        /// <returns> Returns a copy of all registered commands. </returns>
        public static Dictionary<string, MethodInfo> GetCommandList()
        {
            if (CommandList == null)
                return new Dictionary<string, MethodInfo>();
            else
                return new Dictionary<string, MethodInfo>(CommandList);
        }

        /// <summary>
        /// Registers a Delegate as a command.
        /// </summary>
        /// <param name="commandFunction"> The Delegate to run when executing this command. </param>
        /// <returns> True if the command could be registered, false otherwise. </returns>
        public static bool RegisterCommand(Delegate commandFunction)
        {
            return RegisterCommand(commandFunction, commandFunction.Method.Name);
        }
        /// <summary>
        /// Registers a Delegate as a command.
        /// </summary>
        /// <param name="commandFunction"> The Delegate to run when executing this command. </param>
        /// <param name="commandName"> The alias name to use for this command. </param>
        /// <returns> True if the command could be registered, false otherwise. </returns>
        public static bool RegisterCommand(Delegate commandFunction, string commandName)
        {
            if (commandFunction == null)
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"Command function is empty.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(commandName))
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"CommandName for function \"{ commandFunction.Method.Name }\" is empty.");
                return false;
            }
            if ((commandFunction.Method.Attributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"Command function \"{ commandFunction.Method.Name }\" does not match the required signature. Function must be static.");
                return false;
            }

            if (!IsValidCommandAlias(commandName))
                commandName = commandFunction.Method.Name;

            // Add command to the list
            if (commandLoaderWorker == null || commandLoaderWorker.IsBusy)
            {
                if (PendingCommands.ContainsKey(commandName))
                {
                    if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                        Debug.LogWarning($"Could not register pending command. \"{ commandName }\" alias already exists.");
                    return false;
                }
                if (PendingCommands.ContainsValue(commandFunction.Method))
                {
                    if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                        Debug.LogWarning($"Could not register pending command. \"{ commandFunction.Method.Name }\" function already exists.");
                    return false;
                }
                PendingCommands.Add(commandName, commandFunction.Method);
            }
            else
            {
                if (CommandList.ContainsKey(commandName))
                {
                    if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                        Debug.LogWarning($"Could not register command. \"{ commandName }\" alias already exists.");
                    return false;
                }
                if (CommandList.ContainsValue(commandFunction.Method))
                {
                    if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                        Debug.LogWarning($"Could not register command. \"{ commandFunction.Method.Name }\" function already exists.");
                    return false;
                }
                CommandList.Add(commandName, commandFunction.Method);
                OnCommandListUpdated.Invoke();
            }

            if (ConsoleGUISettings.LogLevel >= LogLevel.Info)
                Debug.Log($"Command registered as \"{ commandName }\".");

            return true;
        }
        // TODO some way to unregister commands at runtime

        private static bool IsValidCommandAlias(string commandAlias) => !string.IsNullOrEmpty(commandAlias) && !commandAlias.Contains(' ');

        /// <summary>
        /// Executes a registered command.
        /// </summary>
        /// <param name="command"> The command to be executed. </param>
        /// <returns> True if the command was run, false otherwise. </returns>
        public static bool ExecuteCommand(string command)
        {
            string[] commandData = ProcessCommand(command);
            if (commandData.Length == 0)
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"Command empty.");
                return false;
            }

            string key = CommandList.Keys.FirstOrDefault(s => s.ToLower() == commandData[0].ToLower() && CommandList[s].GetParameters().Length == commandData.Length - 1);
            if (string.IsNullOrEmpty(key) || !CommandList.ContainsKey(key))
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"No command found matching the passed in signature. (Name: { commandData[0] } | Parameter count: { commandData.Length - 1 })");
                return false;
            }

            MethodInfo method = CommandList[key];
            return ExecuteCommand(method, commandData.Where((e, i) => i != 0).ToArray());
        }
        internal static bool ExecuteCommand(MethodInfo methodInfo, string commandParameter)
        {
            return ExecuteCommand(methodInfo, ProcessCommand(commandParameter));
        }
        internal static bool ExecuteCommand(MethodInfo methodInfo, string[] parameterStrings)
        {
            if (methodInfo == null)
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning("Command was null.");
                return false;
            }
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            if (parameterStrings.Length != paramInfos.Length)
            {
                if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                    Debug.LogWarning($"Command parameter count ({ parameterStrings.Length }) does not match function parameter cout ({ paramInfos.Length }).");
                return false;
            }

            object[] parameters = new object[parameterStrings.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                bool parameterConverted = false;

                // Default converter
                TypeConverter converter = TypeDescriptor.GetConverter(paramInfos[i].ParameterType);
                if (parameterConverted == false && converter.IsValid(parameterStrings[i]))
                {
                    parameters[i] = converter.ConvertFrom(null, CultureInfo.InvariantCulture, parameterStrings[i]);
                    parameterConverted = true;
                }

                // Num -> Enum converter
                if (parameterConverted == false && paramInfos[i].ParameterType.IsEnum)
                {
                    converter = TypeDescriptor.GetConverter(typeof(int));
                    if (converter.IsValid(parameterStrings[i]))
                    {
                        int paramRaw = (int)converter.ConvertFrom(null, CultureInfo.InvariantCulture, parameterStrings[i]);
                        Array enumValues = paramInfos[i].ParameterType.GetEnumValues();
                        if (paramRaw >= 0 && enumValues.Length > paramRaw)
                        {
                            parameters[i] = enumValues.GetValue(paramRaw);
                            parameterConverted = true;
                        }
                    }
                }

                // Param could not be converted
                if (!parameterConverted)
                {
                    if (ConsoleGUISettings.LogLevel >= LogLevel.Warn)
                        Debug.LogWarning($"Parameter {i} with value \"{ parameterStrings[i] }\" cannot be converted to type { paramInfos[i].ParameterType.Name }.");
                    return false;
                }
            }

            // Invoke command
            object returnValue = null;
            try
            {
                returnValue = methodInfo.Invoke(null, parameters);
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException);
            }

            // Log executed command
            if (ConsoleGUISettings.LogLevel >= LogLevel.Info)
            {
                string command = "";
                if (parameterStrings.Length > 0)
                {
                    foreach (string s in parameterStrings)
                        command += (s.Contains(' ') ? "\"" + s + "\"" : s) + " ";
                    command = command.Remove(command.Length - 1);
                }

                string log = $"Successfully executed command: " + command;
                if (methodInfo.ReturnType != typeof(void))
                    log += " | Returned: " + returnValue;

                Debug.Log(log);
            }

            return true;
        }

        internal static string[] ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return new string[0];

            string leftover = command;

            List<string> commandList = new List<string>();
            bool stringContext = false;
            int vectorContext = 0;
            string currentString = "";
            char currChar = ' ';
            char prevChar = ' ';
            while (leftover.Length > 0)
            {
                prevChar = currChar;
                currChar = leftover[0];
                leftover = leftover.Remove(0, 1);

                // String context
                if (currChar == '"')
                {
                    stringContext = !stringContext;
                    continue;
                }

                // Vector context
                if (currChar == '(')
                {
                    vectorContext++;
                }
                if (currChar == ')')
                {
                    vectorContext--;
                }

                // End current segment
                if (currChar == ' ' && stringContext == false && vectorContext == 0)
                {
                    commandList.Add(currentString);
                    currentString = "";
                    continue;
                }

                currentString += currChar;
            }
            commandList.Add(currentString);

            return commandList.ToArray();
        }

        private static bool IsParameterValid(ParameterInfo param)
        {
            return param.ParameterType.IsPrimitive ||
                param.ParameterType == typeof(String) ||
                param.ParameterType == typeof(Vector2) ||
                param.ParameterType == typeof(Vector3) ||
                param.ParameterType == typeof(Vector4) ||
                param.ParameterType.IsEnum;
        }
    }
}