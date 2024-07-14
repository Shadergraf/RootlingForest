Shader "Hidden/KuwaharaDirectional/KuwaharaDirectional"
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

    float3 KuwaharaDirectional(float2 uv)
    {
                
        float angle = _SampleRotation;
        #if defined(_KUWAHARA_DIRECTIONAL)
            angle += Sobel(uv);
        #endif // _KUWAHARA_DIRECTIONAL

        float3 mean[4] = { {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}};
        float3 sigma[4] = { {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}, {0, 0, 0,}};
        
        float s = sin(angle);
        float c = cos(angle);

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

                    float2 uvpos = uv + offset * pixelSize * _Spread;
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
		float3 col = float3(0.0f, 0.0f, 0.0f);
        float2 pixelSize = float2(1, PIXEL_Y / PIXEL_X);
		for (int i = 0; i < 5; ++i)
		{
			col += SampleEffect(uv + sharpen_kernels[i].xy * pixelSize * spread).xyz * sharpen_kernels[i].z;
		}
		return col;
    }
    

    half4 FragKuwaharaDirectional(Varyings input) : SV_Target
    {
        //return float4(_MainTex_TexelSize.zw / float2(1920, 1080) / 2, 0, 1);
        //return half4(replicate3(dot(SampleMain(input.uv), float3(0.2126, 0.7152, 0.0722))), 1);
        //return half4(replicate3(Sobel(input.uv)), 1);
        return half4(KuwaharaDirectional(input.uv), 1);
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

        col.rgb = lerp(main, effect, _Blend);

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
            #pragma vertex vert
            #pragma fragment FragKuwaharaDirectional
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
    }
    FallBack "Diffuse"
}