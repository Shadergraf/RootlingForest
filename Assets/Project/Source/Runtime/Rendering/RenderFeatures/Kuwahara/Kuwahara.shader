Shader "Hidden/Kuwahara/Kuwahara"
{
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            

            #define VIEWSIZE (_ScreenParams.xy)

            #define PIXEL_X (_ScreenParams.z - 1)
            #define PIXEL_Y (_ScreenParams.w - 1)


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            int _Radius;
            float _Blend;

            float3 mean[4] =
            {
                {0, 0, 0},
                {0, 0, 0},
                {0, 0, 0},
                {0, 0, 0},
            };
            float3 sigma[4] =
            {
                {0, 0, 0},
                {0, 0, 0},
                {0, 0, 0},
                {0, 0, 0},
            };
            
            float2 offsets[4];


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

            float3 Kuwahara(float2 uv)
            {
                float2 pos;
                float3 col;

                offsets[0] =float2(-1, -1);
                offsets[1] =float2(-1, 0);
                offsets[2] =float2(0, -1);
                offsets[3] =float2(0, 0);

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < _Radius; j++)
                    {
                        for (int k = 0; k < _Radius; k++)
                        {
                            pos = float2(j, k) + offsets[i] * _Radius;
                            float2 uvpos = uv + pos * float2(PIXEL_X, PIXEL_Y);

                            col = SampleMain(uvpos);
                            mean[i] += col;
                            sigma[i] += col * col;
                        }
                    }
                }

                float n = pow(_Radius, 2);
                float sigma_f;

                float min = 1;
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


            half4 frag(Varyings input) : SV_Target
            {
                return half4(Kuwahara(input.uv), 1);
            }
    
            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}