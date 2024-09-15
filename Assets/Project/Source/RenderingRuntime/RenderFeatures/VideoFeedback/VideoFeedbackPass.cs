using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Experimental.Rendering.RenderGraphModule.IBaseRenderGraphBuilder;

public class VideoFeedbackRenderPass : ScriptableRenderPass
{
    private RTHandle m_LastRT;
    private RTHandle m_CurrentRT;

    private Material m_VideoFeedbackMaterial;


    public VideoFeedbackRenderPass()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(Material VideoFeedbackMaterial, float mix)
    {
        m_VideoFeedbackMaterial = VideoFeedbackMaterial;

        m_VideoFeedbackMaterial.SetFloat("_Mix", mix);
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (!VolumeManager.instance.stack.isValid)
            return;
        VideoFeedbackComponent VideoFeedback = VolumeManager.instance.stack.GetComponent<VideoFeedbackComponent>();
        if (!VideoFeedback || !VideoFeedback.active)
            return;

        RenderTextureDescriptor VideoFeedbackDesc = renderingData.cameraData.cameraTargetDescriptor;
        VideoFeedbackDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_LastRT, VideoFeedbackDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_LastRT");
        RenderingUtils.ReAllocateIfNeeded(ref m_CurrentRT, VideoFeedbackDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CurrentRT");
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera)
            return;
        if (renderingData.cameraData.isPreviewCamera)
            return;
        if (!VolumeManager.instance.stack.isValid)
            return;

        CommandBuffer cmd = CommandBufferPool.Get("VideoFeedback");

        RTHandle cameraColorRt = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraColorRt, m_CurrentRT);

        cmd.SetGlobalTexture(Shader.PropertyToID("_LastTex"), m_LastRT);


        cmd.Blit(m_CurrentRT, cameraColorRt, m_VideoFeedbackMaterial, 0);

        cmd.Blit(cameraColorRt, m_LastRT);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_VideoFeedbackMaterial);

        m_LastRT?.Release();
        m_CurrentRT?.Release();
    }
}