using Manatea;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KuwaharaRenderPass : ScriptableRenderPass
{
    private RTHandle m_KuwaharaRT;
    private RTHandle m_BlurRT0;
    private RTHandle m_BlurRT1;
    private RTHandle m_CamMirrorRT;

    private readonly Material m_KuwaharaMaterial;
    private const string k_KeywordDirectional = "_KUWAHARA_DIRECTIONAL";

    private int m_Downscale = 0;
    private KuwaharaRenderFeature.BlurSettings m_PreBlurSettings;
    private KuwaharaRenderFeature.BlurSettings m_PostBlurSettings;

    public KuwaharaRenderPass(Material KuwaharaMaterial)
    {
        m_KuwaharaMaterial = KuwaharaMaterial;

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(ref KuwaharaRenderFeature.KuwaharaSettings kuwaharaSettings, ref KuwaharaRenderFeature.BlurSettings preBlurSettings, ref KuwaharaRenderFeature.BlurSettings postBlurSettings, ref KuwaharaRenderFeature.CompositeSettings compositeSettings)
    {
        CoreUtils.SetKeyword(m_KuwaharaMaterial, k_KeywordDirectional, kuwaharaSettings.Directional);
        m_KuwaharaMaterial.SetInt("_Radius", kuwaharaSettings.Radius);
        m_KuwaharaMaterial.SetFloat("_Spread", kuwaharaSettings.Spread / 500);
        m_KuwaharaMaterial.SetFloat("_SampleRotation", kuwaharaSettings.SampleRotation * MMath.Deg2Rad);
        m_Downscale = kuwaharaSettings.Downscale;

        m_KuwaharaMaterial.SetFloat("_Blend", compositeSettings.Blend);
        m_KuwaharaMaterial.SetFloat("_SharpenSpread", compositeSettings.SharpenSpread / 500);

        m_PreBlurSettings = preBlurSettings;
        m_PostBlurSettings = postBlurSettings;
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor kuwaharaDesc = renderingData.cameraData.cameraTargetDescriptor;
        kuwaharaDesc.depthBufferBits = 0;
        kuwaharaDesc.width = MMath.RoundToInt(kuwaharaDesc.width / MMath.Pow(2, m_Downscale));
        kuwaharaDesc.height = MMath.RoundToInt(kuwaharaDesc.height / MMath.Pow(2, m_Downscale));
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaRT, kuwaharaDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaDirectionalTexture");

        RenderTextureDescriptor blurDesc = renderingData.cameraData.cameraTargetDescriptor;
        blurDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_BlurRT0, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurTexture0");
        RenderingUtils.ReAllocateIfNeeded(ref m_BlurRT1, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurTexture1");

        RenderTextureDescriptor camDesc = renderingData.cameraData.cameraTargetDescriptor;
        camDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_CamMirrorRT, camDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CamMirrorRT");
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Kuwahara");

        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraTargetHandle, m_CamMirrorRT);

        if (m_PreBlurSettings.Enabled)
        {
            cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PreBlurSettings.Iterations);
            cmd.SetGlobalFloat(Shader.PropertyToID("_BlurSpread"), m_PreBlurSettings.Spread / 500);
            cmd.Blit(cameraTargetHandle, m_BlurRT0, m_KuwaharaMaterial, 1);
            cmd.Blit(m_BlurRT0, m_BlurRT1, m_KuwaharaMaterial, 2);
        }
        else
        {
            cmd.Blit(cameraTargetHandle, m_BlurRT1);
        }

        cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PreBlurSettings.Iterations);
        cmd.Blit(m_BlurRT1, m_KuwaharaRT, m_KuwaharaMaterial, 0);

        if (m_PostBlurSettings.Enabled)
        {
            cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PostBlurSettings.Iterations);
            cmd.SetGlobalFloat(Shader.PropertyToID("_BlurSpread"), m_PostBlurSettings.Spread / 500);
            cmd.Blit(m_KuwaharaRT, m_BlurRT0, m_KuwaharaMaterial, 1);
            cmd.Blit(m_BlurRT0, m_KuwaharaRT, m_KuwaharaMaterial, 2);
        }

        cmd.SetGlobalTexture(Shader.PropertyToID("_EffectTex"), m_KuwaharaRT);
        cmd.Blit(m_CamMirrorRT, cameraTargetHandle, m_KuwaharaMaterial, 3);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_KuwaharaMaterial);

        m_KuwaharaRT?.Release();
        m_BlurRT0?.Release();
        m_BlurRT1?.Release();
        m_CamMirrorRT?.Release();
    }
}