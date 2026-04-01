Shader "Game/NMap/WaterFlow"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _Texture2DCover ("Cover Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _brightness ("Brightness", Float) = 1
        _BlendFactor ("Blend Factor", Range(0, 1)) = 0.2
        _MainFlow ("Main Flow", Vector) = (0.01, 0.004, 0, 0)
        _OverlayFlow ("Overlay Flow", Vector) = (-0.018, 0.01, 0, 0)
        _MainTiling ("Main Tiling", Float) = 1
        _OverlayTiling ("Overlay Tiling", Float) = 1.15
        _OverlayStrength ("Overlay Strength", Range(0, 1)) = 0.68
        _DistortionStrength ("Distortion Strength", Range(0, 0.2)) = 0.025
        _MaskLow ("Mask Low", Range(0, 1)) = 0.04
        _MaskHigh ("Mask High", Range(0, 1)) = 0.4
        _FoamBand ("Foam Band", Range(0.01, 0.5)) = 0.18
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.32
        _FoamBrightness ("Foam Brightness", Float) = 1.25
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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainFlow;
                float4 _OverlayFlow;
                float _brightness;
                float _BlendFactor;
                float _MainTiling;
                float _OverlayTiling;
                float _OverlayStrength;
                float _DistortionStrength;
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
                float2 uv1 : TEXCOORD1;
                float2 positionOS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv1 = input.uv1;
                output.positionOS = input.positionOS.xy;
                return output;
            }

            half GetMask(half4 sampleValue)
            {
                return saturate(max(sampleValue.a, max(sampleValue.r, max(sampleValue.g, sampleValue.b))));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float timeValue = _Time.y;
                float2 coverUv = input.uv1;
                float2 mainBaseUv = input.positionOS * _MainTiling;
                float2 overlayBaseUv = input.positionOS * _OverlayTiling;

                half4 coverColor = SAMPLE_TEXTURE2D(_Texture2DCover, sampler_Texture2DCover, coverUv);
                half coverMask = GetMask(coverColor);
                half shoreMask = smoothstep(_MaskLow, _MaskHigh, coverMask);
                half innerMask = smoothstep(_MaskLow + _FoamBand, _MaskHigh + _FoamBand, coverMask);
                half foamMask = saturate((shoreMask - innerMask) * _FoamStrength);

                float2 flowedOverlayUv = overlayBaseUv + _OverlayFlow.xy * timeValue;
                half4 overlayFlowSample = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, flowedOverlayUv);
                float2 distortion = (overlayFlowSample.rg * 2.0h - 1.0h) * _DistortionStrength;

                float2 mainUv = mainBaseUv + _MainFlow.xy * timeValue + distortion;
                float2 overlayUv = flowedOverlayUv + distortion * 0.5;

                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUv);
                half4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, overlayUv);

                half blend = saturate(_BlendFactor + _OverlayStrength * shoreMask);
                half3 rgb = lerp(mainColor.rgb, overlayColor.rgb, blend);
                half3 foamColor = overlayColor.rgb * _FoamBrightness;
                rgb = lerp(rgb, foamColor, foamMask);
                half alpha = shoreMask;

                return half4(rgb * _Color.rgb * _brightness, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
