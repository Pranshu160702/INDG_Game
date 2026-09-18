Shader "Custom/UIBlur"
{
    Properties
    {
        [Toggle(IS_BLUR_ALPHA_MASKED)] _IsAlphaMasked("Image Alpha Masks Blur", Float) = 1
        [Toggle(IS_SPRITE_VISIBLE)] _IsSpriteVisible("Show Image", Float) = 1

        _Radius("Blur Radius", Range(0, 64)) = 1
        _OverlayColor("Blurred Overlay/Opacity", Color) = (0.5, 0.5, 0.5, 1)

        [HideInInspector][PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
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
            Name "UIBlur"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ IS_BLUR_ALPHA_MASKED
            #pragma multi_compile_local __ IS_SPRITE_VISIBLE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OverlayColor;
                float  _Radius;
                float4 _ClipRect;
            CBUFFER_END

            float4 _CameraOpaqueTexture_TexelSize;

            struct Attributes
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvmain      : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.vertex.xyz);
                OUT.uvmain = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            #define BLEND_OVERLAY(a,b) ((b) <= 0.5 ? (2*(b))*(a) : (1-(1-2*((b)-0.5))*(1-(a))))

            half3 overlayBlend(half3 back, half3 front)
            {
                return half3(
                    BLEND_OVERLAY(back.r, front.r),
                    BLEND_OVERLAY(back.g, front.g),
                    BLEND_OVERLAY(back.b, front.b));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Screen UV from clip position
                float2 screenUV = IN.positionHCS.xy * _CameraOpaqueTexture_TexelSize.xy;

                half4 spritePx = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvmain);

                float radius = clamp(_Radius, 0, 64);
                float4 sum = 0;
                float steps = 0;
                float step = 2.0;

                // Separable blur approximated in one pass (X+Y combined)
                for (float r = 0; r <= radius; r += step)
                {
                    float2 offX = float2(r * _CameraOpaqueTexture_TexelSize.x, 0);
                    float2 offY = float2(0, r * _CameraOpaqueTexture_TexelSize.y);
                    sum += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + offX);
                    sum += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV - offX);
                    sum += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + offY);
                    sum += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV - offY);
                    steps += (r == 0) ? 1 : 4;
                }
                half4 blurred = sum / max(steps, 1);

                float4 overlayColor = _OverlayColor;
                half3 result = overlayBlend(blurred.rgb, overlayColor.rgb);

                #if IS_BLUR_ALPHA_MASKED
                    float visibility = overlayColor.a * spritePx.a;
                #else
                    float visibility = overlayColor.a;
                #endif

                #if IS_SPRITE_VISIBLE
                    half4 blurLayer = half4(result, visibility);
                    float a0 = spritePx.a * IN.color.a;
                    float a1 = blurLayer.a;
                    float a01 = (1 - a0) * a1 + a0;
                    half3 blended = ((1 - a0) * a1 * blurLayer.rgb + a0 * (spritePx.rgb * IN.color.rgb)) / max(a01, 0.0001);
                    return half4(blended, a01);
                #else
                    return half4(result, visibility);
                #endif
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
