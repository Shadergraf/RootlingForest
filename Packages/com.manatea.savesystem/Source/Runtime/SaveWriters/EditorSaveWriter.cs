#if UNITY_EDITOR

using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace Manatea.SaveSystem
{
    public class EditorFileSaveWriter : FileSaveWriter
    {
        public override bool UsePrettyPrint => true;
        public override string DirectoryPath => Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/Saved/Saves/";
    }
    public class EditorIniSaveWriter : IniSaveWriter
    {
        public override string DirectoryPath => Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/Saved/Saves/";
    }
    public class EditorBinarySaveWriter : BinarySaveWriter
    {
        public override string DirectoryPath => Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/Saved/Saves/";
    }
}

#endif