using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using Object = UnityEngine.Object;

namespace Manatea.BuildPipeline
{
    public static class BuildSystem
    {
        public static readonly string s_BuildsRootDirectory = "Saved\\Builds";

        private static readonly string s_Eol = Environment.NewLine;
        private static readonly string s_BuildWarningPrefix = "(Warning) ";
        private static readonly string s_BuildErrorPrefix = "(Error) ";
        private static readonly string[] s_CommandLineSecrets = { "androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass" };

        private static List<PreBuildHook> s_PreBuildHooks = new List<PreBuildHook>();
        private static List<PreBuildHook> s_PostBuildHooks = new List<PreBuildHook>();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            BindCustomBuildPlayerHandler();
        }

        private static void BindCustomBuildPlayerHandler()
        {
            // We are not using BuildPlayerWindow.RegisterBuildPlayerHandler to omit the warning message
            var eventInfo = typeof(BuildPlayerWindow).GetField("buildPlayerHandler", BindingFlags.Static | BindingFlags.NonPublic);
            var methodInfo = typeof(BuildSystem).GetMethod(nameof(RunEditorPlayerBuild), BindingFlags.Static | BindingFlags.NonPublic);
            eventInfo.SetValue(null, (Action<BuildPlayerOptions>)Delegate.CreateDelegate(typeof(Action<BuildPlayerOptions>), methodInfo));
        }


        public static void RunGithubBuild()
        {
            Debug.Log("Using the Manatea build system");

            Dictionary<string, string> options = GetValidatedCommandLineOptions();
            options.GetValueOrDefault("buildConfig");
            // Find build config
            if (!options.ContainsKey("buildConfig"))
            {
                Debug.LogError("No build configuration provided!");
                return;
            }
            string[] buildConfigAssets = AssetDatabase.FindAssets("t:" + nameof(BuildConfiguration) + " " + options["buildConfig"]);
            if (buildConfigAssets.Length == 0)
            {
                Debug.LogError("No build configuration found for " + options["buildConfig"]);
                return;
            }
            var buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfiguration>(AssetDatabase.GUIDToAssetPath(buildConfigAssets[0]));

            // Setup build version
            SetupBuildVersion(options.GetValueOrDefault("buildVersion"), options.GetValueOrDefault("sha"));

            // TODO build path of "build/" is defined by gameCI but could be changed by the user
            string path = "build/" +
                          buildConfig.name + "_" + PlayerSettings.bundleVersion + "/" +
                          PlayerSettings.productName + "." +
                          GetFileExtensionFromBuildTarget(buildConfig.BuildTarget);

            // Build with config
            BuildConfig(buildConfig, path);

            // Cleanup editor build version
            CleanupBuildVersion();

            // Save any build modified assets
            AssetDatabase.SaveAssets();
        }

        public static bool RunEditorBuild(BuildConfiguration buildConfig, BuildOptions additionalOptions = BuildOptions.None)
        {
            Debug.Log("Using the Manatea build system");

            // TODO make sure that every step is performed, even when some steps in between fail
            //      Otherwise some cleanup operations might not run correctly

            // TODO "clear console on build" will not work correctly as it clears all logs that
            //      happened between now and the actual start of the unity build process

            // TODO downloading the souce of a tagged git commit from github does not include git tags
            //      as such we cant find the tag in git anymore to set the build version. But we can
            //      use the directory name of the downloaded repo wich has the correct tag as a postfix

            if (!buildConfig)
            {
                Debug.LogError("No build configurations was provided!");
                return false;
            }

            if (!IsBuildTargetSupported(buildConfig.BuildTarget))
            {
                Debug.LogError(string.Format("Build target {0} is not supported!", buildConfig.BuildTarget));
                return false;
            }

            GetVersionAndRevisionFromGit(out string version, out string revision);
            SetupBuildVersion(version, revision);

            string path = s_BuildsRootDirectory + "\\" +
                          buildConfig.name + "_" + PlayerSettings.bundleVersion + "/" +
                          PlayerSettings.productName + "." +
                          GetFileExtensionFromBuildTarget(buildConfig.BuildTarget);

            // Build with config
            BuildReport report = BuildConfig(buildConfig, path, additionalOptions);

            // Cleanup editor build version
            CleanupBuildVersion();

            // Save any build modified assets
            AssetDatabase.SaveAssets();

            return report.summary.result == BuildResult.Succeeded;
        }

        private static void RunEditorPlayerBuild(BuildPlayerOptions buildOptions)
        {
            Debug.Log("Using the Manatea build system");

            GetVersionAndRevisionFromGit(out string version, out string revision);
            SetupBuildVersion(version, revision);

            // Build without config
            BuildPlayer(buildOptions);

            // Cleanup editor build version
            CleanupBuildVersion();

            // Save any build modified assets
            AssetDatabase.SaveAssets();
        }


        public static void RegisterPreBuildAction(BuildAction action, int order = 0)
        {
            s_PreBuildHooks.Add(new PreBuildHook() { Action = action, Order = order });
        }
        public static void RegisterPostBuildAction(BuildAction action, int order = 0)
        {
            s_PostBuildHooks.Add(new PreBuildHook() { Action = action, Order = order });
        }


        private static BuildPlayerOptions CreateBuildPlayerOptionsFromBuildConfig(BuildConfiguration config)
        {
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.target = config.BuildTarget;
            buildOptions.targetGroup = GetBuildTargetGroupFromBuildTarget(config.BuildTarget);
            buildOptions.options = config.BuildOptions;
            buildOptions.scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            buildOptions.locationPathName = s_BuildsRootDirectory + "\\" +
                                            config.name + "\\" +
                                            PlayerSettings.bundleVersion + "\\" +
                                            PlayerSettings.productName + "." +
                                            GetFileExtensionFromBuildTarget(config.BuildTarget);
            // TODO add the extra scripting defines to the build options maybe?
            return buildOptions;
        }

        private static BuildReport BuildConfig(BuildConfiguration buildConfig, string path, BuildOptions additionalBuildOptions = BuildOptions.None)
        {
            var buildOptions = CreateBuildPlayerOptionsFromBuildConfig(buildConfig);

            if (!string.IsNullOrEmpty(path))
                buildOptions.locationPathName = path;

            buildOptions.options |= additionalBuildOptions;

            Debug.Log("Setup build config.");
            buildConfig.Apply(ref buildOptions);

            // Actual build
            BuildReport report = BuildPlayer(buildOptions);

            Debug.Log("Cleanup build config.");
            buildConfig.Clear();

            return report;
        }
        private static BuildReport BuildPlayer(BuildPlayerOptions buildOptions)
        {
            // TODO maybe add a prioritized pre-build process queue here?

            // TODO test if we can omit this code...
            //Debug.Log("Setup & cache editor build target.");
            //BuildTarget cachedBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            //if (cachedBuildTarget != buildOptions.target)
            //    EditorUserBuildSettings.SwitchActiveBuildTarget(buildOptions.targetGroup, buildOptions.target);

            s_PreBuildHooks.Sort((h1, h2) => h1.Order.CompareTo(h2.Order));
            foreach (var hook in s_PreBuildHooks)
                hook.Action.Invoke(ref buildOptions);

            Debug.Log("Build player at path: " + buildOptions.locationPathName);
            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildOptions);
            LogBuildReport(report);

            s_PostBuildHooks.Sort((h1, h2) => h1.Order.CompareTo(h2.Order));
            foreach (var hook in s_PostBuildHooks)
                hook.Action.Invoke(ref buildOptions);
            
            //Debug.Log("Cleanup editor build target.");
            //if (cachedBuildTarget != buildOptions.target)
            //    EditorUserBuildSettings.SwitchActiveBuildTarget(GetBuildTargetGroupFromBuildTarget(cachedBuildTarget), cachedBuildTarget);

            return report;
        }


        public static void GetVersionAndRevisionFromGit(out string buildVersion, out string revision)
        {
            buildVersion = "";
            revision = "";
            try
            {
                buildVersion = Git.GitOperator.GetBuildVersion();
                revision = Git.GitOperator.GetLastCommitId(false);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        private static void SetupBuildVersion(string buildVersion, string revision)
        {
            try
            {
                Debug.Log("Setup BuildVersion");

                if (string.IsNullOrEmpty(buildVersion))
                    buildVersion = "invalid";
                if (string.IsNullOrEmpty(revision))
                    revision = "invalid";

                PlayerSettings.bundleVersion = buildVersion;
                PlayerSettings.macOS.buildNumber = buildVersion;
                PlayerSettings.Android.bundleVersionCode = buildVersion.GetHashCode();

                Object buildData = Resources.Load("BuildData");
                if (!buildData)
                {
                    // TODO create asset if it doesn't exist
                    Debug.LogError("BuildData asset did not exist");
                    return;
                }

                SerializedObject soBuildData = new SerializedObject(buildData);

                soBuildData.FindProperty("m_BuildSha").stringValue = revision;

                soBuildData.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        private static void CleanupBuildVersion()
        {
            try
            {
                Debug.Log("Cleanup BuildVersion");

                PlayerSettings.bundleVersion = "";
                PlayerSettings.macOS.buildNumber = "0";
                PlayerSettings.Android.bundleVersionCode = 0;

                Object buildData = Resources.Load("BuildData");
                if (!buildData)
                {
                    Debug.LogError("BuildData asset did not exist");
                    return;
                }

                SerializedObject soBuildData = new SerializedObject(buildData);

                if (!string.IsNullOrEmpty(PlayerSettings.bundleVersion))
                    Debug.LogWarning("PlayerSettings.bundleVersion will be overwritten by the BuildSystem!");

                soBuildData.FindProperty("m_BuildSha").stringValue = "";

                soBuildData.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }


        private static void LogBuildReport(BuildReport report)
        {
            switch (report.summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log(string.Format("The build for target {0} was successful!", report.summary.platform));
                    break;
                case BuildResult.Failed:
                    Debug.LogError(string.Format("The build for target {0} failed!", report.summary.platform));
                    break;
                case BuildResult.Cancelled:
                    Debug.LogError(string.Format("The build for target {0} was cancelled!", report.summary.platform));
                    break;
                case BuildResult.Unknown:
                default:
                    Debug.LogError(string.Format("The build for target {0} produced an unknown result!", report.summary.platform));
                    break;
            }
        }


        public static bool IsBuildTargetSupported(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.Switch:
                case BuildTarget.Android:
                case BuildTarget.EmbeddedLinux:
                    return true;
                case BuildTarget.StandaloneOSX:
                case BuildTarget.iOS:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.WebGL:
                case BuildTarget.WSAPlayer:
                case BuildTarget.PS4:
                case BuildTarget.XboxOne:
                case BuildTarget.tvOS:
                case BuildTarget.Lumin:
                case BuildTarget.Stadia:
                case BuildTarget.LinuxHeadlessSimulation:
                case BuildTarget.GameCoreXboxSeries:
                case BuildTarget.GameCoreXboxOne:
                case BuildTarget.PS5:
                case BuildTarget.NoTarget:
                default:
                    return false;
            }
        }
        public static BuildTargetGroup GetBuildTargetGroupFromBuildTarget(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.WebGL:
                    return BuildTargetGroup.WebGL;
                case BuildTarget.WSAPlayer:
                    return BuildTargetGroup.WSA;
                case BuildTarget.PS4:
                    return BuildTargetGroup.PS4;
                case BuildTarget.XboxOne:
                    return BuildTargetGroup.XboxOne;
                case BuildTarget.tvOS:
                    return BuildTargetGroup.tvOS;
                case BuildTarget.Switch:
                    return BuildTargetGroup.Switch;
                case BuildTarget.Lumin:
                    return BuildTargetGroup.Lumin;
                case BuildTarget.Stadia:
                    return BuildTargetGroup.Stadia;
                case BuildTarget.LinuxHeadlessSimulation:
                    return BuildTargetGroup.LinuxHeadlessSimulation;
                case BuildTarget.GameCoreXboxSeries:
                    return BuildTargetGroup.GameCoreXboxSeries;
                case BuildTarget.GameCoreXboxOne:
                    return BuildTargetGroup.GameCoreXboxOne;
                case BuildTarget.PS5:
                    return BuildTargetGroup.PS5;
                case BuildTarget.EmbeddedLinux:
                    return BuildTargetGroup.EmbeddedLinux;
                case BuildTarget.NoTarget:
                default:
                    return BuildTargetGroup.Unknown;
            }
        }
        public static string GetFileExtensionFromBuildTarget(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                case BuildTarget.iOS:
                    throw new NotImplementedException();
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "exe";
                case BuildTarget.StandaloneLinux64:
                    throw new NotImplementedException();
                case BuildTarget.Android:
                    return "apk";
                case BuildTarget.WebGL:
                    throw new NotImplementedException();
                case BuildTarget.WSAPlayer:
                    throw new NotImplementedException();
                case BuildTarget.PS4:
                    throw new NotImplementedException();
                case BuildTarget.XboxOne:
                    throw new NotImplementedException();
                case BuildTarget.tvOS:
                    throw new NotImplementedException();
                case BuildTarget.Switch:
                    return "nss";
                case BuildTarget.Lumin:
                    throw new NotImplementedException();
                case BuildTarget.Stadia:
                    throw new NotImplementedException();
                case BuildTarget.LinuxHeadlessSimulation:
                    throw new NotImplementedException();
                case BuildTarget.GameCoreXboxSeries:
                    throw new NotImplementedException();
                case BuildTarget.GameCoreXboxOne:
                    throw new NotImplementedException();
                case BuildTarget.PS5:
                    throw new NotImplementedException();
                case BuildTarget.EmbeddedLinux:
                    return "";
                case BuildTarget.NoTarget:
                default:
                    throw new NotSupportedException();
            }
        }


        private static Dictionary<string, string> GetValidatedCommandLineOptions()
        {
            ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out string _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out string buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            {
                EditorApplication.Exit(121);
            }

            if (!validatedOptions.TryGetValue("customBuildPath", out string _))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }

            const string defaultCustomBuildName = "TestBuild";
            if (!validatedOptions.TryGetValue("customBuildName", out string customBuildName))
            {
                Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }
            else if (customBuildName == "")
            {
                Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }

            return validatedOptions;
        }
        private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();

            Console.WriteLine(
                $"{s_Eol}" +
                $"###########################{s_Eol}" +
                $"#    Parsing settings     #{s_Eol}" +
                $"###########################{s_Eol}" +
                $"{s_Eol}"
            );

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {

                // Parse flag
                bool isFlag = args[current].StartsWith("-");
                if (!isFlag) continue;
                string flag = args[current].TrimStart('-');

                // Parse optional value
                bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
                string value = flagHasValue ? args[next].TrimStart('-') : "";
                bool secret = s_CommandLineSecrets.Contains(flag);
                string displayValue = secret ? "*HIDDEN*" : "\"" + value + "\"";

                // Assign
                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
        }

        private struct PreBuildHook
        {
            public int Order;
            public BuildAction Action;
        }

        public delegate void BuildAction(ref BuildPlayerOptions options);
    }
}
