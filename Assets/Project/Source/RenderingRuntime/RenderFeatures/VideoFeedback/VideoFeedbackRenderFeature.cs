using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//TODO add shader via buildsystem
public class VideoFeedbackRenderFeature : ScriptableRendererFeature
{
    [Range(0, 1)]
    public float Mix;

    private Material m_VideoFeedbackMaterial;
    private VideoFeedbackRenderPass m_RenderPass;


    public override void Create()
    {
#if UNITY_EDITOR
        ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif

        if (m_VideoFeedbackMaterial == null)
        {
            m_VideoFeedbackMaterial = CoreUtils.CreateEngineMaterial("Hidden/VideoFeedback/VideoFeedback");
        }
        if (m_RenderPass == null)
        {
            m_RenderPass = new VideoFeedbackRenderPass();
        }
    }
    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        m_RenderPass = null;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_RenderPass.Setup(m_VideoFeedbackMaterial, Mix);
        renderer.EnqueuePass(m_RenderPass);
    }
}