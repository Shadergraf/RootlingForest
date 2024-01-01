using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Manatea.CommandSystem
{
    /// <summary>
    /// The Unity component providing the console GUI functionality.
    /// </summary>
    [DisallowMultipleComponent]
    public class ConsoleGUI : MonoBehaviour
    {
        internal static ConsoleGUI Singleton
        { get; set; }
        private static Dictionary<string, MethodInfo> CachedCommandList;

        /// <summary>
        /// Called when the console window was opened.
        /// </summary>
        public static event Action onConsoleOpened;
        /// <summary>
        /// Called when the console window was closed.
        /// </summary>
        public static event Action onConsoleClosed;
        /// <summary>
        /// Called when a command was successfully(!) executed.
        /// </summary>
        public static event Action<string> onCommandExecuted;


        private static readonly string[] fontQueries = new string[] { "Consolas", "Lucida Console", "Arial" };

        private bool dirty;

        private int fontSize => Mathf.Max(ConsoleGUISettings.FontSize, 1);
        private int LineHeight => fontSize + 3;
        private int CharacterWidth => (int)(fontSize * 0.75);
        private bool DisplayCurrentCommand => ConsoleGUISettings.AutocompleteCommands && CommandMethod != null && CommandMethod.GetParameters().Length > 0;

        private bool consoleVisible = false;
        private GUISkin consoleSkin;

        private string CommandInput = "";
        private string[] ProcessedCommand;
        private MethodInfo CommandMethod;
        private TextEditor CommandInputTextEditor;
        private List<string> CommandCache = new List<string>();

        private int AutocompleteSelectedId;
        private object AutocompleteSelectedObject;
        private List<object> AutocompleteObjects = new List<object>();

        private string AutocompleteLastCommandInput;
        private Vector2 AutocompletePosition = Vector2.zero;

        private Font font;
        private Texture2D texBackground;
        private Texture2D texSelection;

        private Color colorError = Color.red;
        private float commandErrorAnimation = 0;
        private Color colorDescription = new Color(0.5f, 0.5f, 0.5f);
        private Dictionary<Type, Color> colorTypes = new Dictionary<Type, Color>();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (!Debug.isDebugBuild && ConsoleGUISettings.DisableInBuild)
            {
                return;
            }

            // TODO move to EnsureSingleton?
            Singleton = new GameObject("ConsoleManager").AddComponent<ConsoleGUI>();
            DontDestroyOnLoad(Singleton.gameObject);

            CommandManager.OnCommandListUpdated += () => { 
                CachedCommandList = CommandManager.GetCommandList(); 
            };
        }


        private void Awake()
        {
            Redraw();
        }

        private void EnsureSingleton()
        {
            if (!Singleton)
            {
                Singleton = this;
                DontDestroyOnLoad(Singleton.gameObject);

                CachedCommandList = CommandManager.GetCommandList();
                Singleton.Redraw();

                CommandManager.OnCommandListUpdated += () => {
                    CachedCommandList = CommandManager.GetCommandList();
                    if (Singleton)
                        Singleton.Redraw();
                };
            }
        }

        internal void Redraw()
        {
            dirty = true;
        }

        /// <summary>
        /// Ensure the consoleSkin exists and apply it to GUI.skin
        /// </summary>
        private void EnsureSkinGUI()
        {
            if (consoleSkin && !dirty)
            {
                GUI.skin = consoleSkin;
                return;
            }

            RebuildSkinGUI();
            GUI.skin = consoleSkin;
        }

        /// <summary>
        /// Build the consoleSkin
        /// </summary>
        private void RebuildSkinGUI()
        {
            if (consoleSkin)
                Destroy(consoleSkin);
            consoleSkin = Instantiate(GUI.skin);

            // Create Font
            int fontSize = (int)(this.fontSize * 1.0f);
            if (ConsoleGUISettings.Font != null)
            {
                font = ConsoleGUISettings.Font;
            }
            else
            {
                font = Font.CreateDynamicFontFromOSFont(fontQueries, fontSize);
            }
            consoleSkin.font = font;


            consoleSkin.settings.selectionColor = new Color(0, 120 / 255f, 215 / 255f);

            if (!texBackground)
                texBackground = new Texture2D(1, 1);
            texBackground.SetPixel(0, 0, ConsoleGUISettings.ColorBg);
            texBackground.Apply();
            if (!texSelection)
                texSelection = new Texture2D(1, 1);
            texSelection.SetPixel(0, 0, ConsoleGUISettings.ColorSelection);
            texSelection.Apply();


            consoleSkin.label.fontSize = fontSize;
            consoleSkin.label.alignment = TextAnchor.UpperLeft;
            consoleSkin.label.clipping = TextClipping.Overflow;
            consoleSkin.label.wordWrap = false;
            consoleSkin.label.normal.background = texBackground;
            consoleSkin.label.hover.background = texBackground;
            consoleSkin.label.focused.background = texBackground;
            consoleSkin.label.border = new RectOffset();
            consoleSkin.label.margin = new RectOffset();
            consoleSkin.label.padding = new RectOffset();
            consoleSkin.label.overflow = new RectOffset();
            
            consoleSkin.textField.fontSize = fontSize;
            consoleSkin.textField.alignment = TextAnchor.UpperLeft;
            consoleSkin.textField.clipping = TextClipping.Overflow;
            consoleSkin.textField.wordWrap = false;
            consoleSkin.textField.normal.background = texBackground;
            consoleSkin.textField.hover.background = texBackground;
            consoleSkin.textField.focused.background = texBackground;
            consoleSkin.textField.border = new RectOffset();
            consoleSkin.textField.margin = new RectOffset();
            consoleSkin.textField.padding = new RectOffset();
            consoleSkin.textField.overflow = new RectOffset();
            
            consoleSkin.button.fontSize = fontSize;
            consoleSkin.button.alignment = TextAnchor.UpperLeft;
            consoleSkin.button.clipping = TextClipping.Overflow;
            consoleSkin.button.wordWrap = false;
            consoleSkin.button.normal.background = texBackground;
            consoleSkin.button.hover.background = texSelection;
            consoleSkin.button.focused.background = texSelection;
            consoleSkin.button.active.background = texSelection;
            consoleSkin.button.border = new RectOffset();
            consoleSkin.button.margin = new RectOffset();
            consoleSkin.button.padding = new RectOffset();
            consoleSkin.button.overflow = new RectOffset();


            colorTypes.Clear();
            colorTypes.Add(typeof(Boolean), ConsoleGUISettings.ParamColorBool);
            colorTypes.Add(typeof(Int16), ConsoleGUISettings.ParamColorInt);
            colorTypes.Add(typeof(Int32), ConsoleGUISettings.ParamColorInt);
            colorTypes.Add(typeof(Int64), ConsoleGUISettings.ParamColorInt);
            colorTypes.Add(typeof(Single), ConsoleGUISettings.ParamColorFloat);
            colorTypes.Add(typeof(Double), ConsoleGUISettings.ParamColorFloat);
            colorTypes.Add(typeof(Enum), ConsoleGUISettings.ParamColorEnum);
            colorTypes.Add(typeof(String), ConsoleGUISettings.ParamColorString);
            colorTypes.Add(typeof(Char), ConsoleGUISettings.ParamColorString);
        }

        private void OnGUI()
        {
            EnsureSingleton();
            UpdateHierarchyVisibility();

            if (dirty)
                EnsureSkinGUI();

            ProcessEvents();
            if (Event.current.type == EventType.Used)
                return;

            if (!consoleVisible)
            {
                dirty = false;
                return;
            }

            EnsureSkinGUI();
            GUI.depth = -1900;
            GUI.contentColor = ConsoleGUISettings.ColorText;



            GUI.Label(new Rect(0, Screen.height - LineHeight, CharacterWidth, LineHeight), ">");

            if (Event.current.type == EventType.Repaint)
                commandErrorAnimation = Mathf.Max(commandErrorAnimation - Time.unscaledDeltaTime, 0);
            GUI.contentColor = Color.Lerp(ConsoleGUISettings.ColorText, colorError, Mathf.SmoothStep(0, 1, commandErrorAnimation));

            const string CommandTextField = "CommandTextField";
            GUI.SetNextControlName(CommandTextField);
            string newCommandInput = GUI.TextField(new Rect(CharacterWidth, Screen.height - LineHeight, Screen.width - CharacterWidth, LineHeight), CommandInput);
            CommandInputTextEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (CommandInput != newCommandInput || dirty)
            {
                SetCommand(newCommandInput);
            }
            GUI.FocusControl(CommandTextField);

            GUI.contentColor = ConsoleGUISettings.ColorText;



            Rect FullAutoWindow = new Rect(CharacterWidth, 0, Screen.width - CharacterWidth, Screen.height - LineHeight);
            float AutocompleteHeight = AutocompleteObjects.Count * LineHeight;
            Rect AutocompleteWindow = new Rect(CharacterWidth, 0, Screen.width - CharacterWidth, Mathf.Min(AutocompleteHeight, Screen.height - LineHeight));
            AutocompleteWindow.y = Screen.height - LineHeight - AutocompleteWindow.height;
            GUILayout.BeginArea(FullAutoWindow);

            GUILayout.FlexibleSpace();

            // Autocompletion
            if (AutocompleteObjects.Count != 0)
            {
                // Align autocompletion horizontally
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0, 0, 0, 0);
                string commandSpacer = "";
                if (ConsoleGUISettings.ShowParameters && DisplayCurrentCommand)
                {
                    string aC = GetAutocompleteFormatting(CommandMethod, true);
                    string[] autocompletedProcessedCommand = CommandManager.ProcessCommand(aC);
                    for (int i = 0; i < ProcessedCommand.Length - 1 && i < autocompletedProcessedCommand.Length; i++)
                    {
                        commandSpacer += autocompletedProcessedCommand[i] + " ";
                    }
                }
                else
                {
                    for (int i = 0; i < ProcessedCommand.Length - 1; i++)
                    {
                        commandSpacer += ProcessedCommand[i] + " ";
                    }
                }
                GUILayout.Label(commandSpacer, GUILayout.Height(LineHeight), GUILayout.ExpandWidth(false));
                GUI.color = Color.white;
                GUILayout.BeginVertical();

                // Display full autocompletion list
                AutocompletePosition = GUILayout.BeginScrollView(AutocompletePosition, false, false, GUIStyle.none, GUIStyle.none);
                for (int i = 0; i < AutocompleteObjects.Count; i++)
                {
                    int id = AutocompleteObjects.Count - 1 - i;

                    // Apply keyboard cursor background selection
                    consoleSkin.button.normal.background = (id == AutocompleteSelectedId) ? texSelection : texBackground;

                    AutocompleteElement(AutocompleteObjects[id]);
                }
                GUILayout.EndScrollView();

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            // Display current command
            if (DisplayCurrentCommand)
            {
                GUILayout.Label(GetAutocompleteFormatting(CommandMethod), GUILayout.Height(LineHeight));
            }

            GUILayout.EndArea();

            dirty = false;
        }

        private void AutocompleteElement(object element)
        {
            GUI.SetNextControlName("Button");
            if (element is MethodInfo)
            {
                MethodInfo methodInfo = (MethodInfo)element;
                if (GUILayout.Button(GetAutocompleteFormatting(methodInfo), GUILayout.Height(LineHeight)))
                {
                    SelectAutocompleteObject(methodInfo);
                }
            }
            else
            {
                if (GUILayout.Button(element.ToString(), GUILayout.Height(LineHeight)))
                {
                    SelectAutocompleteObject(element);
                }
            }
        }

        private void SelectAutocompleteObject(object element)
        {
            GUI.SetNextControlName("Button");
            // Add element as is when current command is empty
            if (string.IsNullOrEmpty(CommandInput))
            {
                SetCommand(element.ToString(), true);
                ClearAutocompleteSelection();
            }
            // Add element as MethodInfo command
            else if (element is MethodInfo)
            {
                ApplyCommand((MethodInfo)element);
                ClearAutocompleteSelection();
            }
            // Add element as a processed parameter
            else
            {
                string newCommand = "";
                for (int i = 0; i < ProcessedCommand.Length - 1; i++)
                {
                    if (ProcessedCommand[i].Contains(' '))
                        newCommand += "\"" + ProcessedCommand[i] + "\" ";
                    else
                        newCommand += ProcessedCommand[i] + " ";
                }
                string autocompleteString = element.ToString();
                if (autocompleteString.Contains(' '))
                    newCommand += "\"" + autocompleteString + "\"";
                else
                    newCommand += autocompleteString;
                if (CommandMethod != null && CommandMethod.GetParameters().Length >= ProcessedCommand.Length)
                    newCommand += " ";
                SetCommand(newCommand, true);
                ClearAutocompleteSelection();
            }
        }

        private void UpdateAutocompletion()
        {
            if (AutocompleteLastCommandInput == CommandInput && !dirty)
                return;

            AutocompleteObjects = GetAutocompletionCommands();
            AutocompletePosition = new Vector2(0, 100000000);
            
            AutocompleteLastCommandInput = CommandInput;

            int newAutocommandSelectedId = AutocompleteObjects.IndexOf(AutocompleteSelectedObject);
            // Keep autocomplete cursor on the last selected object even if autocompletion list changes
            if (newAutocommandSelectedId != -1)
            {
                AutocompleteSelectedId = newAutocommandSelectedId;
            }
            // Clear autocomplete cursor
            if (AutocompleteObjects.Count == 0)
            {
                ClearAutocompleteSelection();
            }
        }

        private List<object> GetAutocompletionCommands()
        {
            // Clear autocompletion due to empty inputString
            List<object> output = new List<object>();
            if (ProcessedCommand.Length == 0)
            {
                CommandMethod = null;
                return output;
            }

            // Clear autocompletion due to empty command
            string commandSegment = ProcessedCommand[0];
            if (string.IsNullOrWhiteSpace(commandSegment))
            {
                CommandMethod = null;
                return output;
            }

            // Autocomplete commands
            if (ProcessedCommand.Length >= 1)
            {
                List<string> atcCommandNameList = new List<string>(CachedCommandList.Keys);

                // Search for commands
                if (ProcessedCommand.Length == 1)
                {
                    if (ProcessedCommand[0].Length == 1)
                        atcCommandNameList = atcCommandNameList.FindAll(c => c.ToLower().StartsWith(commandSegment.ToLower()));
                    else
                        atcCommandNameList = atcCommandNameList.FindAll(c => c.ToLower().Contains(commandSegment.ToLower()));
                }
                else
                    atcCommandNameList = atcCommandNameList.FindAll(c => c.ToLower() == commandSegment.ToLower());
                atcCommandNameList.Sort((a1, a2) => LevenshteinDist(a1.ToLower(), ProcessedCommand[0]).CompareTo(LevenshteinDist(a2.ToLower(), ProcessedCommand[0])));

                // Set CommandMethod if exact match exists
                //MethodInfo match = atcCommandNameList.Find(c => c.ToLower() == commandSegment.ToLower());
                string matchKey = atcCommandNameList.Find(c => c.ToLower() == commandSegment.ToLower());
                if (!string.IsNullOrEmpty(matchKey))
                {
                    CommandMethod = CachedCommandList[matchKey];
                    atcCommandNameList.Remove(matchKey);
                }
                else
                {
                    CommandMethod = null;
                }

                if (ConsoleGUISettings.AutocompleteCommands)
                {
                    // Add commands to autocompletion list
                    foreach (string key in atcCommandNameList)
                        output.Add(CachedCommandList[key]);
                }
            }

            // Autocomplete parameters
            if (ConsoleGUISettings.AutocompleteParameters && CommandMethod != null)
            {
                string paramSegment = ProcessedCommand[ProcessedCommand.Length - 1];

                int parameterId = ProcessedCommand.Length - 2;
                ParameterInfo[] parameters = CommandMethod.GetParameters();
                if (parameterId >= 0 && parameterId <= parameters.Length - 1)
                {
                    ParameterInfo parameter = parameters[parameterId];

                    bool customAutocompleteFound = false;
                    CommandAttribute cmd = CommandMethod.GetCustomAttribute<CommandAttribute>();
                    if (cmd != null && cmd.ParameterAutocompleteDelegates != null && cmd.ParameterAutocompleteDelegates.Length - 1 >= parameterId &&
                        cmd.ParameterAutocompleteDelegates[parameterId] != null)
                    {
                        string autocompleteMethodName = cmd.ParameterAutocompleteDelegates[parameterId];
                        foreach (var method in CommandMethod.DeclaringType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                        {
                            if (method.Name == autocompleteMethodName && method.ReturnType == typeof(string[]))
                            {
                                string[] autoS = (string[])method.Invoke(null, null);
                                List<object> autocompletion = new List<object>();
                                if (autoS != null)
                                    autocompletion = new List<object>(autoS);
                                output.AddRange(autocompletion.FindAll(o => o != null && o.ToString().ToLower().StartsWith(paramSegment.ToLower())));
                                customAutocompleteFound = true;
                                break;
                            }
                        }
                    }
                    if (!customAutocompleteFound)
                    {
                        if (parameter.ParameterType == typeof(Boolean))
                        {
                            List<object> autocompletion = new List<object>();
                            autocompletion.Add(true);
                            autocompletion.Add(false);

                            autocompletion.RemoveAll(o => o.ToString().ToLower() == paramSegment.ToLower());
                            output.AddRange(autocompletion.FindAll(o => o.ToString().ToLower().StartsWith(paramSegment.ToLower())));
                        }
                        else if (parameter.ParameterType.IsEnum)
                        {
                            List<object> autocompletion = new List<object>();
                            foreach (var e in parameter.ParameterType.GetEnumValues())
                                autocompletion.Add(e);

                            autocompletion.RemoveAll(o => o.ToString().ToLower() == paramSegment.ToLower());
                            output.AddRange(autocompletion.FindAll(o => o.ToString().ToLower().StartsWith(paramSegment.ToLower())));
                        }
                    }
                }
            }

            return output;
        }

        private bool ClearAutocompleteSelection()
        {
            bool cleared = AutocompleteSelectedId != -1 || AutocompleteSelectedObject != null;
            AutocompleteSelectedId = -1;
            AutocompleteSelectedObject = null;
            return cleared;
        }

        internal string GetAutocompleteFormatting(MethodInfo methodInfo, bool forceRawCommand = false)
        {
            string output = CachedCommandList.FirstOrDefault(m => m.Value == methodInfo).Key;

            int segment = GetCommandCursorSegment(CommandInput, CommandInputTextEditor.cursorIndex);

            if (!ConsoleGUISettings.ShowParameters)
                return output;

            ParameterInfo[] parameters = methodInfo.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];
                string paramString = $" [{ param.Name }]";

                if (!forceRawCommand && ConsoleGUISettings.TintParameterTypes)
                {
                    Color paramColor = Color.magenta;
                    if (colorTypes.ContainsKey(param.ParameterType))
                        paramColor = colorTypes[param.ParameterType];
                    if (param.ParameterType.IsEnum)
                        paramColor = colorTypes[typeof(Enum)];
                    paramString = $"<color=#{ ColorUtility.ToHtmlStringRGBA(paramColor) }>{ paramString }</color>";
                    //paramString = $" <color=#{ColorUtility.ToHtmlStringRGBA(paramColor)}>{param.Name}({param.ParameterType.Name})</color>";
                }

                if (!forceRawCommand && ConsoleGUISettings.HighlightActiveParameter && i == segment - 1)
                    paramString = $"<b>{ paramString }</b>";

                output += paramString;
            }

            CommandAttribute commandAttr = methodInfo.GetCustomAttribute<CommandAttribute>();
            if (!forceRawCommand && commandAttr != null && !string.IsNullOrWhiteSpace(commandAttr.Description))
                output += $" <i><color=#{ ColorUtility.ToHtmlStringRGBA(colorDescription) }>{ commandAttr.Description }</color></i>";

            return output;
        }

        // TODO only update if command and cursorPos changed
        private int GetCommandCursorSegment(string command, int cursorPos)
        {
            cursorPos = Mathf.Clamp(cursorPos, 0, command.Length);

            if (cursorPos < command.Length)
                command = command.Remove(cursorPos);

            int segmentCount = CommandManager.ProcessCommand(command).Length - 1;
            return segmentCount;
        }

        private void ProcessEvents()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                // Early out when console is invisible
                if (!consoleVisible)
                {
                    // Show Console
                    if (Event.current.keyCode == ConsoleGUISettings.ConsoleKey &&
                        (!ConsoleGUISettings.ModifierShift || Event.current.shift) &&
                        (!ConsoleGUISettings.ModifierControl || Event.current.control) &&
                        (!ConsoleGUISettings.ModifierAlt || Event.current.alt))
                    {
                        ToggleVisibility();
                        SetCommand("");
                        Event.current.Use();
                    }

                    return;
                }

                // Escape
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    if (ClearAutocompleteSelection())
                    {
                        Event.current.Use();
                        return;
                    }
                }

                // Up/Down
                int autocompleteCursorDelta = 0;
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    autocompleteCursorDelta++;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    autocompleteCursorDelta--;
                    Event.current.Use();
                }
                if (autocompleteCursorDelta != 0)
                {
                    if (AutocompleteObjects.Count == 0 && string.IsNullOrWhiteSpace(CommandInput))
                    {
                        AutocompleteObjects.AddRange(CommandCache.Reverse<string>());
                    }

                    if (AutocompleteObjects.Count != 0)
                    {
                        if (AutocompleteSelectedId == -1)
                            AutocompleteSelectedId = autocompleteCursorDelta > 0 ? -1 : AutocompleteObjects.Count;
                        AutocompleteSelectedId = (int)Mathf.Repeat(AutocompleteSelectedId + autocompleteCursorDelta, AutocompleteObjects.Count);
                        AutocompleteSelectedObject = AutocompleteObjects[AutocompleteSelectedId];

                        float limit = (AutocompleteObjects.Count - AutocompleteSelectedId - 1) * LineHeight;
                        if (limit < AutocompletePosition.y)
                            AutocompletePosition.y = limit;
                        limit = (AutocompleteObjects.Count - AutocompleteSelectedId - 1) * LineHeight - (Screen.height - LineHeight * (DisplayCurrentCommand ? 3 : 2));
                        if (limit > AutocompletePosition.y)
                            AutocompletePosition.y = limit;
                    }
                    return;
                }

                // Autocomplete first autocompleted command
                if (Event.current.keyCode == KeyCode.Tab)
                {
                    if (AutocompleteObjects.Count > 0)
                    {
                        if (AutocompleteSelectedObject != null)
                            SelectAutocompleteObject(AutocompleteSelectedObject);
                        else
                            SelectAutocompleteObject(AutocompleteObjects[0]);

                        Event.current.Use();
                        return;
                    }
                }

                // Execute Command
                if (Event.current.keyCode == KeyCode.Return)
                {
                    if (AutocompleteSelectedObject == null)
                    {
                        ExecuteCommand();
                    }
                    else
                    {
                        SelectAutocompleteObject(AutocompleteSelectedObject);
                    }
                    Event.current.Use();
                    return;
                }


                // Exit Console
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    SetVisibility(false);
                    SetCommand("");
                    Event.current.Use();
                    return;
                }

                // Show Console
                if (Event.current.keyCode == ConsoleGUISettings.ConsoleKey)
                {
                    ToggleVisibility();
                    SetCommand("");
                    Event.current.Use();
                    return;
                }
            }
        }

        private void SetCommand(string newCommandInput, bool jumpToEnd = false)
        {
            CommandInput = newCommandInput;
            ProcessedCommand = CommandManager.ProcessCommand(CommandInput);

            if (jumpToEnd)
            {
                CommandInputTextEditor.text = CommandInput;
                CommandInputTextEditor.cursorIndex = CommandInput.Length;
                CommandInputTextEditor.selectIndex = CommandInput.Length;
            }

            UpdateAutocompletion();
        }

        private void ExecuteCommand()
        {
            bool executionResult = false;
            if (CommandMethod != null)
            {
                string commandName = CachedCommandList.FirstOrDefault(m => m.Value == CommandMethod).Key;
                string parameters = CommandInput.Remove(0, (int)Mathf.Min(CommandInput.Length, commandName.Length + 1));
                executionResult = CommandManager.ExecuteCommand(CommandMethod, parameters);
            }
            else
            {
                executionResult = CommandManager.ExecuteCommand(CommandInput);
            }

            if (executionResult)
            {
                CommandCache.Add(CommandInput);
                onCommandExecuted?.Invoke(CommandInput);
                SetCommand("");

                if (!Event.current.shift)
                    Hide();
            }
            else
            {
                commandErrorAnimation = 1;
            }
        }

        private void ApplyCommand(MethodInfo methodInfo)
        {
            CommandMethod = methodInfo;
            string commandName = CachedCommandList.FirstOrDefault(m => m.Value == methodInfo).Key;
            SetCommand(commandName + (methodInfo.GetParameters().Length > 0 ? " " : ""), true);
        }


        /// <summary>
        /// Toggle the console window visibility.
        /// </summary>
        public static void ToggleVisibility()
        {
            SetVisibility(!Singleton.consoleVisible);
        }
        /// <summary>
        /// Set the console window visibility.
        /// </summary>
        public static void SetVisibility(bool newVisible)
        {
            if (newVisible)
                Show();
            else
                Hide();
        }
        /// <summary>
        /// Show the console window.
        /// </summary>
        public static void Show()
        {
            if (!Singleton || Singleton.consoleVisible)
                return;

            Singleton.consoleVisible = true;
            if (ConsoleGUISettings.LogLevel >= LogLevel.Debug)
                Debug.Log("Opened console.");
            onConsoleOpened?.Invoke();
        }
        /// <summary>
        /// Hide the console window.
        /// </summary>
        public static void Hide()
        {
            if (!Singleton || !Singleton.consoleVisible)
                return;

            Singleton.consoleVisible = false;
            if (ConsoleGUISettings.LogLevel >= LogLevel.Debug)
                Debug.Log("Closed console.");
            onConsoleClosed?.Invoke();
        }


        private void UpdateHierarchyVisibility()
        {
            if (ConsoleGUISettings.HideConsoleGameObject)
                gameObject.hideFlags = HideFlags.HideInHierarchy;
            else
                gameObject.hideFlags = HideFlags.None;
        }


        private static int LevenshteinDist(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Verify arguments.
            if (n == 0)
                return m;
            if (m == 0)
                return n;

            // Initialize arrays.
            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            // Begin looping.
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    // Compute cost.
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }

            // Return cost.
            return d[n, m];
        }
    }
}
