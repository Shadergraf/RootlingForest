using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Manatea.BuildPipeline
{
    public static class Build
    {
        private static BuildData s_BuildConfiguration;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            s_BuildConfiguration = Resources.Load<BuildData>("BuildData");
        }

#if UNITY_EDITOR
        public static string BuildSha => "[GitRevision]";
#else
        public static string BuildSha => s_BuildConfiguration.BuildSha;
#endif
    }
}
