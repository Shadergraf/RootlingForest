Shader "Hidden/CanopyShadows/CanopyShadows"
{
    HLSLINCLUDE
    
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
    

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    
    TEXTURE2D(_LastTex);
    SAMPLER(sampler_LastTex);

    float _Mix;
    
    #define replicate3(x) float3(x, x, x)


    struct VertexAttributes
    {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
    };
    
    struct FragmentVaryings
    {
        float2 uv     : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    FragmentVaryings VertToFrag(VertexAttributes input)
    {
        FragmentVaryings output = (FragmentVaryings)0;
    
        VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
        output.vertex = vertexInput.positionCS;
        output.uv = input.uv;
    
        return output;
    }
    
    float4 SampleMain(float2 uv)
    {
        return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    }
    float4 SampleLast(float2 uv)
    {
        return SAMPLE_TEXTURE2D(_LastTex, sampler_LastTex, uv);
    }
    half4 Frag(FragmentVaryings input) : SV_Target
    {
        float4 main = SampleMain(input.uv);
        float4 last = SampleLast(input.uv);

        float4 col = lerp(last, main, _Mix);

		return col;
    }


    ENDHLSL
    
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertToFrag
            #pragma fragment Frag
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}