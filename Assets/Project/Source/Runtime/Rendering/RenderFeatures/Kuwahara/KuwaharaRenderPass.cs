using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using static UnityEngine.XR.XRDisplaySubsystem;

public class KuwaharaRenderPass : ScriptableRenderPass
{
    private RTHandle m_KuwaharaRT;

    private readonly Material m_KuwaharaMaterial;


    public KuwaharaRenderPass(Material kuwaharaMaterial)
    {
        m_KuwaharaMaterial = kuwaharaMaterial;

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(KuwaharaRenderFeature.KuwaharaSettings settings)
    {
        m_KuwaharaMaterial.SetInt("_Radius", settings.Radius);
        m_KuwaharaMaterial.SetFloat("_Blend", settings.Blend);
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaRT, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaTexture");
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Kuwahara");

        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;


        //cmd.Blit(cameraTargetHandle, cameraTargetHandle, m_KuwaharaMaterial, -1);
        cmd.Blit(cameraTargetHandle, m_KuwaharaRT, m_KuwaharaMaterial, -1);
        cmd.Blit(m_KuwaharaRT, cameraTargetHandle);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_KuwaharaMaterial);
    }
}