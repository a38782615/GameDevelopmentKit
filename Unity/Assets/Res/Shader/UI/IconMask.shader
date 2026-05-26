// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/IconMask"
{
   Properties 
    {  
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Mask ("Base (RGB)", 2D) = "white" {}  


        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }
    
    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {         
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct a2v
            {
                fixed2 uv : TEXCOORD0;
                fixed2 uv1 : TEXCOORD1;
                float4 vertex : POSITION;
                float4 color    : COLOR;
                float2 uv2 : TEXCOORD2;
            };

            fixed4 _Color;

            struct v2f
            {
                fixed2 uv : TEXCOORD0;
                fixed2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 WPS : TEXCOORD3;
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _Mask;  
            float4 _ClipRect;

            v2f vert (a2v i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;
                o.uv1 = i.uv1;
                o.uv2 = i.uv2;
                o.WPS = i.vertex;
                o.color = i.color * _Color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv) * i.color; 
                float4 mask = tex2D(_Mask, i.uv2.xy); 
                color.a *= mask.a;
                //根据_ClipRect比较当前像素是否在裁切区域中，如果不在颜色将设置成透明
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.WPS.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }  
    }   
}
