using UnityEngine;
using UnityEngine.Events;

namespace Manatea.GameplaySystem
{
    public class GameplayTagListener : MonoBehaviour
    {
        [SerializeField]
        private Fetched<GameplayTagOwner> m_TagOwner = new(FetchingType.InParents);
        [SerializeField]
        private GameplayTag m_Tag;
        [SerializeField]
        private UnityEvent m_TagAdded;
        [SerializeField]
        private UnityEvent m_TagRemoved;

        private bool m_CurrentValue = false;


        private void Awake()
        {
            m_TagOwner.FetchFrom(gameObject);

            m_CurrentValue = m_TagOwner.value.HasTag(m_Tag);
            if (m_CurrentValue)
                m_TagAdded.Invoke();
            else
                m_TagRemoved.Invoke();
        }


        private void FixedUpdate()
        {
            bool newValue = m_TagOwner.value.HasTag(m_Tag);
            if (newValue != m_CurrentValue)
            {
                if (newValue)
                    m_TagAdded.Invoke();
                else
                    m_TagRemoved.Invoke();
            }
            m_CurrentValue = newValue;
        }
    }
}
