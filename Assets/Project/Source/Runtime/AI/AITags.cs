using Manatea.GameplaySystem;
using UnityEngine;

public class AITags : MonoBehaviour
{
    [SerializeField]
    private GameplayTag[] m_Tags;

    public GameplayTag[] Tags => m_Tags;
}
