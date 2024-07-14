Shader "Hidden/Kuwahara/Kuwahara"
{
    HLSLINCLUDE
    
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    
    #pragma multi_compile_fragment _ _KUWAHARA_DIRECTIONAL


    TEXTURE2D(_MainTex);
    float4 _MainTex_TexelSize;
    SAMPLER(sampler_MainTex);
    
    TEXTURE2D(_EffectTex);
    SAMPLER(sampler_EffectTex);

    TEXTURE2D(_MaskTex);
    SAMPLER(sampler_MaskTex);

    TEXTURE2D(_DepthTex);
    SAMPLER(sampler_DepthTex);

    int _Radius;
    float _Spread;
    float _SampleRotation;

    int _BlurIterations;
    float _BlurSpread;
    
    // Post processing
    float _SharpenSpread;
    float _Blend;
            

    static const float sobelX[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};
    static const float sobelY[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};

    static const float2 offsets[4] =
    {
        {-_Radius, -_Radius},
        {-_Radius,  0},
        { 0, -_Radius},
        { 0,  0},
    };

    static const float3 sharpen_kernels[5] =
    {
        {-1, 0, -1},
        {1, 0, -1},
        { 0, -1, -1},
        { 0, 1, -1},
        { 0, 0, 5},
    };


    #define VIEWSIZE _MainTex_TexelSize.zw
    #define TEXELSIZE (_MainTex_TexelSize.xy)

    #define PIXEL_X (_MainTex_TexelSize.x)
    #define PIXEL_Y (_MainTex_TexelSize.y)
    
    #define E 2.71828f

    #define replicate3(x) float3(x, x, x)


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
    float3 SampleEffect(float2 uv)
    {
        return SAMPLE_TEXTURE2D(_EffectTex, sampler_EffectTex, uv).rgb;
    }
    float SampleMask(float2 uv)
    {
        return SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
    }
            
    float Sobel(float2 uv)
    {
        float gradientX = 0;
        float gradientY = 0;
        int index = 0;

        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            [unroll]
            for (int y = -1; y <= 1; y++)
            {
                if (index == 4)
                {
                    index++;
                    continue;
                }

                float2 offset = float2(x, y) * TEXELSIZE;
                float3 col = SampleMain(uv + offset).rgb;
                float lum = dot(col, float3(0.2126, 0.7152, 0.0722));

                gradientX += lum * sobelX[index];
                gradientY += lum * sobelY[index];

                index++;
            }
        }

        float angle = 0;
        if (abs(gradientX) > 0.001)
        {
            angle = atan(gradientY / gradientX);
        }

        return angle;
    }

    float3 Kuwahara(float2 uv)
    {
                
        float angle = _SampleRotation;
        #if defined(_KUWAHARA_DIRECTIONAL)
            angle += Sobel(uv);
        #endif // _KUWAHARA_DIRECTIONAL

        float3 mean[4] = { {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}};
        float3 sigma[4] = { {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}};
        
        float s = sin(angle);
        float c = cos(angle);

        float spread = _Spread * SampleMask(uv);

        float3 col;
        float2 offset;
        float2 pixelSize = float2(1, PIXEL_Y / PIXEL_X);
        [unroll]
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < _Radius; j++)
            {
                for (int k = 0; k < _Radius; k++)
                {
                    offset = float2(j, k) + offsets[i];
                    offset = float2(offset.x * c - offset.y * s, offset.x * s + offset.y * c);  // rotate sample by angle

                    float2 uvpos = uv + offset * pixelSize * spread;
                    col = SampleMain(uvpos);

                    mean[i] += col;
                    sigma[i] += col * col;
                }
            }
        }

        float n = _Radius * _Radius;
        float sigma_f;

        float min = 1;
        [unroll]
        for (int i = 0; i < 4; i++)
        {
            mean[i] /= n;
            sigma[i] = abs(sigma[i] / n - mean[i] * mean[i]);
            sigma_f = sigma[i].r + sigma[i].g + sigma[i].b;

            if (sigma_f < min)
            {
                min = sigma_f;
                col = mean[i];
            }
        }

        return lerp(SampleMain(uv), col, _Blend);
    }
    
    float gaussian(int x, float spread)
	{
		float sigmaSqu = spread * spread;
		return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
	}

    half3 gauss(float2 uv, float2 dir, int iterations, float spread)
    {
		float3 col = float3(0.0f, 0.0f, 0.0f);
		float gridSum = 0.0f;

		int upper = iterations;
		int lower = -upper;

        spread = spread * SampleMask(uv);

        float2 pixelSize = float2(1, PIXEL_Y / PIXEL_X);
		for (int x = lower; x <= upper; ++x)
		{
			float gauss = gaussian(x, 2);
			gridSum += gauss;
			col += gauss * SampleMain(uv + pixelSize * dir * x * spread).xyz;
		}
		col /= gridSum;

		return col;
    }
    
    half3 sharpenEffect(float2 uv, float spread)
    {
        spread = spread * SampleMask(uv);
		float3 col = float3(0.0f, 0.0f, 0.0f);
        float2 pixelSize = float2(1, PIXEL_Y / PIXEL_X);
		for (int i = 0; i < 5; ++i)
		{
			col += SampleEffect(uv + sharpen_kernels[i].xy * pixelSize * spread).xyz * sharpen_kernels[i].z;
		}
		return col;
    }

    half DepthMask(float2 uv)
    {
        #if UNITY_REVERSED_Z
            real depth = SampleSceneDepth(uv);
        #else
            // Adjust z to match NDC for OpenGL
            real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
        #endif

        float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

        return saturate(max(0, abs(worldPos.y - 5) - 2) / 3);
    }
    half ScreenMask(float2 uv)
    {
        float mask = length(uv - 0.5) * 2 * 0.707;
        mask = mask;
        return mask;
    }
    

    half4 FragKuwahara(Varyings input) : SV_Target
    {
        return half4(Kuwahara(input.uv), 1);
    }
    half4 FragGaussV(Varyings input) : SV_Target
    {
		return float4(gauss(input.uv, float2(1.0, 0.0), _BlurIterations, _BlurSpread), 1.0f);
    }
    half4 FragGaussH(Varyings input) : SV_Target
    {
		return float4(gauss(input.uv, float2(0.0, 1.0), _BlurIterations, _BlurSpread), 1.0f);
    }
    half4 FragComposite(Varyings input) : SV_Target
    {
        float3 main = SampleMain(input.uv).rgb;
        float3 effect = sharpenEffect(input.uv, _SharpenSpread);

        float4 col = float4(0, 0, 0, 1);

        float blend = _Blend;
        blend *= SampleMask(input.uv).r;

        col.rgb = lerp(main, effect, blend);

		return col;
    }
    half FragMask(Varyings input) : SV_Target
    {
		float mask = 0;
		mask += DepthMask(input.uv);
		mask += ScreenMask(input.uv);
		return saturate(mask);
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
            #pragma vertex vert
            #pragma fragment FragKuwahara
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment FragGaussV
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment FragGaussH
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment FragComposite
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment FragMask
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}