using UnityEngine;

namespace Manatea.Managers
{
    public abstract class ManagerProxy : ScriptableObject
    {
        internal abstract void Register(ManagerBehaviour manager);
        internal abstract void Unregister(ManagerBehaviour manager);
    }

    public abstract class ManagerProxy<T> : ManagerProxy where T : ManagerBehaviour
    {
        private T m_Manager;
        protected T Manager => m_Manager;

        internal sealed override void Register(ManagerBehaviour manager)
        {
            Debug.Assert(m_Manager == null, "This proxy already has a manager set up.", this);
            m_Manager = (T)manager;
        }
        internal sealed override void Unregister(ManagerBehaviour manager)
        {
            Debug.Assert(m_Manager == manager, "The manager to unregister was not set up.", this);
            m_Manager = null;
        }
    }
}
