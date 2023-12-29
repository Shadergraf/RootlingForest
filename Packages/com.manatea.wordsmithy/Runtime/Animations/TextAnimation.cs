using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Manatea.WordSmithy.Animations
{
    public abstract class TextAnimation : ScriptableObject
    {
        protected const string CreateAssetPath = "Manatea/Word Smithy/Animations/";

        /// <summary>
        /// Animate a single character of a TMP text mesh
        /// </summary>
        /// <param name="charInfo">The TMP character info for the currently animated character.</param>
        /// <param name="data">Animation data for the whole text provided by the <see cref="TextAnimator"/> component.</param>
        /// <param name="meshInfo">The mesh info of the currently animated text mesh.</param>
        /// <param name="extraData">Extra character data coming from the <see cref="TextAnimator"/> component.</param>
        /// <returns>True if the texmesh was in any way modified by this function.</returns>
        /// <remarks>This is a hot code path as it modifies the texmesh once for every active animation. Optimize this function as much as possible!</remarks>
        public abstract bool Animate(ref TMP_CharacterInfo charInfo, ref TextAnimationData data, ref TMP_MeshInfo meshInfo, ref CharacterExtraData extraData);
    }
}
