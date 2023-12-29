using UnityEngine;

namespace Manatea.Managers
{
    public abstract class ManagerBehaviour : MonoBehaviour
    { }

    [DefaultExecutionOrder(-1000)]
    public abstract class ManagerBehaviour<T> : ManagerBehaviour where T : ManagerProxy
    {
        [SerializeField]
        protected T Proxy;

        protected virtual void Awake()
        {
            Proxy.Register(this);
        }
        protected virtual void OnDestroy()
        {
            Proxy.Unregister(this);
        }
    }
}
