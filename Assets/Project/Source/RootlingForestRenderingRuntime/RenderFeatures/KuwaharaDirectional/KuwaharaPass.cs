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
    private RTHandle m_MaskRT;

    private Material m_KuwaharaMaterial;
    private const string k_KeywordDirectional = "_KUWAHARA_DIRECTIONAL";

    private int m_Downscale = 0;
    private KuwaharaRenderFeature.BlurSettings m_PreBlurSettings;
    private KuwaharaRenderFeature.BlurSettings m_PostBlurSettings;

    public KuwaharaRenderPass()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(Material kuwaharaMaterial, KuwaharaRenderFeature.KuwaharaSettings kuwaharaSettings, KuwaharaRenderFeature.BlurSettings preBlurSettings, KuwaharaRenderFeature.BlurSettings postBlurSettings, KuwaharaRenderFeature.CompositeSettings compositeSettings)
    {
        m_KuwaharaMaterial = kuwaharaMaterial;

        CoreUtils.SetKeyword(m_KuwaharaMaterial, k_KeywordDirectional, kuwaharaSettings.Directional);
        m_KuwaharaMaterial.SetInt("_Radius", kuwaharaSettings.Radius);
        m_KuwaharaMaterial.SetFloat("_Spread", kuwaharaSettings.Spread / 500);
        m_KuwaharaMaterial.SetFloat("_SampleRotation", kuwaharaSettings.SampleRotation * Mathf.Deg2Rad);
        m_Downscale = kuwaharaSettings.Downscale;

        m_KuwaharaMaterial.SetFloat("_Blend", compositeSettings.Blend);
        m_KuwaharaMaterial.SetFloat("_SharpenSpread", compositeSettings.SharpenSpread / 500);

        m_PreBlurSettings = preBlurSettings;
        m_PostBlurSettings = postBlurSettings;
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (!VolumeManager.instance.stack.isValid)
            return;
        KuwaharaComponent kuwahara = VolumeManager.instance.stack.GetComponent<KuwaharaComponent>();
        if (!kuwahara || !kuwahara.active)
            return;

        RenderTextureDescriptor kuwaharaDesc = renderingData.cameraData.cameraTargetDescriptor;
        kuwaharaDesc.depthBufferBits = 0;
        kuwaharaDesc.width = Mathf.RoundToInt(kuwaharaDesc.width / Mathf.Pow(2, m_Downscale));
        kuwaharaDesc.height = Mathf.RoundToInt(kuwaharaDesc.height / Mathf.Pow(2, m_Downscale));
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaRT, kuwaharaDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaDirectionalTexture");

        RenderTextureDescriptor blurDesc = renderingData.cameraData.cameraTargetDescriptor;
        blurDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_BlurRT0, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurTexture0");
        RenderingUtils.ReAllocateIfNeeded(ref m_BlurRT1, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurTexture1");

        RenderTextureDescriptor camDesc = renderingData.cameraData.cameraTargetDescriptor;
        camDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_CamMirrorRT, camDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CamMirrorRT");

        RenderTextureDescriptor maskDesc = renderingData.cameraData.cameraTargetDescriptor;
        maskDesc.depthBufferBits = 0;
        maskDesc.colorFormat = RenderTextureFormat.RHalf;
        RenderingUtils.ReAllocateIfNeeded(ref m_MaskRT, maskDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_MaskRT");

        var aspectRatio = renderingData.cameraData.cameraTargetDescriptor.width / (float)renderingData.cameraData.cameraTargetDescriptor.height;
        m_KuwaharaMaterial.SetVector(Shader.PropertyToID("_Vignette_Params"), new Vector4(kuwahara.vignetteIntensity.value, kuwahara.vignetteSmoothness.value, kuwahara.vignetteRounded.value ? aspectRatio : 1.0f));

        m_KuwaharaMaterial.SetVector(Shader.PropertyToID("_Focus_Params"), new Vector4(kuwahara.focusDistance.value, kuwahara.focusStart.value, kuwahara.focusEnd.value));
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera)
            return;
        if (!VolumeManager.instance.stack.isValid)
            return;
        KuwaharaComponent kuwahara = VolumeManager.instance.stack.GetComponent<KuwaharaComponent>();
        if (!kuwahara || !kuwahara.active)
            return;

        CommandBuffer cmd = CommandBufferPool.Get("Kuwahara");

        RTHandle cameraColorRt = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraColorRt, m_CamMirrorRT);

        RTHandle cameraDepthRT = renderingData.cameraData.renderer.cameraDepthTargetHandle;
        cmd.Blit(cameraDepthRT, m_MaskRT, m_KuwaharaMaterial, 4);

        cmd.SetGlobalTexture(Shader.PropertyToID("_MaskTex"), m_MaskRT);

        if (m_PreBlurSettings.Enabled)
        {
            cmd.SetGlobalInt(Shader.PropertyToID("_BlurIterations"), m_PreBlurSettings.Iterations);
            cmd.SetGlobalFloat(Shader.PropertyToID("_BlurSpread"), m_PreBlurSettings.Spread / 500);
            cmd.Blit(cameraColorRt, m_BlurRT0, m_KuwaharaMaterial, 1);
            cmd.Blit(m_BlurRT0, m_BlurRT1, m_KuwaharaMaterial, 2);
        }
        else
        {
            cmd.Blit(cameraColorRt, m_BlurRT1);
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
        cmd.Blit(m_CamMirrorRT, cameraColorRt, m_KuwaharaMaterial, 3);

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