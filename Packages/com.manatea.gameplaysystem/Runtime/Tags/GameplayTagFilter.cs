using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [Serializable]
    public class GameplayTagFilter : ICloneable
    {
        /// <summary>
        /// All of these tags must be present
        /// </summary>
        [Tooltip("All of these tags must be present")]
        public List<GameplayTag> RequireTags;

        /// <summary>
        /// None of these tags can be present
        /// </summary>
        [Tooltip("None of these tags can be present")]
        public List<GameplayTag> IgnoreTags;

        /// <summary>
        /// At least one of these tags should be present
        /// </summary>
        [Tooltip("At least one of these tags should be present")]
        public List<GameplayTag> AnyTags;

        /// <summary>
        /// Returns true if both Require and Ignore tag arrays are empty
        /// </summary>
        public bool IsEmpty =>
            (RequireTags == null || RequireTags.Count == 0) &&
            (IgnoreTags == null || IgnoreTags.Count == 0) &&
            (AnyTags == null || AnyTags.Count == 0);

        public object Clone()
        {
            return new GameplayTagFilter()
            {
                RequireTags = new List<GameplayTag>(RequireTags),
                IgnoreTags = new List<GameplayTag>(IgnoreTags),
                AnyTags = new List<GameplayTag>(AnyTags),
            };
        }

        public bool Satisfies(List<GameplayTag> tagList, bool emptyResponse = true)
        {
            if (IsEmpty)
                return emptyResponse;

            return GameplayTagUtility.HasAllTags(tagList, RequireTags) &&
                   GameplayTagUtility.HasNoneTags(tagList, IgnoreTags) &&
                   GameplayTagUtility.HasAnyTags(tagList, AnyTags);
        }
    }
}
