Shader "Game/NMapURP"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _OverlayTex("Overlay Texture", 2D) = "white" {}
        _Texture2DCover("Cover Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);
            TEXTURE2D(_Texture2DCover);
            SAMPLER(sampler_Texture2DCover);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OverlayTex_ST;
                float4 _Texture2DCover_ST;
                float4 _Color;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv0 = TRANSFORM_TEX(input.uv0, _MainTex);
                output.uv1 = TRANSFORM_TEX(input.uv1, _OverlayTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv0);
                half4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, input.uv1);
                half4 coverColor = SAMPLE_TEXTURE2D(_Texture2DCover, sampler_Texture2DCover, input.uv1);
                half blend = saturate(max(overlayColor.a, coverColor.a));
                half3 mixedRgb = lerp(mainColor.rgb, overlayColor.rgb, blend);
                half alpha = max(mainColor.a, blend);
                return half4(mixedRgb, alpha) * _Color;
            }
            ENDHLSL
        }
    }
}
