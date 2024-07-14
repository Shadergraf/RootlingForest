using Manatea;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KuwaharaDirectionalRenderPass : ScriptableRenderPass
{
    private RTHandle m_KuwaharaDirectionalRT;
    private RTHandle m_BlurRT0;
    private RTHandle m_BlurRT1;
    private RTHandle m_CamMirrorRT;

    private readonly Material m_KuwaharaDirectionalMaterial;
    private const string k_KeywordDirectional = "_KUWAHARA_DIRECTIONAL";

    private int m_Downscale = 0;
    private KuwaharaDirectionalRenderFeature.KuwaharaBlurSettings m_PreBlurSettings;
    private KuwaharaDirectionalRenderFeature.KuwaharaBlurSettings m_PostBlurSettings;

    public KuwaharaDirectionalRenderPass(Material KuwaharaDirectionalMaterial)
    {
        m_KuwaharaDirectionalMaterial = KuwaharaDirectionalMaterial;

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(KuwaharaDirectionalRenderFeature.KuwaharaDirectionalSettings settings)
    {
        CoreUtils.SetKeyword(m_KuwaharaDirectionalMaterial, k_KeywordDirectional, settings.Kuwahara.Directional);
        m_KuwaharaDirectionalMaterial.SetInt("_Radius", settings.Kuwahara.Radius);
        m_KuwaharaDirectionalMaterial.SetFloat("_Spread", settings.Kuwahara.Spread);
        m_KuwaharaDirectionalMaterial.SetFloat("_SampleRotation", settings.Kuwahara.SampleRotation * MMath.Deg2Rad);
        m_Downscale = settings.Kuwahara.Downscale;

        m_KuwaharaDirectionalMaterial.SetFloat("_Blend", settings.Composite.Blend);
        m_KuwaharaDirectionalMaterial.SetFloat("_SharpenSpread", settings.Composite.SharpenSpread);

        m_PreBlurSettings = settings.PreBlur;
        m_PostBlurSettings = settings.PostBlur;
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor kuwaharaDesc = renderingData.cameraData.cameraTargetDescriptor;
        kuwaharaDesc.depthBufferBits = 0;
        kuwaharaDesc.width = MMath.RoundToInt(kuwaharaDesc.width / MMath.Pow(2, m_Downscale));
        kuwaharaDesc.height = MMath.RoundToInt(kuwaharaDesc.height / MMath.Pow(2, m_Downscale));
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaDirectionalRT, kuwaharaDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaDirectionalTexture");

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
        CommandBuffer cmd = CommandBufferPool.Get("KuwaharaDirectional");

        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraTargetHandle, m_CamMirrorRT);

        if (m_PreBlurSettings.Enabled)
        {
            cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PreBlurSettings.Iterations);
            cmd.SetGlobalFloat(Shader.PropertyToID("_BlurSpread"), m_PreBlurSettings.Spread / 2);
            cmd.Blit(cameraTargetHandle, m_BlurRT0, m_KuwaharaDirectionalMaterial, 1);
            cmd.Blit(m_BlurRT0, m_BlurRT1, m_KuwaharaDirectionalMaterial, 2);
        }
        else
        {
            cmd.Blit(cameraTargetHandle, m_BlurRT1);
        }

        //cmd.Blit(cameraTargetHandle, cameraTargetHandle, m_KuwaharaDirectionalMaterial, -1);
        cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PreBlurSettings.Iterations);
        cmd.Blit(m_BlurRT1, m_KuwaharaDirectionalRT, m_KuwaharaDirectionalMaterial, 0);

        if (m_PostBlurSettings.Enabled)
        {
            cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PostBlurSettings.Iterations);
            cmd.SetGlobalFloat(Shader.PropertyToID("_BlurSpread"), m_PostBlurSettings.Spread / 2);
            cmd.Blit(m_KuwaharaDirectionalRT, m_BlurRT0, m_KuwaharaDirectionalMaterial, 1);
            cmd.Blit(m_BlurRT0, m_KuwaharaDirectionalRT, m_KuwaharaDirectionalMaterial, 2);
        }

        cmd.SetGlobalTexture(Shader.PropertyToID("_EffectTex"), m_KuwaharaDirectionalRT);
        cmd.Blit(m_CamMirrorRT, cameraTargetHandle, m_KuwaharaDirectionalMaterial, 3);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_KuwaharaDirectionalMaterial);

        m_KuwaharaDirectionalRT?.Release();
        m_BlurRT0?.Release();
        m_BlurRT1?.Release();
    }
}