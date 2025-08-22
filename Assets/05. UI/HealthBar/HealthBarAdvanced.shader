Shader "Custom/UI/HealthBarAdvanced_Refactored"
{
    Properties
    {
        // Core
        _Health                ("Health (0-1)", Range(0,1)) = 1

        // Fill / Gradient
        _FillColorA            ("Fill Color A", Color) = (1,0.55,0.05,1)
        _FillColorB            ("Fill Color B", Color) = (1,0.85,0.25,1)
        _FillSecondary         ("Secondary Tint", Color) = (1,0.4,0.1,1)
        _SecondaryMix          ("Secondary Mix", Range(0,1)) = 0.35
        _BackColor             ("Back Color", Color) = (0.07,0.07,0.07,1)

        // Edge Highlight (HP 끝 경계)
        [HDR]_EdgeColor        ("Edge Color (HDR)", Color) = (1.5,1.5,1.5,1)
        _EdgeWidthPx           ("Edge Width (px)", Range(0,32)) = 6
        _EdgeSharpness         ("Edge Sharpness", Range(1,256)) = 128
        _EdgeAdd               ("Edge Add Intensity", Range(0,20)) = 6
        _EdgeLerp              ("Edge Lerp (Blend)", Range(0,1)) = 0.35

        // Head Highlight (시작 부분)
        [HDR]_HeadColor        ("Head Color (HDR)", Color) = (1.2,0.9,0.3,1)
        _HeadWidthPx           ("Head Width (px)", Range(0,32)) = 4
        _HeadAdd               ("Head Add Intensity", Range(0,10)) = 2
        _HeadLerp              ("Head Lerp (Blend)", Range(0,1)) = 0.25
        _HeadSharpness         ("Head Sharpness", Range(1,64)) = 3

        // Noise
        _NoiseTex              ("Noise (Optional)", 2D) = "gray" {}
        _NoiseScale            ("Noise Scale", Float) = 3
        _NoiseScroll           ("Noise Scroll (x,y)", Vector) = (0.4, 0.0, 0, 0)
        _NoiseIntensity        ("Noise Intensity", Range(0,2)) = 0.25
        _NoiseContrast         ("Noise Contrast", Range(0,4)) = 1.5
        _NoisePulseSpeed       ("Noise Pulse Speed", Range(0,20)) = 6
        _NoiseProceduralMix    ("Noise Procedural Mix", Range(0,1)) = 0.5
        _NoiseChannel          ("Noise Channel (0=R,1=G,2=B,3=A)", Range(0,3)) = 0
        _NoiseSigned           ("Noise Signed (0/1)", Range(0,1)) = 0
        _NoiseBlendMode        ("Noise Blend (0=Add 1=Mul 2=Overlay)", Range(0,2)) = 0
        _NoiseDebug            ("Noise Debug (0~1)", Range(0,1)) = 0

        // Low HP Flash
        _LowHPThreshold        ("Low HP Threshold", Range(0,1)) = 0.25
        _LowHPFlashSpeed       ("Low HP Flash Speed", Range(0,20)) = 8
        _LowHPFlashColor       ("Low HP Flash Color", Color) = (1,0.15,0.15,1)
        _LowHPFlashStrength    ("Low HP Flash Strength", Range(0,1)) = 0.6

        // Layout / Border / Glow
        _WidthPx               ("Bar Pixel Width", Float) = 300
        _HeightPx              ("Bar Pixel Height", Float) = 32
        _BorderPx              ("Border Width(px)", Range(0,8)) = 2
        _BorderColor           ("Border Color", Color) = (1,1,1,1)
        _SideGlow              ("Side Glow Strength", Range(0,2)) = 0.4

        // Global
        _Brightness            ("Final Brightness", Range(0,10)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // === Constants ===
            static const float SAFE_EPS = 1e-5;

            // === Properties ===
            float  _Health;
            float4 _FillColorA, _FillColorB, _FillSecondary, _BackColor;
            float  _SecondaryMix;

            float4 _EdgeColor;
            float  _EdgeWidthPx, _EdgeSharpness, _EdgeAdd, _EdgeLerp;

            float4 _HeadColor;
            float  _HeadWidthPx, _HeadAdd, _HeadLerp, _HeadSharpness;

            sampler2D _NoiseTex; float4 _NoiseTex_ST;
            float  _NoiseScale, _NoiseIntensity, _NoiseContrast, _NoisePulseSpeed, _NoiseProceduralMix;
            float2 _NoiseScroll;
            float  _NoiseChannel, _NoiseSigned, _NoiseBlendMode, _NoiseDebug;

            float4 _LowHPFlashColor;
            float  _LowHPThreshold, _LowHPFlashSpeed, _LowHPFlashStrength;

            float  _WidthPx, _HeightPx, _BorderPx;
            float4 _BorderColor;
            float  _SideGlow, _Brightness;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f      { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            // ---- Noise Helpers ----
            float hash21(float2 p){
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }
            float noise2(float2 p){
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i+float2(1,0));
                float c = hash21(i+float2(0,1));
                float d = hash21(i+float2(1,1));
                float2 u = f*f*(3-2*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            // 내부 UV 계산
            void ComputeInnerUV(float2 uv, out float2 innerUV, out float2 px, out float innerW, out float innerH){
                px = float2(uv.x * _WidthPx, uv.y * _HeightPx);
                innerW = max(1.0, _WidthPx  - _BorderPx*2);
                innerH = max(1.0, _HeightPx - _BorderPx*2);
                innerUV.x = (px.x - _BorderPx) / innerW;
                innerUV.y = (px.y - _BorderPx) / innerH;
                innerUV = saturate(innerUV);
            }

            // 경계(highlight) 마스크: rightEdge(HP 끝) / leftEdge(시작)
            float EdgeMaskRight(float x, float hp, float normWidth, float sharp){
                if(hp <= 0 || normWidth <= 0) return 0;
                float dist = hp - x;
                float m = saturate(1 - dist / max(normWidth, SAFE_EPS));
                m *= step(x, hp);
                return pow(m, sharp);
            }
            float EdgeMaskLeft(float x, float hp, float normWidth, float sharp){
                if(hp <= 0 || normWidth <= 0) return 0;
                float m = saturate(1 - x / max(normWidth, SAFE_EPS));
                m *= step(x, hp);
                return pow(m, sharp);
            }

            // 노이즈 샘플 (채운 영역 내부)
            float SampleFillNoise(float2 innerUV){
                float2 nUV = innerUV * _NoiseScale + _NoiseScroll * _Time.y;
                float4 t = tex2D(_NoiseTex, nUV);
                // 채널 선택
                int ch = (int)round(_NoiseChannel);
                ch = clamp(ch,0,3);
                float texN = (ch==0? t.r : (ch==1? t.g : (ch==2? t.b : t.a)));

                float procN = noise2(nUV * 1.7 + _Time.y * 0.25);
                float n = lerp(texN, procN, _NoiseProceduralMix);

                // 콘트라스트 & 펄스
                float pulse = 0.5 + 0.5 * sin(_Time.y * _NoisePulseSpeed);
                n = pow(saturate(n), _NoiseContrast) * (0.75 + 0.25 * pulse);

                // Signed 옵션
                if (_NoiseSigned > 0.5)
                    n = (n - 0.5) * 2.0; // -1 ~ 1 근사

                // 상하 페이드
                float yFade = 1 - pow(abs(innerUV.y - 0.5) * 2, 1.5);
                return n * yFade;
            }

            // Fill Gradient + Secondary
            float3 ComputeFillColor(float x, float hp){
                float3 baseG = lerp(_FillColorA.rgb, _FillColorB.rgb, x);
                float secondaryFactor = saturate(x * (0.5 + hp * 0.5));
                baseG = lerp(baseG, _FillSecondary.rgb, secondaryFactor * _SecondaryMix);
                return baseG;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 uv = saturate(i.uv);

                // 내부 좌표계
                float2 innerUV, px;
                float innerW, innerH;
                ComputeInnerUV(uv, innerUV, px, innerW, innerH);

                float hp = saturate(_Health);

                // Border 마스크
                float borderMask =
                    step(px.x, _BorderPx) + step(_WidthPx  - _BorderPx, px.x) +
                    step(px.y, _BorderPx) + step(_HeightPx - _BorderPx, px.y);
                borderMask = saturate(borderMask);

                // 기본 색
                float3 fillCol = ComputeFillColor(innerUV.x, hp);
                float3 col = (innerUV.x <= hp) ? fillCol : _BackColor.rgb;

                // Noise (채운 영역)
                if(_NoiseIntensity > 0 && hp > 0 && innerUV.x <= hp){
                    float n = SampleFillNoise(innerUV);

                    // 블렌드 모드
                    int mode = (int)round(_NoiseBlendMode);
                    float3 baseFill = fillCol;

                    float3 noiseApplied;
                    if (mode == 0) {
                        // Add 또는 Signed Add
                        // Signed일 때 n 범위 -1~1 → 중심 0
                        noiseApplied = col + n * _NoiseIntensity * baseFill;
                    } else if (mode == 1) {
                        // Multiply: n(0~1) 전제로, Signed이면 (n*0.5+0.5) 재정규화
                        float nm = (_NoiseSigned > 0.5) ? saturate(n * 0.5 + 0.5) : saturate(n);
                        noiseApplied = col * lerp(1.0, nm, _NoiseIntensity);
                    } else { 
                        // Overlay-ish: 밝은 영역은 더 밝게, 어두운 영역은 곱
                        float ns = (_NoiseSigned > 0.5) ? saturate(n * 0.5 + 0.5) : saturate(n);
                        float3 overlay = (col < 0.5) ? (2 * col * ns) : (1 - 2 * (1 - col) * (1 - ns));
                        noiseApplied = lerp(col, overlay, _NoiseIntensity);
                    }

                    col = noiseApplied;

                    // Debug: 노이즈만 보기 (슬라이더 값 만큼 노이즈 그레이로 페이드)
                    if (_NoiseDebug > 0.001){
                        float show = saturate(_NoiseDebug);
                        float vis = (_NoiseSigned > 0.5) ? saturate((n * 0.5) + 0.5) : saturate(n);
                        col = lerp(col, float3(vis,vis,vis), show);
                    }
                }

                // Low HP Flash
                if(hp > 0 && hp <= _LowHPThreshold && _LowHPFlashStrength > 0){
                    float flash = (sin(_Time.y * _LowHPFlashSpeed) + 1)*0.5;
                    flash = smoothstep(0,1,flash);
                    float danger = 1 - hp / max(_LowHPThreshold, SAFE_EPS);
                    col = lerp(col, _LowHPFlashColor.rgb, flash * danger * _LowHPFlashStrength);
                }

                // Side Glow (좌/우 끝)
                if(_SideGlow > 0){
                    float g = 1 - min(innerUV.x, 1 - innerUV.x) * 2;
                    g = pow(saturate(g), 2) * _SideGlow * 0.35;
                    col += g;
                }

                // Edge Highlight (HP 끝)
                if(_EdgeWidthPx > 0 && _EdgeAdd > 0 && hp > 0){
                    float normW = _EdgeWidthPx / innerW;
                    normW = min(normW, hp * 0.5); // 과도 확장 방지
                    float mask = EdgeMaskRight(innerUV.x, hp, normW, _EdgeSharpness);
                    if(mask > 0){
                        col = lerp(col, _EdgeColor.rgb, mask * _EdgeLerp);
                        col += _EdgeColor.rgb * _EdgeAdd * mask;
                    }
                }

                // Head Highlight (시작)
                if(_HeadWidthPx > 0 && _HeadAdd > 0 && hp > 0){
                    float normHW = _HeadWidthPx / innerW;
                    normHW = min(normHW, hp);
                    float hMask = EdgeMaskLeft(innerUV.x, hp, normHW, _HeadSharpness);
                    if(hMask > 0){
                        col = lerp(col, _HeadColor.rgb, hMask * _HeadLerp);
                        col += _HeadColor.rgb * _HeadAdd * hMask;
                    }
                }

                // Border 최우선
                if(borderMask > 0) col = _BorderColor.rgb;

                col *= _Brightness;
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}