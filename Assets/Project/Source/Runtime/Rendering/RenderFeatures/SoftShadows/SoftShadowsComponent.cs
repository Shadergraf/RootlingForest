using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;


[Serializable, VolumeComponentMenu("Post-processing/SoftShadows")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class SoftShadowsComponent : VolumeComponent, IPostProcessComponent
{
    public FloatParameter m_Zoom = new FloatParameter(1);

    public bool IsActive() => true;
}