using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [CreateAssetMenu(menuName = GameplaySystemGlobals.AssetCreationPath + "AttributeModification")]
    public class GameplayAttributeModification : ScriptableObject
    {
        [SerializeField]
        private GameplayAttribute m_Attribute;
        [SerializeField]
        private GameplayAttributeModificationType m_Type;
        [SerializeField]
        private float m_ValueModification;

        public GameplayAttribute Attribute
        { get { return m_Attribute; } }
        public GameplayAttributeModificationType Type
        { get { return m_Type; } }
        public float ValueModification
        { get { return m_ValueModification; } }
    }

    public enum GameplayAttributeModificationType
    {
        BaseOffset,
        TemporaryOffset,
        TemporaryMultiplier,
    }
}
