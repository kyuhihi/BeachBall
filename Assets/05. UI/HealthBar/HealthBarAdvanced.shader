Shader "Custom/UI/HealthBarAdvanced_Refactored"
{
    Properties
    {
        _Health                ("Health (0-1)", Range(0,1)) = 1
        // Shape
        _ShapeMode             ("Shape Mode (0=Rect 1=Parallelogram 2=TrimEnds)", Range(0,2)) = 0
        _SkewXPx               ("Parallelogram Skew X (px)", Float) = 30
        _LeftTopTrim           ("Left Top Trim", Range(0,0.4)) = 0.08
        _LeftBottomTrim        ("Left Bottom Trim", Range(0,0.4)) = 0.02
        _RightTopTrim          ("Right Top Trim", Range(0,0.4)) = 0.02
        _RightBottomTrim       ("Right Bottom Trim", Range(0,0.4)) = 0.08
        // ==== NEW: Outer Border ====
        _OuterBorderPx         ("Outer Border Width(px)", Range(0,16)) = 4
        _OuterBorderColor      ("Outer Border Color", Color) = (0,0,0,1)
        _OuterBorderSoftPx     ("Outer Border Soft(px)", Range(0,8)) = 1
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
        _Brightness            ("Final Brightness", Range(0,10)) = 1.0

        // ... 추가: 좌우 반전 옵션
        _FlipShapeX            ("Flip Shape X (Mirror Geometry)", Float) = 0
        _FlipFillX             ("Flip Fill Direction", Float) = 0
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

            static const float SAFE_EPS = 1e-5;

            float  _Health;

            // Shape
            float  _ShapeMode;
            float  _SkewXPx;
            float  _LeftTopTrim, _LeftBottomTrim, _RightTopTrim, _RightBottomTrim;

            // Fill / Grad
            float4 _FillColorA, _FillColorB, _FillSecondary, _BackColor;
            float  _SecondaryMix;

            // Highlights
            float4 _EdgeColor;
            float  _EdgeWidthPx, _EdgeSharpness, _EdgeAdd, _EdgeLerp;
            float4 _HeadColor;
            float  _HeadWidthPx, _HeadAdd, _HeadLerp, _HeadSharpness;

            // Noise
            sampler2D _NoiseTex; float4 _NoiseTex_ST;
            float  _NoiseScale, _NoiseIntensity, _NoiseContrast, _NoisePulseSpeed, _NoiseProceduralMix;
            float2 _NoiseScroll;
            float  _NoiseChannel, _NoiseSigned, _NoiseBlendMode, _NoiseDebug;

            // Low HP
            float4 _LowHPFlashColor;
            float  _LowHPThreshold, _LowHPFlashSpeed, _LowHPFlashStrength;

            // Layout
            float  _WidthPx, _HeightPx, _BorderPx;
            float4 _BorderColor;
            // NEW outer
            float  _OuterBorderPx;
            float4 _OuterBorderColor;
            float  _OuterBorderSoftPx;
            float  _SideGlow, _Brightness;

            // 추가: 좌우 반전 옵션
            float  _FlipShapeX;
            float  _FlipFillX;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v){
                // Parallelogram 모드(1)면 vertex.x 기울이기
                if (_ShapeMode > 0.5 && _ShapeMode < 1.5) {
                    float skewNorm = _SkewXPx / max(1.0, _WidthPx); // px → 정규화 비율
                    v.vertex.x += (v.uv.y - 0.5) * skewNorm;        // 중심 기준 상/하 이동량
                }
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            // --- Helpers ---
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

            // --- ComputeInnerUV 교체: (Outer + Inner) 총 테두리 제외 후 내부 UV 산출 ---
            void ComputeInnerUV(float2 uv, out float2 innerUV, out float2 px,
                                out float innerW, out float innerH,
                                out float totalBorderPx)
            {
                px = float2(uv.x * _WidthPx, uv.y * _HeightPx);
                totalBorderPx = _OuterBorderPx + _BorderPx;
                innerW = max(1.0, _WidthPx  - totalBorderPx * 2);
                innerH = max(1.0, _HeightPx - totalBorderPx * 2);
                innerUV.x = (px.x - totalBorderPx) / innerW;
                innerUV.y = (px.y - totalBorderPx) / innerH;
                innerUV = saturate(innerUV);
            }

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

            float SampleFillNoise(float2 iuv){
                float2 nUV = iuv * _NoiseScale + _NoiseScroll * _Time.y;
                float4 t = tex2D(_NoiseTex, nUV);
                int ch = (int)round(_NoiseChannel); ch = clamp(ch,0,3);
                float texN = (ch==0? t.r : (ch==1? t.g : (ch==2? t.b : t.a)));
                float procN = noise2(nUV * 1.7 + _Time.y * 0.25);
                float n = lerp(texN, procN, _NoiseProceduralMix);
                float pulse = 0.5 + 0.5 * sin(_Time.y * _NoisePulseSpeed);
                n = pow(saturate(n), _NoiseContrast) * (0.75 + 0.25 * pulse);
                if (_NoiseSigned > 0.5) n = (n - 0.5) * 2.0;
                float yFade = 1 - pow(abs(iuv.y - 0.5) * 2, 1.5);
                return n * yFade;
            }

            float3 ComputeFillColor(float x, float hp){
                float3 baseG = lerp(_FillColorA.rgb, _FillColorB.rgb, x);
                float secondaryFactor = saturate(x * (0.5 + hp * 0.5));
                baseG = lerp(baseG, _FillSecondary.rgb, secondaryFactor * _SecondaryMix);
                return baseG;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 uv = saturate(i.uv);

                float2 innerUV, px;
                float innerW, innerH;
                float totalBorderPx;
                ComputeInnerUV(uv, innerUV, px, innerW, innerH, totalBorderPx);

                float hp = saturate(_Health);

                // TrimEnds 모드(2)에서 좌/우 사선 폴리곤 마스크 계산
                float logicalX = innerUV.x;
                float2 shapeUV = innerUV;

                // (1) 형상만 좌우 반전 (회전된 오브젝트용)
                if (_FlipShapeX > 0.5)
                    shapeUV.x = 1.0 - shapeUV.x;

                if (_ShapeMode > 1.5) {
                    // y에 따라 좌/우 Trim (shapeUV 사용)
                    float leftTrim  = lerp(_LeftBottomTrim,  _LeftTopTrim,  shapeUV.y);
                    float rightTrim = lerp(_RightBottomTrim, _RightTopTrim, shapeUV.y);

                    float minX = leftTrim;
                    float maxX = 1.0 - rightTrim;

                    if (shapeUV.x < minX || shapeUV.x > maxX) discard;

                    float midLeft  = 0.5 * (_LeftBottomTrim  + _LeftTopTrim);
                    float midRight = 0.5 * (_RightBottomTrim + _RightTopTrim);
                    float minMid = midLeft;
                    float maxMid = 1.0 - midRight;

                    logicalX = (shapeUV.x - minMid) / max(SAFE_EPS, (maxMid - minMid));
                    logicalX = saturate(logicalX);
                }
                else {
                    logicalX = shapeUV.x;
                }

                // (2) Fill / HP 방향까지 반전 (필요할 때만)
                if (_FlipFillX > 0.5)
                    logicalX = 1.0 - logicalX;

                // ====== Border 계산 (Outer + Inner) ======
                // 전체 사각 기준 거리
                float distL = px.x;
                float distR = _WidthPx - px.x;
                float distB = px.y;
                float distT = _HeightPx - px.y;
                float minRectDist = min(min(distL, distR), min(distB, distT));

                // TrimEnds 모드에서는 좌우 사선 반영 (상/하는 동일)
                if (_ShapeMode > 1.5) {
                    float leftTrimY   = lerp(_LeftBottomTrim,  _LeftTopTrim,  innerUV.y);
                    float rightTrimY  = lerp(_RightBottomTrim, _RightTopTrim, innerUV.y);
                    // innerUV.x 는 (totalBorder 제거 후) 0~1
                    float distLeftNorm  = innerUV.x - leftTrimY;
                    float distRightNorm = (1.0 - rightTrimY) - innerUV.x;
                    // 음수는 discard 됐으므로 양수
                    distLeftNorm  = max(0, distLeftNorm);
                    distRightNorm = max(0, distRightNorm);
                    float distLeftPx  = distLeftNorm  * innerW + _OuterBorderPx; // 내부 시작점 오프셋 보정
                    float distRightPx = distRightNorm * innerW + _OuterBorderPx;
                    // 위/아래는 사선 없음: innerUV.y * innerH 등 사용
                    float distBottomPx = innerUV.y * innerH + _OuterBorderPx;
                    float distTopPx    = (1 - innerUV.y) * innerH + _OuterBorderPx;
                    minRectDist = min(min(distLeftPx, distRightPx), min(distBottomPx, distTopPx));
                }

                // Outer / Inner 구분
                float outerW = _OuterBorderPx;
                float innerWBorder = _BorderPx;
                float outerMask = step(minRectDist, outerW + SAFE_EPS); // 외곽선 영역
                float innerMask = step(outerW + SAFE_EPS, minRectDist) *
                                  step(minRectDist, outerW + innerWBorder + SAFE_EPS); // 내부(기존) 테두리

                // Outer soft (알파/강도 조절)
                float outerSoft = 1;
                if (_OuterBorderSoftPx > 0.001 && outerMask > 0.0) {
                    // minRectDist 0~outerW -> 가장자리; soft 범위: (outerW - soft)~outerW
                    float edgeStart = max(0.0, outerW - _OuterBorderSoftPx);
                    outerSoft = 1 - smoothstep(edgeStart, outerW, minRectDist);
                }

                // Fill 계산
                float3 fillCol = ComputeFillColor(logicalX, hp);
                float3 col = (logicalX <= hp) ? fillCol : _BackColor.rgb;

                // Noise
                if(_NoiseIntensity > 0 && hp > 0 && logicalX <= hp){
                    float n = SampleFillNoise(float2(logicalX, innerUV.y));
                    int mode = (int)round(_NoiseBlendMode);
                    float3 baseFill = fillCol;
                    if (mode == 0) {
                        col = col + n * _NoiseIntensity * baseFill;
                    } else if (mode == 1) {
                        float nm = (_NoiseSigned > 0.5) ? saturate(n * 0.5 + 0.5) : saturate(n);
                        col = col * lerp(1.0, nm, _NoiseIntensity);
                    } else {
                        float ns = (_NoiseSigned > 0.5) ? saturate(n * 0.5 + 0.5) : saturate(n);
                        float3 overlay = (col < 0.5) ? (2 * col * ns) : (1 - 2 * (1 - col) * (1 - ns));
                        col = lerp(col, overlay, _NoiseIntensity);
                    }
                    if (_NoiseDebug > 0.001){
                        float show = saturate(_NoiseDebug);
                        float vis = (_NoiseSigned > 0.5) ? saturate(n * 0.5 + 0.5) : saturate(n);
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

                // Side Glow (Shape 모드에 따라 좌/우 길이 변하나 단순 innerUV.x 사용)
                if(_SideGlow > 0){
                    float g = 1 - min(innerUV.x, 1 - innerUV.x) * 2;
                    g = pow(saturate(g), 2) * _SideGlow * 0.35;
                    col += g;
                }

                // Edge Highlight (HP 끝) - logicalX 기준
                if(_EdgeWidthPx > 0 && _EdgeAdd > 0 && hp > 0){
                    float normW = _EdgeWidthPx / innerW;
                    normW = min(normW, hp * 0.5);
                    float mask = EdgeMaskRight(logicalX, hp, normW, _EdgeSharpness);
                    if(mask > 0){
                        col = lerp(col, _EdgeColor.rgb, mask * _EdgeLerp);
                        col += _EdgeColor.rgb * _EdgeAdd * mask;
                    }
                }

                // Head Highlight (시작)
                if(_HeadWidthPx > 0 && _HeadAdd > 0 && hp > 0){
                    float normHW = _HeadWidthPx / innerW;
                    normHW = min(normHW, hp);
                    float hMask = EdgeMaskLeft(logicalX, hp, normHW, _HeadSharpness);
                    if(hMask > 0){
                        col = lerp(col, _HeadColor.rgb, hMask * _HeadLerp);
                        col += _HeadColor.rgb * _HeadAdd * hMask;
                    }
                }

                // 적용 순서: Outer > Inner > (내용)
                if (outerMask > 0) {
                    col = lerp(col, _OuterBorderColor.rgb, outerSoft); // soft edge
                } 
                else if (innerMask > 0) {
                    col = _BorderColor.rgb;
                }

                col *= _Brightness;
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}