using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Experimental.Rendering.RenderGraphModule.IBaseRenderGraphBuilder;

public class CanopyShadowsRenderPass : ScriptableRenderPass
{
    private RTHandle m_LastRT;
    private RTHandle m_CurrentRT;

    private Material m_CanopyShadowsMaterial;


    public CanopyShadowsRenderPass()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(Material CanopyShadowsMaterial, float mix)
    {
        m_CanopyShadowsMaterial = CanopyShadowsMaterial;

        m_CanopyShadowsMaterial.SetFloat("_Mix", mix);
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (!VolumeManager.instance.stack.isValid)
            return;
        CanopyShadowsComponent CanopyShadows = VolumeManager.instance.stack.GetComponent<CanopyShadowsComponent>();
        if (!CanopyShadows || !CanopyShadows.active)
            return;

        RenderTextureDescriptor CanopyShadowsDesc = renderingData.cameraData.cameraTargetDescriptor;
        CanopyShadowsDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_LastRT, CanopyShadowsDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_LastRT");
        RenderingUtils.ReAllocateIfNeeded(ref m_CurrentRT, CanopyShadowsDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CurrentRT");
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera)
            return;
        if (renderingData.cameraData.isPreviewCamera)
            return;
        if (!VolumeManager.instance.stack.isValid)
            return;

        CommandBuffer cmd = CommandBufferPool.Get("CanopyShadows");

        RTHandle cameraColorRt = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraColorRt, m_CurrentRT);

        cmd.SetGlobalTexture(Shader.PropertyToID("_LastTex"), m_LastRT);


        cmd.Blit(m_CurrentRT, cameraColorRt, m_CanopyShadowsMaterial, 0);

        cmd.Blit(cameraColorRt, m_LastRT);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_CanopyShadowsMaterial);

        m_LastRT?.Release();
        m_CurrentRT?.Release();
    }
}