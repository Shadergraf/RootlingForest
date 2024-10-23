using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [Serializable]
    public class GameplayEffectInstance
    {
        private GameplayEffectOwner m_EffectOwner;

        private GameplayEffect m_Effect;

        private float m_Time;
        private bool m_Expired;

        private List<GameplayTag> m_ManagedTags = new List<GameplayTag>();
        private List<AddedGameplayAttributeModification> m_AddedModifications = new List<AddedGameplayAttributeModification>();

        public GameplayEffect Effect => m_Effect;
        public float Time => m_Time;
        public bool Expired => m_Expired;


        public GameplayEffectInstance(GameplayEffect effect)
        {
            m_Effect = effect;
        }

        internal void Update(float deltaTime)
        {
            m_Time += deltaTime;
            if (m_Effect.Lifetime == EffectLifetime.Duration && m_Time > m_Effect.Duration)
            {
                m_Expired = true;
            }
        }

        internal bool AddTo(GameplayEffectOwner effectOwner)
        {
            if (m_EffectOwner)
                return false;

            m_EffectOwner = effectOwner;
            Debug.Assert(m_ManagedTags.Count == 0, "There cant be any already added tags as this effect is being freshly applied right now!", m_EffectOwner.gameObject);
            Debug.Assert(m_AddedModifications.Count == 0, "There cant be any already added tags as this effect is being freshly applied right now!", m_EffectOwner.gameObject);

            m_Expired = false;
            m_Time = 0;

            switch (m_Effect.Lifetime)
            {
                case EffectLifetime.Instant:
                    m_Expired = true;
                    break;
            }

            if (m_EffectOwner.TagOwner)
            {
                if (m_Effect.Lifetime == EffectLifetime.Instant)
                {
                    m_EffectOwner.TagOwner.AddUnmanagedRange(Effect.TagsToAdd);
                }
                else
                {
                    m_EffectOwner.TagOwner.AddManagedRange(Effect.TagsToAdd);
                    m_ManagedTags.AddRange(Effect.TagsToAdd);
                }
            }

            if (m_EffectOwner.AttributeOwner)
            {
                for (int i = 0; i < Effect.GameplayAttributeModifications.Length; i++)
                {
                    // Instant effects dont add modifications but modify base values directly
                    if (Effect.Lifetime == EffectLifetime.Instant)
                    {
                        var modification = Effect.GameplayAttributeModifications[i];
                        var attrInst = m_EffectOwner.AttributeOwner.GetAttributeInstance(modification.Attribute);
                        if (attrInst != null)
                        {
                            float value = attrInst.BaseValue;
                            if (modification.Type == GameplayAttributeModifierType.Additive)
                            {
                                value += modification.Value;
                            }
                            if (modification.Type == GameplayAttributeModifierType.Multiplicative)
                            {
                                value *= modification.Value;
                            }

                            for (int j = 0; j < attrInst.PostProcessors.Length; j++)
                            {
                                attrInst.PostProcessors[j].Process(m_EffectOwner.AttributeOwner, modification.Attribute, ref value);
                            }

                            attrInst.BaseValue = value;
                        }
                    }
                    else
                    {
                        var modification = Effect.GameplayAttributeModifications[i];
                        var addedModification = new AddedGameplayAttributeModification()
                        {
                            Attribute = modification.Attribute,
                            ModifierInst = new GameplayAttributeModifierInstance(modification.Type, modification.Value),
                        };
                        m_EffectOwner.AttributeOwner.AddAttributeModifier(addedModification.Attribute, addedModification.ModifierInst);
                        m_AddedModifications.Add(addedModification);
                    }
                }
            }

            if (m_EffectOwner.EventReceiver)
            {
                for (int i = 0; i < Effect.Events.Length; i++)
                {
                    m_EffectOwner.EventReceiver.SendEventImmediate(Effect.Events[i], null);
                }
            }

            return true;
        }
        internal bool Remove()
        {
            // Instant effects dont remove their modifications
            if (Effect.Lifetime != EffectLifetime.Instant)
            {
                if (m_EffectOwner.TagOwner)
                {
                    m_EffectOwner.TagOwner.RemoveManagedRange(m_ManagedTags);
                    m_ManagedTags.Clear();
                }
                if (m_EffectOwner.AttributeOwner)
                {
                    for (int i = 0; i < m_AddedModifications.Count; i++)
                    {
                        var addedModification = m_AddedModifications[i];
                        m_EffectOwner.AttributeOwner.RemoveAttributeModifier(addedModification.Attribute, addedModification.ModifierInst);
                    }
                    m_AddedModifications.Clear();
                }
            }

            m_EffectOwner = null;

            return true;
        }

        private struct AddedGameplayAttributeModification
        {
            public GameplayAttribute Attribute;
            public GameplayAttributeModifierInstance ModifierInst;
        }
    }
}
