using UnityEngine;

namespace Manatea.CommandSystem
{
    /// <summary>
    /// Project settings defining the visual style and functionalities of the console GUI.
    /// </summary>
    public class ConsoleGUISettings : ScriptableObject
    {
        [SerializeField, Tooltip("Hides the manager GameObject that the console window is attached to.")]
        private bool _hideGameObject = true;
        [SerializeField, Tooltip("Disables the console window in build by adding the \"CONSOLE_DISABLE\" define symbol.")]
        private bool _disableInBuild = false;
        [SerializeField, Tooltip("The font size to use for console text.")]
        private int _defaultFontSize = 16;
        [SerializeField, Tooltip("The font to use for console text.")]
        private Font _font = null;
        [SerializeField, Tooltip("Enable command autocompletion.")]
        private bool _autocompleteCommands = true;
        [SerializeField, Tooltip("Enable parameter autocompletion.")]
        private bool _autocompleteParameters = true;
        [SerializeField, Tooltip("Enable parameter previews.")]
        private bool _showParameters = true;
        [SerializeField, Tooltip("Enable highlighting of the parameter under the cursor.")]
        private bool _highlightActiveParameter = true;
        [SerializeField, Tooltip("Enable coloring of command parameters based on their underlying type.")]
        private bool _tintParameterTypes = true;
        [SerializeField, Tooltip("The key that opens the console.")]
        private KeyCode _consoleKey = KeyCode.F3;
        [SerializeField, Tooltip("Must the Shift key also be pressed to open the console?")]
        private bool _modifierShift = false;
        [SerializeField, Tooltip("Must the Control key also be pressed to open the console?")]
        private bool _modifierControl = false;
        [SerializeField, Tooltip("Must the Alt key also be pressed to open the console?")]
        private bool _modifierAlt = false;
        [SerializeField, Tooltip("Defines the log level to use for any console log messages.")]
        private LogLevel _logLevel = LogLevel.Warn;

        [SerializeField, Tooltip("Defines the text color.")]
        private Color _colorText = new Color(1, 1, 1);
        [SerializeField, Tooltip("Defines the background color.")]
        private Color _colorBg = new Color(0, 0, 0, 0.75f);
        [SerializeField, Tooltip("Defines the background color of selected items.")]
        private Color _colorSelection = new Color(0.25f, 0.25f, 0.25f, 0.75f);
        [SerializeField, Tooltip("Defines the color of bool parameters.")]
        private Color _paramColorBool = new Color(0.92f, 0.35f, 0.23f);
        [SerializeField, Tooltip("Defines the color of integer parameters.")]
        private Color _paramColorInt = new Color(0.4f, 0.85f, 0.08f);
        [SerializeField, Tooltip("Defines the color of float parameters.")]
        private Color _paramColorFloat = new Color(0.08f, 0.9f, 0.75f);
        [SerializeField, Tooltip("Defines the color of enum parameters.")]
        private Color _paramColorEnum = new Color(0.16f, 0.35f, 0.97f);
        [SerializeField, Tooltip("Defines the color of string parameters.")]
        private Color _paramColorString = new Color(0.85f, 0.3f, 0.92f);


        /// <summary>
        /// Hides the GameObject that the console window is attached to.
        /// </summary>
        public static bool HideConsoleGameObject { get; set; }
        /// <summary>
        /// Disables the console window in build by adding the "CONSOLE_DISABLE" define symbol.
        /// </summary>
        public static bool DisableInBuild { get; set; }
        /// <summary>
        /// The font size to use for console text.
        /// </summary>
        public static int FontSize { get; set; }
        /// <summary>
        /// The font to use for console text.
        /// </summary>
        public static Font Font { get; set; }
        /// <summary>
        /// Enable command autocompletion.
        /// </summary>
        public static bool AutocompleteCommands { get; set; }
        /// <summary>
        /// Enable parameter autocompletion.
        /// </summary>
        public static bool AutocompleteParameters { get; set; }
        /// <summary>
        /// Enable parameter previews.
        /// </summary>
        public static bool ShowParameters { get; set; }
        /// <summary>
        /// Enable highlighting of the parameter under the cursor.
        /// </summary>
        public static bool HighlightActiveParameter { get; set; }
        /// <summary>
        /// Enable coloring of command parameters based on their underlying type.
        /// </summary>
        public static bool TintParameterTypes { get; set; }
        /// <summary>
        /// The key that opens the console.
        /// </summary>
        public static KeyCode ConsoleKey { get; set; }
        /// <summary>
        /// Must the Shift key also be pressed to open the console?
        /// </summary>
        public static bool ModifierShift { get; set; }
        /// <summary>
        /// Must the Control key also be pressed to open the console?
        /// </summary>
        public static bool ModifierControl { get; set; }
        /// <summary>
        /// Must the Alt key also be pressed to open the console?
        /// </summary>
        public static bool ModifierAlt { get; set; }
        /// <summary>
        /// Defines the log level to use for any console log messages.
        /// </summary>
        public static LogLevel LogLevel { get; set; }


        /// <summary>
        /// Defines the text color.
        /// </summary>
        public static Color ColorText { get; set; }
        /// <summary>
        /// Defines the background color.
        /// </summary>
        public static Color ColorBg { get; set; }
        /// <summary>
        /// Defines the background color of selected items.
        /// </summary>
        public static Color ColorSelection { get; set; }
        /// <summary>
        /// Defines the color of bool parameters.
        /// </summary>
        public static Color ParamColorBool { get; set; }
        /// <summary>
        /// Defines the color of integer parameters.
        /// </summary>
        public static Color ParamColorInt { get; set; }
        /// <summary>
        /// Defines the color of float parameters.
        /// </summary>
        public static Color ParamColorFloat { get; set; }
        /// <summary>
        /// Defines the color of enum parameters.
        /// </summary>
        public static Color ParamColorEnum { get; set; }
        /// <summary>
        /// Defines the color of string parameters.
        /// </summary>
        public static Color ParamColorString { get; set; }


        private void OnEnable()
        {
            Refresh(this);
        }

        private void OnValidate()
        {
            _defaultFontSize = Mathf.Max(1, _defaultFontSize);

            Refresh(this);
            if (ConsoleGUI.Singleton != null)
                ConsoleGUI.Singleton.Redraw();
        }

        /// <summary>
        /// Update the variables for the global ConsoleSettings to use.
        /// </summary>
        /// <param name="instance"> The instance whose settings to apply. </param>
        public static void Refresh(ConsoleGUISettings instance)
        {
            HideConsoleGameObject = instance._hideGameObject;
            DisableInBuild = instance._disableInBuild;
            FontSize = instance._defaultFontSize;
            Font = instance._font;
            AutocompleteCommands = instance._autocompleteCommands;
            AutocompleteParameters = instance._autocompleteParameters;
            ShowParameters = instance._showParameters;
            HighlightActiveParameter = instance._highlightActiveParameter;
            TintParameterTypes = instance._tintParameterTypes;
            ConsoleKey = instance._consoleKey;
            ModifierShift = instance._modifierShift;
            ModifierControl = instance._modifierControl;
            ModifierAlt = instance._modifierAlt;
            LogLevel = instance._logLevel;

            ColorText = instance._colorText;
            ColorBg = instance._colorBg;
            ColorSelection = instance._colorSelection;
            ParamColorBool = instance._paramColorBool;
            ParamColorInt = instance._paramColorInt;
            ParamColorFloat = instance._paramColorFloat;
            ParamColorEnum = instance._paramColorEnum;
            ParamColorString = instance._paramColorString;
        }
    }

    /// <summary>
    /// Describes the levels of log information supported.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Nothing is logged.
        /// </summary>
        None = -1,
        /// <summary>
        /// Only unexpected errors and failures are logged.
        /// </summary>
        Error = 0,
        /// <summary>
        /// Abnormal situations that may result in problems are reported, in addition to anything from the LogLevel.Error level.
        /// </summary>
        Warn = 1,
        /// <summary>
        /// High-level informational messages are reported, in addition to anything from the LogLevel.Warn level.
        /// </summary>
        Info = 2,
        /// <summary>
        /// Detailed informational messages are reported, in addition to anything from the LogLevel.Info level.
        /// </summary>
        Verbose = 3,
        /// <summary>
        /// Debugging messages are reported, in addition to anything from the LogLevel.Verbose level.
        /// </summary>
        Debug = 4,
        /// <summary>
        /// Extremely detailed messages are reported, in addition to anything from the LogLevel.Debug level.
        /// </summary>
        Silly = 5
    }
}