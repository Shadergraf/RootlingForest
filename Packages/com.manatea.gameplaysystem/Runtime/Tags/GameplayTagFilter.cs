using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manatea.GameplaySystem
{
    [Serializable]
    public class GameplayTagFilter : ICloneable
    {
        /// <summary>
        /// All of these tags must be present
        /// </summary>
        public List<GameplayTag> RequireTags;

        /// <summary>
        /// None of these tags can be present
        /// </summary>
        public List<GameplayTag> IgnoreTags;

        /// <summary>
        /// Returns true if both Require and Ignore tag arrays are empty
        /// </summary>
        public bool IsEmpty =>
            (RequireTags == null || RequireTags.Count == 0) &&
            (IgnoreTags == null || IgnoreTags.Count == 0);

        public object Clone()
        {
            return new GameplayTagFilter()
            {
                RequireTags = new List<GameplayTag>(RequireTags),
                IgnoreTags = new List<GameplayTag>(IgnoreTags),
            };
        }
    }
}
