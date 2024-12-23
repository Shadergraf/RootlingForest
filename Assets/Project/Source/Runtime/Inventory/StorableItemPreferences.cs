using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    public class StorableItemPreferences : GameplayFeaturePreference
    {
        [SerializeField]
        private ItemDescription m_Description;
    }
}
