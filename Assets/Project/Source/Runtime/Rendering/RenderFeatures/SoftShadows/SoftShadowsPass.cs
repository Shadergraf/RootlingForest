using Manatea;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SoftShadowsRenderPass : ScriptableRenderPass
{
    RayTracingShader m_RayTraceShader;
    RayTracingAccelerationStructure m_RayTraceAccStruct;

    private RTHandle m_RayTraceResultRT_A;
    private RTHandle m_RayTraceResultRT_B;

    public SoftShadowsRenderPass(RayTracingShader shader)
    {
        renderPassEvent = RenderPassEvent.AfterRendering;


        var settings = new RayTracingAccelerationStructure.Settings()
        {
            layerMask = -1,
            managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic,
            rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything,
        };
        m_RayTraceAccStruct = new RayTracingAccelerationStructure(settings);

        m_RayTraceShader = shader;
    }

    public void Setup()
    {
    }


    /// <inheritdoc/>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (!VolumeManager.instance.stack.isValid)
            return;
        SoftShadowsComponent softShadows = VolumeManager.instance.stack.GetComponent<SoftShadowsComponent>();
        if (!softShadows || !softShadows.active)
            return;

        RenderTextureDescriptor rayTraceDesc = renderingData.cameraData.cameraTargetDescriptor;
        rayTraceDesc.depthBufferBits = 0;
        rayTraceDesc.colorFormat = RenderTextureFormat.ARGBFloat;
        rayTraceDesc.enableRandomWrite = true;
        RenderingUtils.ReAllocateIfNeeded(ref m_RayTraceResultRT_A, rayTraceDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_RayTraceResultRT_A");
        RenderingUtils.ReAllocateIfNeeded(ref m_RayTraceResultRT_B, rayTraceDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_RayTraceResultRT_B");
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!VolumeManager.instance.stack.isValid)
            return;
        SoftShadowsComponent softShadows = VolumeManager.instance.stack.GetComponent<SoftShadowsComponent>();
        if (!softShadows || !softShadows.active)
            return;

        var cameraDesc = renderingData.cameraData.cameraTargetDescriptor;
        RTHandle cameraColorRt = renderingData.cameraData.renderer.cameraColorTargetHandle;

        CommandBuffer cmd = CommandBufferPool.Get("SoftShadows");

        cmd.BuildRayTracingAccelerationStructure(m_RayTraceAccStruct);
        cmd.SetRayTracingAccelerationStructure(m_RayTraceShader, Shader.PropertyToID("g_SceneAccelStruct"), m_RayTraceAccStruct);
        cmd.SetRayTracingTextureParam(m_RayTraceShader, Shader.PropertyToID("g_Output"), m_RayTraceResultRT_B);
        cmd.SetRayTracingFloatParam(m_RayTraceShader, Shader.PropertyToID("g_Zoom"), Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f) * softShadows.m_Zoom.value);
        cmd.SetRayTracingFloatParam(m_RayTraceShader, Shader.PropertyToID("g_AspectRatio"), cameraDesc.width / (float)cameraDesc.height);

        cmd.Blit(cameraColorRt, m_RayTraceResultRT_A);
        cmd.DispatchRays(m_RayTraceShader, "MainRayGenShader", (uint)cameraDesc.width, (uint)cameraDesc.height, 1, renderingData.cameraData.camera);
        cmd.Blit(m_RayTraceResultRT_B, cameraColorRt);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        m_RayTraceResultRT_A?.Release();
        m_RayTraceResultRT_B?.Release();
    }
}