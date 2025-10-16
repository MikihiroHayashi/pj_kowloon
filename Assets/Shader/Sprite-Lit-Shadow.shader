Shader "Custom/Sprite-Lit-Shadow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        LOD 200
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha


        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            // --- Fix for missing LerpWhiteTo (URP 14.0.11 bug) ---
            #ifndef LerpWhiteTo
            inline half3 LerpWhiteTo(half3 color, half t)
            {
                return lerp(half3(1.0h, 1.0h, 1.0h), color, t);
            }
            #endif

            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Structures
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;

            // Vertex
            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            // Fragment
            half4 frag (Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

              
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 normalWS = float3(0, 0, -1); 
                half NdotL = saturate(dot(normalWS, -lightDir));

                
                half3 litColor = texColor.rgb * (mainLight.color * NdotL + 0.2h); 

                return half4(litColor, texColor.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM

            #ifndef LerpWhiteTo
            inline half3 LerpWhiteTo(half3 color, half t)
            {
                return lerp(half3(1.0h, 1.0h, 1.0h), color, t);
            }
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
