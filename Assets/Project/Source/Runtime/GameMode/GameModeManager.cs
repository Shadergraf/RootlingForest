using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineTriggerAction.ActionSettings;

namespace Manatea.RootlingForest
{
    public class GameModeManager : MonoBehaviour
    {
        private static GameModeManager s_Instance;

        public static GameModeManager Instance => s_Instance;

        private GameMode m_CurrentGameMode;
        public GameMode GameMode => m_CurrentGameMode;


        private void Awake()
        {
            Debug.Log("GameMode manager created.");

            s_Instance = this;
        }


        public void RegisterGameMode(GameMode gameMode)
        {
            m_CurrentGameMode = gameMode;
        }
        public void UnregisterGameMode()
        {
            m_CurrentGameMode = null;
        }
    }
}
