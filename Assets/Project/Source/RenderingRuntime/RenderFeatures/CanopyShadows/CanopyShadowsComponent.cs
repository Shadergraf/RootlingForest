using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;


[Serializable, VolumeComponentMenu("Post-processing/CanopyShadows")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class CanopyShadowsComponent : VolumeComponent, IPostProcessComponent
{

    /// <inheritdoc/>
    public bool IsActive() => true;
}