Shader "Hidden/Painterly/StructureTensor"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
            
    float _StructureTensorSpread;
    int _StructureTensorIterations;
    



    #define E 2.71828f
    #define PIXEL_X (_ScreenParams.z - 1)
    #define PIXEL_Y (_ScreenParams.w - 1)


    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
    };
        
    struct Varyings
    {
        float2 uv     : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };
        
    Varyings vert(Attributes input)
    {
        Varyings output = (Varyings)0;
        
        VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
        output.vertex = vertexInput.positionCS;
        output.uv = input.uv;
        
        return output;
    }

    float3 SampleMain(float2 uv)
    {
        return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
    }

    float3 SobelU(float2 uv)
    {
        return (
            -1.0f * SampleMain(uv + float2(-PIXEL_X, -PIXEL_Y)) +
            -2.0f * SampleMain(uv + float2(-PIXEL_X, 0)) +
            -1.0f * SampleMain(uv + float2(-PIXEL_X, PIXEL_Y)) +
        
            1.0f * SampleMain(uv + float2(PIXEL_X, -PIXEL_Y)) +
            2.0f * SampleMain(uv + float2(PIXEL_X, 0)) +
            1.0f * SampleMain(uv + float2(PIXEL_X, PIXEL_Y))
        ) / 4.0;     
    }
        
    float3 SobelV(float2 uv)
    {
        return (
            -1.0f * SampleMain(uv + float2(-PIXEL_X, -PIXEL_Y)) +
            -2.0f * SampleMain(uv + float2(0, -PIXEL_Y)) +
            -1.0f * SampleMain(uv + float2(PIXEL_X, -PIXEL_Y)) +
        
            1.0f * SampleMain(uv + float2(-PIXEL_X, PIXEL_Y)) +
            2.0f * SampleMain(uv + float2(0, PIXEL_Y)) +
            1.0f * SampleMain(uv + float2(PIXEL_X, PIXEL_Y))
        ) / 4.0;    
    }

    float3 StructureTensor(float2 uv)
    {
        float3 u = SobelU(uv);
        float3 v = SobelV(uv);
            
        return float3(dot(u, u), dot(v, v), dot(u, v));
    }
            

    
    float gaussian(int x)
	{
		float sigmaSqu = _StructureTensorSpread * _StructureTensorSpread;
		return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
	}

    half3 gauss(float2 uv, float2 delta)
    {
		float3 col = float3(0.0f, 0.0f, 0.0f);
		float gridSum = 0.0f;

		int upper = _StructureTensorIterations - 1;
		int lower = -upper;

		for (int x = lower; x <= upper; ++x)
		{
			float gauss = gaussian(x);
			gridSum += gauss;
			col += gauss * SampleMain(uv + float2(PIXEL_X, PIXEL_Y) * delta * x).xyz;
		}

		col /= gridSum;

		return col;
    }

    half4 tensor(Varyings input) : SV_Target
    {
		return float4(StructureTensor(input.uv), 1.0f);
    }
    
    half4 gaussV(Varyings input) : SV_Target
    {
		return float4(gauss(input.uv, float2(1.0, 0.0)), 1.0f);
    }
    half4 gaussH(Varyings input) : SV_Target
    {
		return float4(gauss(input.uv, float2(0.0, 1.0)), 1.0f);
    }

    half4 pack(Varyings input) : SV_Target
    {
        float3 t = SampleMain(input.uv);
        
        float lambda1 = 0.5f * (t.x + t.y + sqrt((t.x - t.y) * (t.x - t.y) + 4.0f * t.z * t.z));
        float lambda2 = 0.5f * (t.x + t.y - sqrt((t.x - t.y) * (t.x - t.y) + 4.0f * t.z * t.z));
        
        float2 direction = float2(lambda1 - t.x, -t.z);
        direction = (length(direction) > 0.0) ? normalize(direction) : float2(0, 1);
        
        float angle = atan2(direction.y, direction.x);
        
        float anisotropy = (lambda1 + lambda2 <= 0.0) ? 0.0 : (lambda1 - lambda2) / (lambda1 + lambda2);
        
        return half4(direction, angle, anisotropy);
    }
        
    ENDHLSL


    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StructureTensorSpread ("StructureTensorSpread", Range(0.1, 32)) = 1
        [IntRange] _StructureTensorIterations ("StructureTensorIterations", Range(1, 8)) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Structure Tensor"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment tensor
            ENDHLSL
        }
        Pass
        {
            Name "Blur Vertical"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment gaussV
            ENDHLSL
        }
        Pass
        {
            Name "Blur Horizontal"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment gaussH
            ENDHLSL
        }
        Pass
        {
            Name "Pack Tensors"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment pack
            ENDHLSL
        }
    }
}
