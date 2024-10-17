using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manatea.GameplaySystem
{
    [Serializable]
    public struct GameplayAttributeModification
    {
        public GameplayAttribute Attribute;
        public GameplayAttributeModifierType Type;
        public float Value;
    }

    [Serializable]
    public struct GameplayAttributeModifier
    {
        public GameplayAttributeModifierType Type;
        public float Value;
    }

    [Serializable]
    public class GameplayAttributeModifierInstance
    {
        public event Action OnChange;

        private GameplayAttributeModifier m_Modifier;


        public GameplayAttributeModifierInstance()
        { }
        public GameplayAttributeModifierInstance(GameplayAttributeModifier modifier)
        {
            m_Modifier = modifier;
        }
        public GameplayAttributeModifierInstance(GameplayAttributeModifierType type, float value)
        {
            m_Modifier = new GameplayAttributeModifier()
            {
                Type = type,
                Value = value,
            };
        }

        public GameplayAttributeModifierType Type
        {
            get => m_Modifier.Type;
            set
            {
                if (value != m_Modifier.Type)
                {
                    m_Modifier.Type = value;
                    OnChange?.Invoke();
                }
            }
        }
        public float Value
        {
            get => m_Modifier.Value;
            set
            {
                if (value != m_Modifier.Value)
                {
                    m_Modifier.Value = value;
                    OnChange?.Invoke();
                }
            }
        }
    }

    public enum GameplayAttributeModifierType
    {
        Additive = 0,
        Multiplicative = 1,
    }
}
