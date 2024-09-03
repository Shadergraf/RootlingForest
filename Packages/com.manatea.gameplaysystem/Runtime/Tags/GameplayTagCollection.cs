using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [Serializable]
    public class GameplayTagCollection : IList<GameplayTag>, ICollection<GameplayTag>, IEnumerable<GameplayTag>, IEnumerable, IList, ICollection, IReadOnlyList<GameplayTag>, IReadOnlyCollection<GameplayTag>
    {
        /// <summary>
        /// This sub-property is serialized by unity and contains the main list of GameplayTags
        /// </summary>
        [SerializeField]
        private List<GameplayTag> m_List;


        public GameplayTagCollection()
        {
            m_List = new List<GameplayTag>();
        }
        public GameplayTagCollection(IEnumerable<GameplayTag> tags)
        {
            if (tags == null)
                m_List = new List<GameplayTag>();
            else
                m_List = tags.ToList();
        }

        public void AddRange(GameplayTagCollection items)
        {
            for (int i = 0; i < items.m_List.Count; i++)
            {
                Add(items.m_List[i]);
            }
        }
        public void RemoveRange(GameplayTagCollection items)
        {
            for (int i = 0; i < items.m_List.Count; i++)
            {
                Remove(items.m_List[i]);
            }
        }

        /// <summary>
        /// Checks if a GameplayTag is explicitly or implicitly present on the entity.
        /// </summary>
        /// <param name="tag">The Tag to check for.</param>
        /// <returns>True if the specified Tag or any of its parents is present.</returns>
        public bool HasTag(GameplayTag tag)
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                GameplayTag t = m_List[i];
                if (t == tag)
                    return true;
                if (!t)
                    continue;
                GameplayTag gt = t;
                while (gt.Parent)
                {
                    if (gt.Parent == tag)
                        return true;
                    gt = gt.Parent;
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if a GameplayTag is explicitly present on the entity.
        /// </summary>
        /// <param name="tag">The Tag to check for.</param>
        /// <returns>True if the specified Tag is present.</returns>
        public bool HasExplicitTag(GameplayTag tag) => m_List.Contains(tag);


        /// <summary>
        /// Checks if <b>all</b> GameplayTags are present on the entity.
        /// </summary>
        /// <param name="tags">The Tags to check for.</param>
        /// <returns>True if <b>all</b> Tags are present.</returns>
        public bool HasAllTags(GameplayTagCollection tags, bool emptyResponse = true)
        {
            if (tags == null || tags.Count == 0)
                return emptyResponse;

            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag t = tags[i];
                if (!t)
                    continue;
                if (!HasTag(t))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Checks if <b>none</b> GameplayTags are present on the entity.
        /// </summary>
        /// <param name="tags">The Tags to check for.</param>
        /// <returns>True if <b>none</b> of the Tags are present</returns>
        public bool HasNoneTags(GameplayTagCollection tags, bool emptyResponse = true)
        {
            if (tags == null || tags.Count == 0)
                return emptyResponse;

            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag t = tags[i];
                if (!t)
                    continue;
                if (HasTag(t))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Chechs if <b>all</b> of the required filter tags and <b>none</b> of the ignore filter tags are present in the collection.
        /// </summary>
        /// <param name="tagFilter">The filter to check for</param>
        /// <param name="emptyResponse">What to return if the filter is empty</param>
        /// <returns>True if the tag collection passed the filter</returns>
        public bool SatisfiesTagFilter(GameplayTagFilter container, bool emptyResponse = true)
        {
            if (container.IsEmpty)
                return emptyResponse;

            return HasAllTags(container.RequireTags) &&
                   HasNoneTags(container.IgnoreTags);
        }

        public GameplayTag this[int index] { get => m_List[index]; set => m_List[index] = value; }
        object IList.this[int index] { get => m_List[index]; set => m_List[index] = (GameplayTag)value; }
        public int Count => m_List.Count;
        public bool IsReadOnly => ((ICollection<GameplayTag>)m_List).IsReadOnly;
        public bool IsFixedSize => ((IList)m_List).IsFixedSize;
        public bool IsSynchronized => ((ICollection)m_List).IsSynchronized;
        public object SyncRoot => ((ICollection)m_List).SyncRoot;
        public void Add(GameplayTag item) => m_List.Add(item);
        public int Add(object value) => ((IList)m_List).Add(value);
        public void Clear() => m_List.Clear();
        public bool Contains(GameplayTag item) => m_List.Contains(item);
        public bool Contains(object value) => ((IList)m_List).Contains(value);
        public void CopyTo(GameplayTag[] array, int arrayIndex) => m_List.CopyTo(array, arrayIndex);
        public void CopyTo(Array array, int index) => ((ICollection)m_List).CopyTo(array, index);
        public IEnumerator<GameplayTag> GetEnumerator() => m_List.GetEnumerator();
        public int IndexOf(GameplayTag item) => m_List.IndexOf(item);
        public int IndexOf(object value) => ((IList)m_List).IndexOf(value);
        public void Insert(int index, GameplayTag item) => m_List.Insert(index, item);
        public void Insert(int index, object value) => ((IList)m_List).Insert(index, value);
        public bool Remove(GameplayTag item) => m_List.Remove(item);
        public void Remove(object value) => ((IList)m_List).Remove(value);
        public void RemoveAt(int index) => m_List.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => m_List.GetEnumerator();
    }
}