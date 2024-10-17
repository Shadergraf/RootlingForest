using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-98)]
    public class GameplayEffectOwner : MonoBehaviour
    {
        [SerializeField]
        private GameplayTagOwner m_TagOwner;
        [SerializeField]
        private GameplayAttributeOwner m_AttributeOwner;

        public GameplayTagOwner TagOwner => m_TagOwner;
        public GameplayAttributeOwner AttributeOwner => m_AttributeOwner;

        public List<GameplayEffectInstance> m_ActiveEffects;

        private List<GameplayEffectInstance> m_ScheduledForRemoval = new List<GameplayEffectInstance>(8);


        private void FixedUpdate()
        {
            m_ScheduledForRemoval.Clear();
            for (int i = 0; i < m_ActiveEffects.Count; i++)
            {
                var effect = m_ActiveEffects[i];
                effect.Update(Time.fixedDeltaTime);
                if (effect.Expired)
                {
                    m_ScheduledForRemoval.Add(effect);
                }
            }

            for (int i = 0; i < m_ScheduledForRemoval.Count; i++)
            {
                RemoveEffect(m_ScheduledForRemoval[i]);
            }
        }


        public bool AddEffect(GameplayEffectInstance effectInst)
        {
            if (m_ActiveEffects.Contains(effectInst))
                return false;

            bool attached = effectInst.AddTo(this);
            if (attached)
            {
                m_ActiveEffects.Add(effectInst);
            }

            // Remove effect immediately if it is already expired (eg for instant effects)
            if (effectInst.Expired)
            {
                RemoveEffect(effectInst);
            }

            enabled = m_ActiveEffects.Count > 0;

            return attached;
        }
        public GameplayEffectInstance AddEffect(GameplayEffect effect)
        {
            if (!effect)
            {
                Debug.LogError("Effect was null!", gameObject);
                return null;
            }

            var effectInst = new GameplayEffectInstance(effect);
            bool wasAdded = AddEffect(effectInst);

            enabled = m_ActiveEffects.Count > 0;

            return wasAdded ? effectInst : null;
        }

        public bool RemoveEffect(GameplayEffectInstance effectInst)
        {
            if (effectInst == null)
            {
                Debug.LogError("Effect instance was null!", gameObject);
                return false;
            }
            if (!m_ActiveEffects.Contains(effectInst))
                return false;

            effectInst.Remove();
            m_ActiveEffects.Remove(effectInst);

            enabled = m_ActiveEffects.Count > 0;

            return true;
        }
    }
}
