Shader "Game/NMap/SurfaceMasked"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _Texture2DCover ("Cover Texture", 2D) = "white" {}
        _WaterMaskTex ("Water Mask", 2D) = "black" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _brightness ("Brightness", Float) = 1
        _BlendFactor ("Blend Factor", Range(0, 1)) = 0.1
        _UseGlobalMask ("Use Global Mask", Range(0, 1)) = 1
        _LiquidMaskChannel ("Liquid Mask Channel", Range(0, 1)) = 0
        _WaterMaskParams ("Water Mask Params", Vector) = (0, 0, 1, 1)
        _CutoutLow ("Cutout Low", Range(0, 1)) = 0.18
        _CutoutHigh ("Cutout High", Range(0, 1)) = 0.72
        _AlphaClip ("Alpha Clip", Range(0, 0.5)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);
            TEXTURE2D(_Texture2DCover);
            SAMPLER(sampler_Texture2DCover);
            TEXTURE2D(_WaterMaskTex);
            SAMPLER(sampler_WaterMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _WaterMaskParams;
                float _brightness;
                float _BlendFactor;
                float _UseGlobalMask;
                float _LiquidMaskChannel;
                float _CutoutLow;
                float _CutoutHigh;
                float _AlphaClip;
            CBUFFER_END

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
                float2 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv0 = input.uv0;
                output.uv1 = input.uv1;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xy;
                return output;
            }

            half GetMask(half4 sampleValue)
            {
                return saturate(max(sampleValue.a, max(sampleValue.r, max(sampleValue.g, sampleValue.b))));
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv0);
                half4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, input.uv1);
                half4 coverColor = SAMPLE_TEXTURE2D(_Texture2DCover, sampler_Texture2DCover, input.uv1);

                half coverMask = GetMask(coverColor);
                half overlayBlend = saturate(coverMask + _BlendFactor);
                half3 rgb = lerp(mainColor.rgb, overlayColor.rgb, overlayBlend);

                half alpha = mainColor.a * _Color.a;
                if (_UseGlobalMask > 0.5h)
                {
                    float2 maskUv = saturate((input.positionWS - _WaterMaskParams.xy) * _WaterMaskParams.zw);
                    half4 maskColor = SAMPLE_TEXTURE2D(_WaterMaskTex, sampler_WaterMaskTex, maskUv);
                    half liquidCoverage = lerp(maskColor.r, maskColor.g, saturate(_LiquidMaskChannel));
                    half landCoverage = 1.0h - smoothstep(_CutoutLow, _CutoutHigh, liquidCoverage);
                    alpha *= landCoverage;
                }

                clip(alpha - _AlphaClip);
                return half4(rgb * _Color.rgb * _brightness, alpha);
            }
            ENDHLSL
        }
    }
}
