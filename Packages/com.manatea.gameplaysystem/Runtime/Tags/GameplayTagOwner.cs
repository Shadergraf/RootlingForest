using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.GameplaySystem
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class GameplayTagOwner : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("m_MyTags")]
        private GameplayTagCollection m_Tags;

        public GameplayTagCollection Tags => m_Tags;
    }
}
