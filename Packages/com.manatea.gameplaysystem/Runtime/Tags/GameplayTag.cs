using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [CreateAssetMenu(menuName = GameplaySystemGlobals.AssetCreationPath + "Tag")]
    public class GameplayTag : GameplayAsset
    {
        private GameplayTag m_OldParent;
        [SerializeField]
        protected GameplayTag m_Parent;


        public GameplayTag Parent => m_Parent;

        public int AncestorCount => m_Parent ? m_Parent.AncestorCount + 1 : 0;


        public void OnValidate()
        {
            // Set new parent in inspector
            if (m_OldParent != m_Parent)
            {
                GameplayTag newParent = m_Parent;
                m_Parent = m_OldParent;
                if (!SetParent(newParent))
                    Debug.LogError($"Could not set parent of { name } to { newParent.name }.");
            }
        }

        public bool SetParent(GameplayTag newParent)
        {
            if (!IsValidParent(newParent))
                return false;

            m_OldParent = newParent;
            m_Parent = newParent;

            return true;
        }

        private bool IsValidParent(GameplayTag parent)
        {
            if (parent == null)
                return true;
            if (this == parent)
                return false;

            HashSet<GameplayTag> ids = new HashSet<GameplayTag>() { this, parent };

            GameplayTag current = parent;
            while (current.m_Parent)
            {
                current = current.m_Parent;
                if (ids.Contains(current))
                {
                    // Parent chain contains loops
                    return false;
                }
                ids.Add(current);
            }

            return true;
        }
    }
}
