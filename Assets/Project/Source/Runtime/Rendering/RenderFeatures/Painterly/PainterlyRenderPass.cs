using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class PainterlyRenderPass : ScriptableRenderPass
{
    private RTHandle m_StructureTensorRT_A;
    private RTHandle m_StructureTensorRT_B;
    private RenderTextureDescriptor m_KuwaharaDesc;
    private RTHandle m_KuwaharaRT_A;
    private RTHandle m_KuwaharaRT_B;
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
        m_StructureTensorMaterial.SetInt("_StructureTensorIterations", kuwaharaFilterSettings.structureTensorIterations);
        m_StructureTensorMaterial.SetFloat("_StructureTensorSpread", kuwaharaFilterSettings.structureTensorSpread);

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


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor structureTensorDesc = renderingData.cameraData.cameraTargetDescriptor;
        structureTensorDesc.depthBufferBits = 0;
        structureTensorDesc.colorFormat = RenderTextureFormat.ARGBFloat;
        RenderingUtils.ReAllocateIfNeeded(ref m_StructureTensorRT_A, structureTensorDesc);
        RenderingUtils.ReAllocateIfNeeded(ref m_StructureTensorRT_B, structureTensorDesc);

        RenderTextureDescriptor kuwaharaDesc = renderingData.cameraData.cameraTargetDescriptor;
        kuwaharaDesc.depthBufferBits = 0;
        kuwaharaDesc.colorFormat = RenderTextureFormat.ARGBFloat;
        m_KuwaharaDesc = kuwaharaDesc;
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaRT_A, kuwaharaDesc);
        RenderingUtils.ReAllocateIfNeeded(ref m_KuwaharaRT_B, kuwaharaDesc);

        RenderTextureDescriptor edgeFlowDesc = renderingData.cameraData.cameraTargetDescriptor;
        edgeFlowDesc.depthBufferBits = 0;
        edgeFlowDesc.colorFormat = RenderTextureFormat.RFloat;
        RenderingUtils.ReAllocateIfNeeded(ref m_EdgeFlowRT, edgeFlowDesc);
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Painterly");

        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

        cmd.Blit(cameraTargetHandle, m_StructureTensorRT_A, m_StructureTensorMaterial, 0);
        cmd.Blit(m_StructureTensorRT_A, m_StructureTensorRT_B, m_StructureTensorMaterial, 1);
        cmd.Blit(m_StructureTensorRT_B, m_StructureTensorRT_A, m_StructureTensorMaterial, 2);
        cmd.Blit(m_StructureTensorRT_A, m_StructureTensorRT_B, m_StructureTensorMaterial, 3);

        // Debug
        //cmd.Blit(m_StructureTensorRT, cameraTargetHandle);

        m_KuwaharaMaterial.SetTexture("_StructureTensorTex", m_StructureTensorRT_B);
        cmd.Blit(cameraTargetHandle, m_KuwaharaRT_A, m_KuwaharaMaterial, -1);
        
        if (m_KuwaharaFilterIterations > 1)
        {
            for (int i = 0; i < m_KuwaharaFilterIterations - 1; i++)
            {
                cmd.Blit(m_KuwaharaRT_A, m_KuwaharaRT_B, m_KuwaharaMaterial, -1);
                cmd.Blit(m_KuwaharaRT_B, m_KuwaharaRT_A, m_KuwaharaMaterial, -1);
            }
        }
        
        if (m_CompositorEnabled)
        {
            cmd.Blit(m_StructureTensorRT_A, m_EdgeFlowRT, m_LineConvolutionMaterial, -1);
        
            m_CompositeMaterial.SetTexture("_EdgeFlowTex", m_EdgeFlowRT);
            cmd.Blit(m_KuwaharaRT_A, cameraTargetHandle, m_CompositeMaterial, -1);
        }
        else
        {
            cmd.Blit(m_KuwaharaRT_A, cameraTargetHandle);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_StructureTensorMaterial);
        CoreUtils.Destroy(m_KuwaharaMaterial);
        CoreUtils.Destroy(m_LineConvolutionMaterial);
        CoreUtils.Destroy(m_CompositeMaterial);

        m_StructureTensorRT_A?.Release();
        m_StructureTensorRT_B?.Release();
        m_KuwaharaRT_A?.Release();
        m_KuwaharaRT_B?.Release();
        m_EdgeFlowRT?.Release();
    }
}