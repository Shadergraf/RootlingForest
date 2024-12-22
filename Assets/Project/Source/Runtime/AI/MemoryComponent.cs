using Manatea.GameplaySystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class MemoryComponent : MonoBehaviour
    {
        [SerializeField]
        private float m_ObjectPermanenceTime = 5;

        public List<MemoryMemento> m_Mementos = new List<MemoryMemento>();

        public void FixedUpdate()
        {
            for (int i = 0; i < m_Mementos.Count; i++)
            {
                m_Mementos[i].Update(Time.fixedDeltaTime);

                if (m_Mementos[i].TimeSinceLastVerification > m_ObjectPermanenceTime)
                {
                    m_Mementos.RemoveAt(i);
                    i--;
                }
            }
        }


        public void Remember(GameObject obj)
        {
            int index = -1;
            MemoryMemento memento = null;
            for (int i = 0; i < m_Mementos.Count; i++)
            {
                if (m_Mementos[i].AssociatedObject == obj)
                {
                    index = i;
                    memento = m_Mementos[i];
                    break;
                }
            }
            if (index == -1)
                memento = new MemoryMemento();

            memento.TimeSinceLastVerification = 0;
            memento.AssociatedObject = obj;
            memento.LastPosition = obj.transform.position;

            if (index != -1)
                m_Mementos[index] = memento;
            else
                m_Mementos.Add(memento);
        }
        public void Forget(GameObject obj)
        {
            for (int i = 0; i < m_Mementos.Count; i++)
            {
                if (m_Mementos[i].AssociatedObject == obj)
                {
                    m_Mementos.RemoveAt(i);
                    return;
                }
            }
        }

        internal bool Query(MemoryQuery query, out MemoryMemento memento)
        {
            memento = null;

            foreach (MemoryMemento mem in m_Mementos)
            {
                if (!mem.AssociatedObject)
                    continue;

                if (mem.AssociatedObject.TryGetComponent(out GameplayTagOwner tagOwner) && tagOwner.SatisfiesTagFilter(query.TagFilter))
                {
                    memento = mem;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class MemoryMemento
    {
        public float TimeSinceInception;
        public float TimeSinceLastVerification;
        public GameObject AssociatedObject;
        public Vector3 LastPosition;

        public void Update(float dt)
        {
            TimeSinceInception += dt;
            TimeSinceLastVerification += dt;
        }
    }
}
