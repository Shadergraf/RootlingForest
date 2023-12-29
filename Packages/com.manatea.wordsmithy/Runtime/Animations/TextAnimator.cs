using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Manatea.WordSmithy.Animations
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [DefaultExecutionOrder(1)]
    public class TextAnimator : MonoBehaviour
    {
        [SerializeField]
        protected List<TextAnimation> m_ConstantAnimations;

        public List<TextAnimation> ConstantAnimations => m_ConstantAnimations;
        public List<TextAnimatonSegment> AnimationSegments
        { get; private set; } = new List<TextAnimatonSegment>();

        public bool IsAnimated => m_ConstantAnimations.Count > 0 || AnimationSegments.Count > 0;

        private TextMeshProUGUI m_CachedTextMesh;

        private CharacterExtraData[] m_ExtraData = new CharacterExtraData[0];


        private TMP_TextInfo m_TextInfo;
        private int m_CharCount;
        private TMP_MeshInfo[] m_CachedMeshInfo;

        private bool m_IsCacheClean;
        private bool m_IsMeshCurrentlyAnimated;


        protected virtual void Awake()
        {
            m_CachedTextMesh = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            RecalculateMesh();
            m_IsMeshCurrentlyAnimated = false;
        }

        private void LateUpdate()
        {
            if (m_CachedTextMesh.havePropertiesChanged)
                RecalculateMesh();

            UpdateTextMesh();
        }


        private void RecalculateMesh()
        {
            // TODO this gets caled in certain intervals depending on the WordSmith text speed.
            //      Investigate why this is called so often when typewriting a text.

            UnityEngine.Profiling.Profiler.BeginSample("Recalculate text mesh");

            // TODO which of the following methods do we really need here?
            m_CachedTextMesh.ForceMeshUpdate();
            m_TextInfo = m_CachedTextMesh.textInfo;
            m_CharCount = m_TextInfo.characterCount;

            TMP_MeshInfo[] copiedMeshInfo = m_TextInfo.CopyMeshInfoVertexData();
            if (m_CachedMeshInfo == null)
                m_CachedMeshInfo = new TMP_MeshInfo[copiedMeshInfo.Length];
            else
                Array.Resize(ref m_CachedMeshInfo, copiedMeshInfo.Length);

            for (int i = 0; i < copiedMeshInfo.Length; i++)
                CopyMeshInfo(ref copiedMeshInfo[i], ref m_CachedMeshInfo[i], true);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void UpdateTextMesh()
        {
            // Reset mesh to cached version
            UnityEngine.Profiling.Profiler.BeginSample("Reset text mesh");
            if (IsAnimated || !m_IsCacheClean)
            {
                for (int i = 0; i < m_CachedMeshInfo.Length; i++)
                    CopyMeshInfo(ref m_CachedMeshInfo[i], ref m_TextInfo.meshInfo[i], false);
                m_IsCacheClean = true;
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (!IsAnimated && m_IsCacheClean && !m_IsMeshCurrentlyAnimated)
                return;
            if (m_TextInfo.characterCount == 0)
                return;

            // Set up extra text data
            UnityEngine.Profiling.Profiler.BeginSample("Init character data");
            if (m_ExtraData.Length != m_CharCount)
                Array.Resize(ref m_ExtraData, m_CharCount);
            float cachedDeltaTime = Time.deltaTime;
            for (int i = 0; i < m_ExtraData.Length; i++)
            {
                if (m_ExtraData[i].IsVisible != m_TextInfo.characterInfo[i].isVisible)
                    m_ExtraData[i].TimeSinceVisibilityChange = 0;
                m_ExtraData[i].Index = i;
                m_ExtraData[i].IsVisible = m_TextInfo.characterInfo[i].isVisible;
                m_ExtraData[i].TimeSinceVisibilityChange += cachedDeltaTime;
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Setup animation data
            TextAnimationData data;
            data.time = Time.time;

            // Iterate through characters and apply animation.
            UnityEngine.Profiling.Profiler.BeginSample("Animate text characters");
            bool meshAnimated = false;
            for (int i = 0; i < m_CharCount; i++)
            {
                if (!m_TextInfo.characterInfo[i].isVisible)
                    continue;

                int materialIndex = m_TextInfo.characterInfo[i].materialReferenceIndex;

                for (int j = 0; j < m_ConstantAnimations.Count; j++)
                    meshAnimated |= m_ConstantAnimations[j].Animate(ref m_TextInfo.characterInfo[i], ref data, ref m_TextInfo.meshInfo[materialIndex], ref m_ExtraData[i]);

                for (int j = 0; j < AnimationSegments.Count; j++)
                    if (i >= AnimationSegments[j].Start && (AnimationSegments[j].Length < 0 || i < AnimationSegments[j].Start + AnimationSegments[j].Length))
                        meshAnimated |= AnimationSegments[j].Animation.Animate(ref m_TextInfo.characterInfo[i], ref data, ref m_TextInfo.meshInfo[materialIndex], ref m_ExtraData[i]);
            }
            if (meshAnimated)
                m_IsCacheClean = false;
            UnityEngine.Profiling.Profiler.EndSample();

            // Apply modified mesh data
            UnityEngine.Profiling.Profiler.BeginSample("Update Text Mesh");
            if (meshAnimated || (!meshAnimated && m_IsMeshCurrentlyAnimated))
            {
                m_CachedTextMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
                m_IsMeshCurrentlyAnimated = meshAnimated;
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }


        private void CopyMeshInfo(ref TMP_MeshInfo from, ref TMP_MeshInfo to, bool alsoInstanciate = false)
        {
            if (from.vertices != null)
            {
                Array.Resize(ref to.vertices, from.vertices.Length);
                from.vertices.CopyTo(to.vertices, 0);
            }

            if (from.normals != null)
            {
                Array.Resize(ref to.normals, from.normals.Length);
                from.normals.CopyTo(to.normals, 0);
            }

            if (from.tangents != null)
            {
                Array.Resize(ref to.tangents, from.tangents.Length);
                from.tangents.CopyTo(to.tangents, 0);
            }

            if (from.uvs0 != null)
            {
                Array.Resize(ref to.uvs0, from.uvs0.Length);
                from.uvs0.CopyTo(to.uvs0, 0);
            }

            if (from.uvs2 != null)
            {
                Array.Resize(ref to.uvs2, from.uvs2.Length);
                from.uvs2.CopyTo(to.uvs2, 0);
            }

            if (from.colors32 != null)
            {
                Array.Resize(ref to.colors32, from.colors32.Length);
                from.colors32.CopyTo(to.colors32, 0);
            }

            if (from.triangles != null)
            {
                Array.Resize(ref to.triangles, from.triangles.Length);
                from.triangles.CopyTo(to.triangles, 0);
            }

            if (alsoInstanciate)
            {
                if (from.mesh != null)
                    to.mesh = Instantiate(from.mesh);
                if (from.material != null)
                    to.material = Instantiate(from.material);
            }
            to.vertexCount = from.vertexCount;
        }
    }

    [Serializable]
    public struct CharacterExtraData
    {
        public int Index;
        public bool IsVisible;
        public float TimeSinceVisibilityChange;
    }

    public struct TextAnimationData
    {
        public float time;
    }

    [Serializable]
    public struct TextAnimatonSegment
    {
        public int Start;
        public int Length;
        public TextAnimation Animation;
    }
}
