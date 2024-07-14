using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class KuwaharaDirectionalRenderFeature : ScriptableRendererFeature
{
    [Serializable]
    public class KuwaharaDirectionalSettings
    {
        public KuwaharaBlurSettings PreBlur;
        public KuwaharaSettings Kuwahara;
        public KuwaharaBlurSettings PostBlur;
        public KuwaharaCompositeSettings Composite;
    }
    [Serializable]
    public class KuwaharaSettings
    {
        [Range(1, 16)]
        public int Radius = 4;
        [Range(0, 0.01f)]
        public float Spread = 0.001f;
        [Range(0, 360)]
        public float SampleRotation = 0;
        public bool Directional = true;
        [Range(0, 4)]
        public int Downscale = 0;
    }
    [Serializable]
    public class KuwaharaBlurSettings
    {
        public bool Enabled = false;
        [Range(1, 16)]
        public int Iterations = 4;
        [Range(0, 0.01f)]
        public float Spread = 0;
    }
    [Serializable]
    public class KuwaharaCompositeSettings
    {
        [Range(0, 1)]
        public float Blend = 1;
        [Range(0, 0.01f)]
        public float SharpenSpread = 0;
    }


    public KuwaharaDirectionalSettings m_Settings;
    
    private KuwaharaDirectionalRenderPass m_RenderPass;
    


    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif

        var KuwaharaDirectionalFilterMaterial = CoreUtils.CreateEngineMaterial("Hidden/KuwaharaDirectional/KuwaharaDirectional");
        m_RenderPass = new KuwaharaDirectionalRenderPass(KuwaharaDirectionalFilterMaterial);

        Debug.Log("KuwaharaDirectionalRenderFeature created");
    }
    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        m_RenderPass = null;

        Debug.Log("KuwaharaDirectionalRenderFeature disposed");
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_RenderPass.Setup(m_Settings);
        renderer.EnqueuePass(m_RenderPass);
    }
}