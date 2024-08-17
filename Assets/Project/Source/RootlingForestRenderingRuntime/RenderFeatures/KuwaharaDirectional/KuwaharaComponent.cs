using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;


[Serializable, VolumeComponentMenu("Post-processing/Kuwahara")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class KuwaharaComponent : VolumeComponent, IPostProcessComponent
{

    /// <summary>
    /// The distance to the point of focus.
    /// </summary>
    [Tooltip("The distance to the point of focus.")]
    public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);
    /// <summary>
    /// The distance to the point of focus.
    /// </summary>
    [Tooltip("The distance to the point of focus.")]
    public FloatParameter focusStart = new FloatParameter(2);
    /// <summary>
    /// The distance to the point of focus.
    /// </summary>
    [Tooltip("The distance to the point of focus.")]
    public FloatParameter focusEnd = new FloatParameter(5);

    /// <summary>
    /// Controls the strength of the vignette effect.
    /// </summary>
    [Tooltip("Use the slider to set the strength of the Vignette effect.")]
    public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(0f, 0f, 3f);

    /// <summary>
    /// Controls the smoothness of the vignette borders.
    /// </summary>
    [Tooltip("Smoothness of the vignette borders.")]
    public ClampedFloatParameter vignetteSmoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

    /// <summary>
    /// Controls how round the vignette is, lower values result in a more square vignette.
    /// </summary>
    [Tooltip("Should the vignette be perfectly round or be dependent on the current aspect ratio?")]
    public BoolParameter vignetteRounded = new BoolParameter(false);

    /// <inheritdoc/>
    public bool IsActive() => vignetteIntensity.value > 0f;

    /// <inheritdoc/>
    [Obsolete("Unused #from(2023.1)", false)]
    public bool IsTileCompatible() => true;
}