using Manatea.GameplaySystem;
using Manatea;
using UnityEngine;

public class GameplayFeaturePreference : MonoBehaviour
{
    [SerializeField]
    private Fetched<GameplayTagOwner> m_TagOwner = new(FetchingType.InParents);
    [SerializeField]
    private GameplayTag[] m_TagsToAdd;


    private void Awake()
    {
        m_TagOwner.FetchFrom(gameObject);
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
