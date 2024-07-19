using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class SoftShadowsRenderFeature : ScriptableRendererFeature
{
    public RayTracingShader m_Shader;

    private SoftShadowsRenderPass m_RenderPass;

    

    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif

        if (m_RenderPass == null)
        {
            m_RenderPass = new SoftShadowsRenderPass(m_Shader);
        }
    }
    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        m_RenderPass = null;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_RenderPass.Setup();
        renderer.EnqueuePass(m_RenderPass);
    }
}