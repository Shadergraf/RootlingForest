using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//TODO add shader via buildsystem
public class CanopyShadowsRenderFeature : ScriptableRendererFeature
{
    [Range(0, 1)]
    public float Mix;

    private Material m_CanopyShadowsMaterial;
    private CanopyShadowsRenderPass m_RenderPass;


    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif

        if (m_CanopyShadowsMaterial == null)
        {
            m_CanopyShadowsMaterial = CoreUtils.CreateEngineMaterial("Hidden/CanopyShadows/CanopyShadows");
        }
        if (m_RenderPass == null)
        {
            m_RenderPass = new CanopyShadowsRenderPass();
        }
    }
    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        m_RenderPass = null;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_RenderPass.Setup(m_CanopyShadowsMaterial, Mix);
        renderer.EnqueuePass(m_RenderPass);
    }
}