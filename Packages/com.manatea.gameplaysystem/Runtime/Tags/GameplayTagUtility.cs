using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manatea.GameplaySystem
{
    public static class GameplayTagUtility
    {
        public static bool HasExplicitTag(List<GameplayTag> tagList, GameplayTag tag) => tagList.Contains(tag);
        public static bool HasTag(List<GameplayTag> tagList, GameplayTag tag)
        {
            for (int i = 0; i < tagList.Count; i++)
            {
                GameplayTag t = tagList[i];
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
        public static bool HasAllTags(List<GameplayTag> tagList, List<GameplayTag> tagsToCheck, bool emptyResponse = true)
        {
            if (tagsToCheck.Count == 0)
                return emptyResponse;

            for (int i = 0; i < tagsToCheck.Count; i++)
            {
                GameplayTag t = tagsToCheck[i];
                if (!t)
                    continue;
                if (!HasTag(tagList, t))
                    return false;
            }
            return true;
        }
        public static bool HasNoneTags(List<GameplayTag> tagList, List<GameplayTag> tagsToCheck, bool emptyResponse = true)
        {
            if (tagsToCheck.Count == 0)
                return emptyResponse;

            for (int i = 0; i < tagsToCheck.Count; i++)
            {
                GameplayTag t = tagsToCheck[i];
                if (!t)
                    continue;
                if (HasTag(tagList, t))
                    return false;
            }
            return true;
        }
        public static bool HasAnyTags(List<GameplayTag> tagList, List<GameplayTag> tagsToCheck, bool emptyResponse = true)
        {
            if (tagsToCheck.Count == 0)
                return emptyResponse;

            for (int i = 0; i < tagsToCheck.Count; i++)
            {
                GameplayTag t = tagsToCheck[i];
                if (!t)
                    continue;
                if (HasTag(tagList, t))
                    return true;
            }
            return false;
        }

        public static bool SatisfiesTagFilter(List<GameplayTag> tagList, GameplayTagFilter container, bool emptyResponse = true)
        {
            if (container.IsEmpty)
                return emptyResponse;

            return container.Satisfies(tagList);
        }
    }
}
