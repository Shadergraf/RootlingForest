using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PainterlyRenderPass : ScriptableRenderPass
{
    private RTHandle m_StructureTensorRT;
    private RenderTextureDescriptor m_KuwaharaDesc;
    private RTHandle m_KuwaharaRT;
    private RTHandle m_EdgeFlowRT;

    private readonly Material m_StructureTensorMaterial;
    private readonly Material m_KuwaharaMaterial;
    private readonly Material m_LineConvolutionMaterial;
    private readonly Material m_CompositeMaterial;
    
    private int m_KuwaharaFilterIterations = 1;

    private bool m_CompositorEnabled;

    public PainterlyRenderPass(Material structureTensorMaterial, Material kuwaharaMaterial, Material lineConvolutionMaterial, Material compositeMaterial)
    {
        m_StructureTensorMaterial = structureTensorMaterial;
        m_KuwaharaMaterial = kuwaharaMaterial;
        m_LineConvolutionMaterial = lineConvolutionMaterial;
        m_CompositeMaterial = compositeMaterial;
    }

    public void Setup(PainterlyRenderFeature.Settings settings)
    {
        SetupKuwaharaFilter(settings.anisotropicKuwaharaFilterSettings);
        SetupLineIntegralConvolution(settings.edgeFlowSettings);
        SetupCompositor(settings.compositorSettings);
    }
    private void SetupKuwaharaFilter(PainterlyRenderFeature.AnisotropicKuwaharaFilterSettings kuwaharaFilterSettings)
    {
        m_KuwaharaMaterial.SetInt("_FilterKernelSectors", kuwaharaFilterSettings.filterKernelSectors);
        m_KuwaharaMaterial.SetTexture("_FilterKernelTex", kuwaharaFilterSettings.filterKernelTexture);
        m_KuwaharaMaterial.SetFloat("_FilterRadius", kuwaharaFilterSettings.filterRadius);
        m_KuwaharaMaterial.SetFloat("_FilterSharpness", kuwaharaFilterSettings.filterSharpness);
        m_KuwaharaMaterial.SetFloat("_Eccentricity", kuwaharaFilterSettings.eccentricity);
        m_KuwaharaFilterIterations = kuwaharaFilterSettings.iterations;
    }
    private void SetupLineIntegralConvolution(PainterlyRenderFeature.EdgeFlowSettings edgeFlowSettings)
    {
        m_LineConvolutionMaterial.SetTexture("_NoiseTex", edgeFlowSettings.noiseTexture);
        m_LineConvolutionMaterial.SetInt("_StreamLineLength", edgeFlowSettings.streamLineLength);
        m_LineConvolutionMaterial.SetFloat("_StreamKernelStrength", edgeFlowSettings.streamKernelStrength);
    }
    private void SetupCompositor(PainterlyRenderFeature.CompositorSettings compositorSettings)
    {
        m_CompositeMaterial.SetFloat("_EdgeContribution", compositorSettings.edgeContribution);
        m_CompositeMaterial.SetFloat("_FlowContribution", compositorSettings.flowContribution);
        m_CompositeMaterial.SetFloat("_DepthContribution", compositorSettings.depthContribution);
        m_CompositeMaterial.SetFloat("_BumpPower", compositorSettings.bumpPower);
        m_CompositeMaterial.SetFloat("_BumpIntensity", compositorSettings.bumpIntensity);
        m_CompositorEnabled = compositorSettings.enableCompositor;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor structureTensorDesc = cameraTextureDescriptor;
        structureTensorDesc.depthBufferBits = 0;
        structureTensorDesc.colorFormat = RenderTextureFormat.ARGBFloat;
        RenderingUtils.ReAllocateIfNeeded(ref m_StructureTensorRT, structureTensorDesc);

        RenderTextureDescriptor kuwaharaDesc = cameraTextureDescriptor;
        kuwaharaDesc.depthBufferBits = 0;
        kuwaharaDesc.colorFormat = RenderTextureFormat.ARGBFloat;
        m_KuwaharaDesc = kuwaharaDesc;
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaRT, kuwaharaDesc);

        RenderTextureDescriptor edgeFlowDesc = cameraTextureDescriptor;
        edgeFlowDesc.depthBufferBits = 0;
        edgeFlowDesc.colorFormat = RenderTextureFormat.RFloat;
        RenderingUtils.ReAllocateIfNeeded(ref m_EdgeFlowRT, edgeFlowDesc);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Painterly");

        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

        cmd.Blit(cameraTargetHandle, m_StructureTensorRT, m_StructureTensorMaterial, -1);

        m_KuwaharaMaterial.SetTexture("_StructureTensorTex", m_StructureTensorRT);
        cmd.Blit(cameraTargetHandle, m_KuwaharaRT, m_KuwaharaMaterial, -1);

        if (m_KuwaharaFilterIterations > 1)
        {
            int temporaryRT = Shader.PropertyToID("_TemporaryRT");
            cmd.GetTemporaryRT(temporaryRT, m_KuwaharaDesc);
            for (int i = 0; i < m_KuwaharaFilterIterations - 1; i++)
            {
                cmd.Blit(m_KuwaharaRT, temporaryRT, m_KuwaharaMaterial, -1);
                cmd.Blit(temporaryRT, m_KuwaharaRT, m_KuwaharaMaterial, -1);
            }
            cmd.ReleaseTemporaryRT(temporaryRT);
        }

        if (m_CompositorEnabled)
        {
            cmd.Blit(m_StructureTensorRT, m_EdgeFlowRT, m_LineConvolutionMaterial, -1);

            m_CompositeMaterial.SetTexture("_EdgeFlowTex", m_EdgeFlowRT);
            cmd.Blit(m_KuwaharaRT, cameraTargetHandle, m_CompositeMaterial, -1);
        }
        else
        {
            cmd.Blit(m_KuwaharaRT, cameraTargetHandle);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Object.Destroy(m_StructureTensorMaterial);
        }
        else
        {
            Object.DestroyImmediate(m_StructureTensorMaterial);
        }
#else
            Object.Destroy(m_StructureTensorMaterial);
#endif

        m_StructureTensorRT?.Release();
        m_KuwaharaRT?.Release();
        m_EdgeFlowRT?.Release();
    }
}