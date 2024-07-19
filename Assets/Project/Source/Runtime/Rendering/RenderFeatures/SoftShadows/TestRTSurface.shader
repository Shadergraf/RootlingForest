// This shader fills the mesh shape with a color that a user can change using the
// Inspector window on a Material.
Shader "Custom/TestRTSurface"
{    
    // The _BaseColor variable is visible in the Material's Inspector, as a field 
    // called Base Color. You can use it to select a custom color. This variable
    // has the default value (1, 1, 1, 1).
    Properties
    { 
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _RTColor("RT Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"        
        
        // To make the Unity shader SRP Batcher compatible, declare all
        // properties related to a Material in a a single CBUFFER block with 
        // the name UnityPerMaterial.
        CBUFFER_START(UnityPerMaterial)
            // The following line declares the _BaseColor variable, so that you
            // can use it in the fragment shader.
            half4 _BaseColor;
            float4 _RTColor;
        CBUFFER_END


        ENDHLSL



        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
    

            struct Attributes
            {
                float4 positionOS   : POSITION;                 
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                // Returning the _BaseColor value.                
                return _BaseColor;
            }
            ENDHLSL
        }
        Pass
        {
            Name "RayTracing"
            Tags { "LightMode" = "RayTracing" }

            HLSLPROGRAM

            #pragma raytracing test

            
            struct RayPayload
            {
                float4 color;
            };
            struct AttributeData
            {
                float2 barycentrics;
                float2 uv;
            };

            [shader("closesthit")]
            void ClosestHitShader(inout RayPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
            {
                payload.color = _RTColor;
            }

            ENDHLSL
        }
    }
}