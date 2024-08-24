using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class OverworldManager : MonoBehaviour
    {
        private static OverworldManager s_Instance;

        public static OverworldManager Instance => s_Instance;


        private void Awake()
        {
            Debug.Log("Overworld manager created.");

            s_Instance = this;
        }
    }
}
