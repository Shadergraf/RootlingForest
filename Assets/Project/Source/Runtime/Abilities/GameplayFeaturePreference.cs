using Manatea.GameplaySystem;
using Manatea;
using UnityEngine;

public class GameplayFeaturePreference : MonoBehaviour
{
    [SerializeField]
    private Optional<GameplayTagOwner> m_TagOwner;
    [SerializeField]
    private GameplayTag[] m_TagsToAdd;


    private void Awake()
    {
        if (!m_TagOwner.value)
            m_TagOwner.value = GetComponentInParent<GameplayTagOwner>();
    }

    private void OnEnable()
    {
        m_TagOwner.value.AddManagedRange(m_TagsToAdd);
    }
    private void OnDisable()
    {
        m_TagOwner.value.RemoveManagedRange(m_TagsToAdd);
    }
}
