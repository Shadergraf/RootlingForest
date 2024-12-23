using Manatea.GameplaySystem;
using System.Collections;
using UnityEngine;

public abstract class BaseAbility : MonoBehaviour
{
    [SerializeField]
    private GameObject m_Target;
    [SerializeField]
    private GameplayTagFilter m_AutoActivateAbilityFilter;
    [SerializeField]
    private GameplayTagFilter m_EnableAbilityFilter;
    [SerializeField]
    private bool m_RequireTarget;
    [SerializeField]
    private GameplayTagFilter m_EnableAbilityTargetFilter;
    [SerializeField]
    private GameplayTagFilter m_StayActiveAbilityFilter;


    public GameObject Target
    {
        get { return m_Target; }
        set
        {
            if (enabled)
            {
                Debug.Assert(false, "Target can only be set if the ability is disabled!");
                return;
            }
            m_Target = value;
        }
    }
    public GameplayTagOwner TagOwner => m_TagOwner;

    private GameplayTagOwner m_TagOwner;
    private bool m_AbilityStarted;

    private void Awake()
    {
        if (!m_AutoActivateAbilityFilter.IsEmpty)
            StartCoroutine(CheckAutoActivation());
    }

    protected virtual void OnEnable()
    {
        m_TagOwner = GetComponentInParent<GameplayTagOwner>();
        if (m_TagOwner && !m_TagOwner.SatisfiesTagFilter(m_EnableAbilityFilter))
        {
            enabled = false;
            return;
        }

        m_AbilityStarted = true;

        AbilityEnabled();
    }

    protected virtual void OnDisable()
    {
        if (m_AbilityStarted)
        {
            AbilityDisabled();
        }

        m_AbilityStarted = false;
    }

    protected virtual void LateUpdate()
    {
        if (m_TagOwner && !m_TagOwner.SatisfiesTagFilter(m_StayActiveAbilityFilter))
        {
            enabled = false;
        }
    }


    public bool CouldActivateAbilityWithTarget(GameObject target)
    {
        if (m_RequireTarget && target == null)
            return false;

        if (!m_EnableAbilityTargetFilter.IsEmpty)
        {
            GameplayTagOwner tagOwner = target.GetComponent<GameplayTagOwner>();
            return tagOwner && tagOwner.SatisfiesTagFilter(m_EnableAbilityTargetFilter);
        }

        return true;
    }

    private IEnumerator CheckAutoActivation()
    {
        while (true)
        {
            if (!m_TagOwner)
                m_TagOwner = GetComponentInParent<GameplayTagOwner>();

            if (m_TagOwner && m_TagOwner.SatisfiesTagFilter(m_AutoActivateAbilityFilter))
            {
                enabled = true;
            }

            yield return null;
        }
    }

    protected abstract void AbilityEnabled();
    protected abstract void AbilityDisabled();
}
