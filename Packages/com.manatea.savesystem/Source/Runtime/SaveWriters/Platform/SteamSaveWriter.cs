#if STEAMWORKS

// TODO Add steamworks API and test steam save writer

using UnityEngine;
using Steamworks;

namespace Manatea.SaveSystem
{
    public class SteamSaveWriter : FileSaveWriter
    {
        public override bool UsePrettyPrint => Debug.isDebugBuild;
        public override string DirectoryPath => Application.persistentDataPath + "/Saves/" + SteamUser.GetSteamID().m_SteamID.ToString() + "/";
    }
}

#endif