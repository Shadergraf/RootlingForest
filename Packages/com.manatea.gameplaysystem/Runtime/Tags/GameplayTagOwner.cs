using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class GameplayTagOwner : MonoBehaviour
    {
        [SerializeField]
        private List<GameplayTag> m_Tags;

        public ReadOnlyCollection<GameplayTag> Tags => m_Tags.AsReadOnly();
    }
}
