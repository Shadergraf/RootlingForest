using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class GameplayTagOwner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The initial permanent tags of this object. These will be stored as managed tags.")]
        private List<GameplayTag> m_InitialPermanentTags;
        [SerializeField]
        [Tooltip("The initial temporary tags of this object. These will be stored as unmanaged tags.")]
        private List<GameplayTag> m_InitialTemporaryTags;


        internal List<GameplayTag> m_UnmanagedTags = new List<GameplayTag>();
        internal List<GameplayTag> m_ManagedTags = new List<GameplayTag>();


        private void Awake()
        {
            m_ManagedTags.AddRange(m_InitialPermanentTags);
            m_UnmanagedTags.AddRange(m_InitialTemporaryTags);
        }


        /// <summary>
        /// Add a managed tag.
        /// </summary>
        /// <remarks>Managed tags are managed by someone and should not be removed by anyone else.</remarks>
        public void AddManaged(GameplayTag tag)
        {
            m_ManagedTags.Add(tag);
        }
        /// <summary>
        /// Remove a managed tag.
        /// </summary>
        /// <remarks>Managed tags are managed by someone and should not be removed by anyone else.</remarks>
        public bool RemoveManaged(GameplayTag tag)
        {
            return m_ManagedTags.Remove(tag);
        }
        /// <summary>
        /// Add multiple managed tags.
        /// </summary>
        /// <remarks>Managed tags are managed by someone and should not be removed by anyone else.</remarks>
        public void AddManagedRange(IList<GameplayTag> tags)
        {
            m_ManagedTags.AddRange(tags);
        }
        /// <summary>
        /// Remove multiple managed tags.
        /// </summary>
        /// <remarks>Managed tags are managed by someone and should not be removed by anyone else.</remarks>
        public void RemoveManagedRange(IList<GameplayTag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                m_ManagedTags.Remove(tags[i]);
            }
        }

        /// <summary>
        /// Add an unmanaged tag.
        /// </summary>
        /// <remarks>Unmanaged tags are not managed by anyone and can be removed at will.</remarks>
        public void AddUnmanaged(GameplayTag tag)
        {
            m_UnmanagedTags.Add(tag);
        }
        /// <summary>
        /// Remove an unmanaged tag.
        /// </summary>
        /// <remarks>Unmanaged tags are not managed by anyone and can be removed at will.</remarks>
        public bool RemoveUnmanaged(GameplayTag tag)
        {
            return m_UnmanagedTags.Remove(tag);
        }
        /// <summary>
        /// Add multiple unmanaged tags.
        /// </summary>
        /// <remarks>Unmanaged tags are not managed by anyone and can be removed at will.</remarks>
        public void AddUnmanagedRange(List<GameplayTag> tags)
        {
            m_UnmanagedTags.AddRange(tags);
        }
        /// <summary>
        /// Remove multiple unmanaged tags.
        /// </summary>
        /// <remarks>Unmanaged tags are not managed by anyone and can be removed at will.</remarks>
        public void RemoveUnmanagedRange(List<GameplayTag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                m_UnmanagedTags.Remove(tags[i]);
            }
        }

        /// <summary>
        /// Get all tags this owner currently holds.
        /// </summary>
        /// <remarks>Any duplicate tags are stripped.</remarks>
        public List<GameplayTag> GetAllTags()
        {
            var tags = new List<GameplayTag>();
            var tagHashes = new HashSet<GameplayTag>();

            if (m_UnmanagedTags != null)
            {
                for (int i = 0; i < m_UnmanagedTags.Count; i++)
                {
                    if (!tagHashes.Contains(m_UnmanagedTags[i]))
                    {
                        tags.Add(m_UnmanagedTags[i]);
                        tagHashes.Add(m_UnmanagedTags[i]);
                    }
                }
            }
            if (m_ManagedTags != null)
            {
                for (int i = 0; i < m_ManagedTags.Count; i++)
                {
                    if (!tagHashes.Contains(m_ManagedTags[i]))
                    {
                        tags.Add(m_ManagedTags[i]);
                        tagHashes.Add(m_ManagedTags[i]);
                    }
                }
            }

            return tags;
        }
        /// <summary>
        /// Get all managed tags
        /// </summary>
        /// <remarks>Managed tags are managed by someone and should not be removed by anyone else.</remarks>
        public List<GameplayTag> GetAllManagedTags()
        {
            return new List<GameplayTag>(m_ManagedTags);
        }
        /// <summary>
        /// Get all unmanaged tags
        /// </summary>
        /// <remarks>Unmanaged tags are not managed by anyone and can be removed at will.</remarks>
        public List<GameplayTag> GetAllUnmanagedTags()
        {
            return new List<GameplayTag>(m_UnmanagedTags);
        }


        /// <summary>
        /// Checks if a GameplayTag is explicitly or implicitly present on the entity.
        /// </summary>
        /// <param name="tag">The Tag to check for.</param>
        /// <returns>True if the specified Tag or any of its parents is present.</returns>
        public bool HasTag(GameplayTag tag)
        {
            return GameplayTagUtility.HasTag(m_UnmanagedTags, tag) || GameplayTagUtility.HasTag(m_ManagedTags, tag);
        }
        public bool HasExplicitTag(GameplayTag tag)
        {
            return HasExplicitTag(m_UnmanagedTags, tag) || HasExplicitTag(m_ManagedTags, tag);
        }


        /// <summary>
        /// Checks if <b>all</b> GameplayTags are present on the entity.
        /// </summary>
        /// <param name="tags">The Tags to check for.</param>
        /// <returns>True if <b>all</b> Tags are present.</returns>
        public bool HasAllTags(List<GameplayTag> tags, bool emptyResponse = true)
        {
            List<GameplayTag> mergedTags = new List<GameplayTag>(m_UnmanagedTags);
            mergedTags.AddRange(m_ManagedTags);
            return GameplayTagUtility.HasAllTags(mergedTags, tags, emptyResponse);
        }
        /// <summary>
        /// Checks if <b>none</b> GameplayTags are present on the entity.
        /// </summary>
        /// <param name="tags">The Tags to check for.</param>
        /// <returns>True if <b>none</b> of the Tags are present</returns>
        public bool HasNoneTags(List<GameplayTag> tags, bool emptyResponse = true)
        {
            List<GameplayTag> mergedTags = new List<GameplayTag>(m_UnmanagedTags);
            mergedTags.AddRange(m_ManagedTags);
            return GameplayTagUtility.HasNoneTags(mergedTags, tags, emptyResponse);
        }
        /// <summary>
        /// Chechs if <b>all</b> of the required filter tags and <b>none</b> of the ignore filter tags are present in the collection.
        /// </summary>
        /// <param name="tagFilter">The filter to check for</param>
        /// <param name="emptyResponse">What to return if the filter is empty</param>
        /// <returns>True if the tag collection passed the filter</returns>
        public bool SatisfiesTagFilter(GameplayTagFilter filter, bool emptyResponse = true)
        {
            List<GameplayTag> mergedTags = new List<GameplayTag>(m_UnmanagedTags);
            mergedTags.AddRange(m_ManagedTags);
            return GameplayTagUtility.SatisfiesTagFilter(mergedTags, filter, emptyResponse);
        }


        public bool HasExplicitTag(List<GameplayTag> tagList, GameplayTag tag) => GameplayTagUtility.HasExplicitTag(tagList, tag);
    }
}
