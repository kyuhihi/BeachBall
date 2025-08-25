Shader "Custom/UI/NoiseTextureBalloon"
{
    Properties
    {
        _MainTex        ("Main Tex (RGB A)", 2D) = "white" {}
        _Color          ("Tint Color", Color) = (1,1,1,1)

        _NoiseTex       ("Noise Tex (R)", 2D) = "gray" {}
        _NoiseColor     ("Noise Color", Color) = (1,1,1,1)
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.5
        _NoiseContrast  ("Noise Contrast", Range(0.1,5)) = 1
        _NoiseScale     ("Noise Tiling", Float) = 1
        _NoiseSpeed     ("Noise Scroll (XY)", Vector) = (0.1,0.1,0,0)

        _ModulateAlpha  ("Modulate Alpha (0/1)", Float) = 0

        [KeywordEnum(Lerp,Add,Multiply,Overlay)]
        _NoiseBlendMode ("Noise Blend Mode", Float) = 0

        _AlphaClip      ("Alpha Clip (0=Off)", Range(0,1)) = 0

        // UI Mask / Stencil 호환 (Unity UI 기본과 유사)
        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID", Float) = 0
        _StencilOp      ("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask      ("Color Mask", Float) = 15
        _UseUIClipRect  ("Use UI ClipRect (0/1)", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]

        Pass
        {
            Name "UINoise"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma shader_feature_local _NOISEBLENDMODE_LERP _NOISEBLENDMODE_ADD _NOISEBLENDMODE_MULTIPLY _NOISEBLENDMODE_OVERLAY

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            float4 _Color;
            float4 _NoiseColor;
            float  _NoiseIntensity;
            float  _NoiseContrast;
            float  _NoiseScale;
            float4 _NoiseSpeed; // xy 사용

            float  _ModulateAlpha;
            float  _AlphaClip;
            float  _UseUIClipRect;

            // UI ClipRect (Unity UI 시스템에서 세팅)
            float4 _ClipRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 uvNoise : TEXCOORD1;
                float4 color : COLOR;
                float4 worldPos : TEXCOORD2; // ClipRect용
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float2 nUV = v.uv * _NoiseScale;
                nUV = nUV * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                o.uvNoise = float4(nUV,0,0);
                o.color = v.color * _Color;
                o.worldPos = v.vertex;
                return o;
            }

            // Overlay 함수
            float3 OverlayBlend(float3 baseCol, float3 over)
            {
                // 표준 Overlay: base<0.5 -> 2*base*over, else 1 - 2*(1-base)*(1-over)
                float3 res;
                float3 lt = step(baseCol, 0.5);
                float3 low = 2.0 * baseCol * over;
                float3 high = 1.0 - 2.0 * (1.0 - baseCol) * (1.0 - over);
                res = lerp(high, low, lt); // lt=1 -> base<0.5
                return res;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 메인
                float4 mainCol = tex2D(_MainTex, i.uv) * i.color;

                if (_AlphaClip > 0 && mainCol.a < _AlphaClip) discard;

                // 노이즈 UV 스크롤
                float2 noiseUV = i.uvNoise.xy + _Time.y * _NoiseSpeed.xy;
                float noiseSample = tex2D(_NoiseTex, noiseUV).r;

                // 콘트라스트
                noiseSample = saturate(pow(noiseSample, _NoiseContrast));

                float3 noiseRGB = _NoiseColor.rgb * noiseSample;

                float3 baseRGB = mainCol.rgb;
                float3 blended = baseRGB;

                #if defined(_NOISEBLENDMODE_ADD)
                    blended = baseRGB + noiseRGB * _NoiseIntensity;
                #elif defined(_NOISEBLENDMODE_MULTIPLY)
                    blended = baseRGB * lerp(1.0, noiseRGB, _NoiseIntensity);
                #elif defined(_NOISEBLENDMODE_OVERLAY)
                    {
                        float3 ov = OverlayBlend(baseRGB, noiseRGB);
                        blended = lerp(baseRGB, ov, _NoiseIntensity);
                    }
                #else // Lerp
                    blended = lerp(baseRGB, noiseRGB, _NoiseIntensity);
                #endif

                // Alpha
                float outA = mainCol.a;
                if (_ModulateAlpha > 0.5)
                    outA *= lerp(1.0, noiseSample, _NoiseIntensity);

                // UI ClipRect
                if (_UseUIClipRect > 0.5)
                {
                    // UnityUI.cginc: clip(rect)
                    if (UnityGet2DClipping(i.worldPos.xy, _ClipRect) == 0)
                        discard;
                }

                return float4(saturate(blended), outA);
            }

            ENDCG
        }
    }

    FallBack Off
}
