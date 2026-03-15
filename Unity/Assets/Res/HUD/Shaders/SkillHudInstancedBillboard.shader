Shader "Game/HUD/InstancedBillboard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _HudColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _HudUvRect)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 uvRect = UNITY_ACCESS_INSTANCED_PROP(Props, _HudUvRect);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = uvRect.xy + input.uv * uvRect.zw;
                output.color = UNITY_ACCESS_INSTANCED_PROP(Props, _HudColor);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half distanceAlpha = texColor.a;
                half edge = max(fwidth(distanceAlpha) * 0.75h, 0.01h);
                half alpha = smoothstep(0.5h - edge, 0.5h + edge, distanceAlpha);
                half4 color = input.color;
                color.a *= alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
