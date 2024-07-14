using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class KuwaharaRenderFeature : ScriptableRendererFeature
{
    [Serializable]
    public class KuwaharaSettings
    {
        [Range(1, 32)]
        public int Radius = 4;
        [Range(0, 1)]
        public float Blend = 1;
    }


    public KuwaharaSettings m_Settings;
    
    private KuwaharaRenderPass m_RenderPass;
    


    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif

        var kuwaharaFilterMaterial = CoreUtils.CreateEngineMaterial("Hidden/Kuwahara/Kuwahara");
        m_RenderPass = new KuwaharaRenderPass(kuwaharaFilterMaterial);

        Debug.Log("KuwaharaRenderFeature created");
    }
    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        m_RenderPass = null;

        Debug.Log("KuwaharaRenderFeature disposed");
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_RenderPass.Setup(m_Settings);
        renderer.EnqueuePass(m_RenderPass);
    }
}