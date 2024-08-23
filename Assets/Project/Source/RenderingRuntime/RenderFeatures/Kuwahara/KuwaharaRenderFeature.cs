using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//TODO add shader via buildsystem
public class KuwaharaRenderFeature : ScriptableRendererFeature
{
    public BlurSettings m_PreBlurSettings;
    public KuwaharaSettings m_KuwaharaSettings;
    public BlurSettings m_PostBlurSettings;
    public CompositeSettings m_CompositeSettings;

    private Material m_KuwaharaMaterial;
    private KuwaharaRenderPass m_RenderPass;


    [Serializable]
    public class KuwaharaSettings
    {
        [Range(1, 16)]
        public int Radius;
        [Range(0, 1f)]
        public float Spread;
        [Range(0, 360)]
        public float SampleRotation;
        public bool Directional;
        [Range(0, 4)]
        public int Downscale;
    }
    [Serializable]
    public class BlurSettings
    {
        public bool Enabled;
        [Range(1, 16)]
        public int Iterations;
        [Range(0, 1f)]
        public float Spread;
    }
    [Serializable]
    public class CompositeSettings
    {
        [Range(0, 1)]
        public float Blend;
        [Range(0, 1f)]
        public float SharpenSpread;
    }
    

    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif

        if (m_KuwaharaMaterial == null)
        {
            m_KuwaharaMaterial = CoreUtils.CreateEngineMaterial("Hidden/Kuwahara/Kuwahara");
        }
        if (m_RenderPass == null)
        {
            m_RenderPass = new KuwaharaRenderPass();
        }
    }
    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        m_RenderPass = null;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_RenderPass.Setup(m_KuwaharaMaterial, m_KuwaharaSettings, m_PreBlurSettings, m_PostBlurSettings, m_CompositeSettings);
        renderer.EnqueuePass(m_RenderPass);
    }
}