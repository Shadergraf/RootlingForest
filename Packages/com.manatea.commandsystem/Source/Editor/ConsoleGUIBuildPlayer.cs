using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Manatea.CommandSystem
{
    public sealed class ConsoleGUIBuildPlayer : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = ConsoleGUISettingsProvider.Load();
            ConsoleGUISettings.Refresh(settings);

            // Add current ConsoleGUISettings to build.
            var settingsType = settings.GetType();
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.RemoveAll(s => s != null && s.GetType() == settingsType);
            preloadedAssets.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }
    }
}