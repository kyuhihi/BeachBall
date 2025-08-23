Shader "Custom/UIDashSegments"
{
    Properties
    {
        // ----- Layout -----
        _WidthPx        ("Total Width(px)", Float) = 400
        _HeightPx       ("Height(px)", Float) = 48
        _LabelWidthPx   ("Label Width(px)", Float) = 80

        // ----- Segments -----
        _SegmentCount   ("Segment Count", Range(1,12)) = 4
        _SpacingPx      ("Segment Spacing(px)", Range(0,30)) = 12
        _CornerR        ("Corner Radius(px)", Range(0,60)) = 20
        _InnerPadPx     ("Inner Padding(px)", Range(0,20)) = 4
        _EdgeSoftPx     ("Edge Softness(px)", Range(0,4)) = 1.2

        // ----- Fill (Normal Mode) -----
        _Value          ("Value 0-1", Range(0,1)) = 1
        [HDR]_FillA     ("Active Color A", Color) = (1,0.6,0.1,1)
        [HDR]_FillB     ("Active Color B", Color) = (1,0.85,0.3,1)
        [HDR]_InactiveA ("Inactive Color A", Color) = (0.2,0.2,0.22,1)
        [HDR]_InactiveB ("Inactive Color B", Color) = (0.3,0.3,0.32,1)

        // ----- Water Mode -----
        _WaterMode          ("Water Mode (0=Normal 1=Water)", Range(0,1)) = 0
        _WaterSequential    ("Water Sequential (0=AllRise 1=PerSegment)", Range(0,1)) = 1
        [HDR]_WaterBottomCol     ("Water Bottom Color", Color) = (0.9,0.45,0.05,1)
        [HDR]_WaterTopCol        ("Water Top Color", Color) = (1.0,0.8,0.3,1)
        [HDR]_WaterSurfaceCol    ("Water Surface Color", Color) = (1.2,1.1,0.9,1)
        [HDR]_WaterFoamCol       ("Water Foam Color", Color) = (1,1,1,1)
        _WaterSurfaceWidthPx("Surface Highlight Width(px)", Range(0,10)) = 3
        _WaterFoamWidthPx   ("Foam Width(px)", Range(0,8)) = 2
        _WaveAmpPx          ("Wave Amplitude(px)", Range(0,20)) = 6
        _WaveLen            ("Wave Length", Range(0.1,10)) = 2
        _WaveSpeed          ("Wave Speed", Range(-10,10)) = 2
        _WaterEdgeDarken    ("Side Darken (0~1)", Range(0,1)) = 0.25
        _WaterColShimmerAmp   ("Water Column Shimmer Amp", Range(0,1)) = 0.35
        _WaterColShimmerFreq  ("Water Column Shimmer Freq", Range(0.5,12)) = 5
        _WaterColShimmerSpeed ("Water Column Shimmer Speed", Range(-12,12)) = 3

        // ----- Noise (공통 적용) -----
        _NoiseMode      ("Noise Mode (0=Off 1=Add 2=Mul)", Range(0,2)) = 0
        _NoiseTex       ("Noise Texture", 2D) = "gray" {}
        _NoiseScale     ("Noise Scale (px)", Float) = 120
        _NoiseSpeed     ("Noise Speed", Float) = 0.2
        _NoiseDistortAmp("Noise Distort Amplitude(px)", Range(0,10)) = 2
        _NoiseColorAmp  ("Noise Color Amp", Range(0,2)) = 0.5

        // ----- Label Panel -----
        [HDR]_LabelColor ("Label Panel Color", Color) = (0.07,0.07,0.08,1)
        _LabelCornerR   ("Label Corner R(px)", Range(0,40)) = 12

        // ----- Effects -----
        [HDR]_GlowColor  ("Glow Color", Color) = (1,0.8,0.4,1)
        _EdgeGlow       ("Edge Glow Intensity", Range(0,3)) = 1.2
        _InnerHighlight ("Inner Highlight (0~1)", Range(0,1)) = 0.4

        // ----- Global -----
        [HDR]_BackgroundCol ("Background Color", Color) = (0,0,0,0)
        _GammaFix       ("Gamma Adjust", Range(0.5,2)) = 1
        _Brightness     ("Brightness", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define TWO_PI 6.2831853

            // Layout / segment
            float _WidthPx,_HeightPx,_LabelWidthPx;
            float _SegmentCount,_SpacingPx,_CornerR,_InnerPadPx,_EdgeSoftPx;

            // Normal fill
            float _Value;
            float4 _FillA,_FillB,_InactiveA,_InactiveB;

            // Water
            float _WaterMode,_WaterSequential;
            float4 _WaterBottomCol,_WaterTopCol,_WaterSurfaceCol,_WaterFoamCol;
            float _WaterSurfaceWidthPx,_WaterFoamWidthPx;
            float _WaveAmpPx,_WaveLen,_WaveSpeed;
            float _WaterEdgeDarken;
            float _WaterColShimmerAmp,_WaterColShimmerFreq,_WaterColShimmerSpeed;

            // Noise
            float _NoiseMode;
            sampler2D _NoiseTex;
            float _NoiseScale,_NoiseSpeed,_NoiseDistortAmp,_NoiseColorAmp;

            // Effects
            float4 _GlowColor;
            float _EdgeGlow,_InnerHighlight;

            // Label
            float4 _LabelColor; float _LabelCornerR;

            // Global
            float4 _BackgroundCol; float _GammaFix,_Brightness;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v){ v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }

            // ---- SDF helpers ----
            float sdRoundRect(float2 p,float2 halfSize,float r){
                float2 q = abs(p) - (halfSize - r);
                return min(max(q.x,q.y),0.0) + length(max(q,0.0)) - r;
            }
            float sdCapsuleH(float2 p,float2 halfSize,float r){
                return sdRoundRect(p, halfSize, min(r, halfSize.y));
            }

            // ---- Utility ----
            float4 InactiveColor(float yNorm){ return lerp(_InactiveA,_InactiveB,yNorm); }
            float4 ActiveGradient(float xNorm,bool active){
                float4 a = active ? _FillA : _InactiveA;
                float4 b = active ? _FillB : _InactiveB;
                return lerp(a,b,xNorm);
            }
            float EdgeGlow(float d,float radius){
                if (_EdgeGlow<=0.001) return 0;
                float g = saturate(1 - abs(d)/(radius+1));
                return pow(g,3) * _EdgeGlow;
            }
            float4 ApplyGamma(float4 c){
                c.rgb = pow(saturate(c.rgb * _Brightness), 1/_GammaFix);
                return c;
            }

            // Wave front (좌->우 채움 경계)
            float ComputeWaterFront(float segFillFrac,float yNorm,int idx){
                if (_WaveAmpPx <= 0.0001) return segFillFrac;
                float t = _Time.y * _WaveSpeed;
                float w = sin(yNorm * (TWO_PI/_WaveLen) + t + idx*0.9);
                float front = segFillFrac + w * (_WaveAmpPx * 0.0015); // 간단 정규화
                return clamp(front,0,1);
            }

            // Noise 샘플 (0~1)
            float SampleNoise(float2 px){
                if (_NoiseMode < 0.5) return 0.5;
                float2 uvn = (px / max(1.0,_NoiseScale)) + float2(_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed*0.73);
                return tex2D(_NoiseTex, uvn).r; // 단일 채널 사용
            }

            void ApplyNoiseColor(inout float3 rgb,float n){
                if (_NoiseMode < 0.5 || _NoiseColorAmp<=0) return;
                float v = n - 0.5; // -0.5~0.5
                if (_NoiseMode < 1.5){
                    // Add
                    rgb += v * _NoiseColorAmp;
                } else {
                    // Mul
                    rgb *= (1 + v * _NoiseColorAmp);
                }
            }

            float4 frag(v2f i):SV_Target
            {
                float2 uv = saturate(i.uv);
                float2 px = float2(uv.x*_WidthPx, uv.y*_HeightPx);
                float totalH = _HeightPx;

                float4 col = _BackgroundCol;

                // Label
                if (_LabelWidthPx>0.5 && px.x <= _LabelWidthPx){
                    float2 c = float2(_LabelWidthPx*0.5,totalH*0.5);
                    float2 hs= float2(_LabelWidthPx*0.5,totalH*0.5);
                    float dL = sdRoundRect(px-c,hs,_LabelCornerR);
                    float aL = saturate(0.5 - dL / max(_EdgeSoftPx,0.001));
                    col = lerp(col,_LabelColor,aL);
                    col.a = max(col.a,aL);
                }

                // Segment layout
                int segCount = max(1,(int)round(_SegmentCount));
                float segAreaX0 = _LabelWidthPx;
                float segAreaX1 = _WidthPx;
                if (px.x < segAreaX0 || px.x>segAreaX1 || px.y < _InnerPadPx || px.y > totalH-_InnerPadPx)
                    return ApplyGamma(col);

                float segAreaW = max(1.0, segAreaX1 - segAreaX0);
                float totalSpacing = _SpacingPx * (segCount-1);
                float perW = (segAreaW - totalSpacing - _InnerPadPx*2)/segCount;
                perW = max(1.0,perW);
                float segH = totalH - _InnerPadPx*2;
                float2 segHalf = float2(perW*0.5, segH*0.5);
                float radius = _CornerR;
                float stride = perW + _SpacingPx;

                float localX = px.x - segAreaX0 - _InnerPadPx;
                int idx = (int)floor(localX / stride);
                if (idx<0 || idx>=segCount) return ApplyGamma(col);

                float segStart = segAreaX0 + _InnerPadPx + idx*stride;
                float segCenterX = segStart + perW*0.5;
                float2 p = float2(px.x - segCenterX, px.y - totalH*0.5);

                float d = sdCapsuleH(p, segHalf, radius);
                float edgeSoft = max(0.5,_EdgeSoftPx);
                float shapeMask = saturate(0.5 - d / edgeSoft);
                if (shapeMask <= 0.001) return ApplyGamma(col);

                float yNormFull = (p.y + segHalf.y)/max(1.0,segH);
                float xNormLocal = saturate( (p.x + segHalf.x)/(segHalf.x*2.0) );

                // Inactive base
                float4 inact = InactiveColor(yNormFull);
                float inactA = shapeMask * inact.a;
                col.rgb = lerp(col.rgb, inact.rgb, inactA);
                col.a   = max(col.a, inactA);

                // 공통 Noise (채워진 부분 계산 전에 미리)
                float noiseVal = SampleNoise(px);

                if (_WaterMode > 0.5)
                {
                    float value = saturate(_Value);
                    float segFillFrac;
                    if (_WaterSequential>0.5){
                        float totalFill = value * segCount;
                        int fullSegs = (int)floor(totalFill);
                        float partial = frac(totalFill);
                        segFillFrac = (idx < fullSegs) ? 1 : (idx == fullSegs ? partial : 0);
                    } else {
                        segFillFrac = value;
                    }
                    segFillFrac = saturate(segFillFrac);

                    if (segFillFrac > 0.0001)
                    {
                        float dynamicFront = ComputeWaterFront(segFillFrac, yNormFull, idx);

                        // Noise 경계 왜곡
                        if (_NoiseMode>0.5 && _NoiseDistortAmp>0){
                            float distortFrac = (_NoiseDistortAmp / perW) * (noiseVal - 0.5);
                            dynamicFront = clamp(dynamicFront + distortFrac, 0,1);
                        }

                        float insideWater = step(xNormLocal, dynamicFront);

                        float4 waterCol = lerp(_WaterBottomCol,_WaterTopCol,yNormFull);

                        // Column shimmer
                        if (_WaterColShimmerAmp > 0.001){
                            float colWave = sin(xNormLocal * _WaterColShimmerFreq * TWO_PI
                                                + _Time.y * _WaterColShimmerSpeed + idx*0.7);
                            colWave = colWave*0.5 + 0.5;
                            float edgeDist = abs(xNormLocal - dynamicFront);
                            float nearEdge = saturate(1 - edgeDist / ( (_WaveAmpPx / max(1.0, perW)) + 0.02));
                            waterCol.rgb += colWave * nearEdge * _WaterColShimmerAmp * 0.4;
                        }

                        float boundaryDistFrac = abs(xNormLocal - dynamicFront);
                        float boundaryDistPx = boundaryDistFrac * (segHalf.x*2.0);

                        if (_WaterSurfaceWidthPx>0){
                            float surfMask = saturate(1 - boundaryDistPx / max(0.001,_WaterSurfaceWidthPx));
                            waterCol.rgb = lerp(waterCol.rgb, _WaterSurfaceCol.rgb, pow(surfMask,1.4));
                        }
                        if (_WaterFoamWidthPx>0){
                            float foamMask = saturate(1 - boundaryDistPx / max(0.001,_WaterFoamWidthPx));
                            waterCol.rgb = lerp(waterCol.rgb, _WaterFoamCol.rgb, pow(foamMask,3));
                        }

                        float sideNorm = abs(p.y)/segHalf.y;
                        waterCol.rgb *= (1 - pow(sideNorm,2) * _WaterEdgeDarken);

                        // Noise 색 변조 (물 내부만)
                        ApplyNoiseColor(waterCol.rgb, noiseVal);

                        float glowF = EdgeGlow(d,radius);
                        if (glowF>0) waterCol.rgb += _GlowColor.rgb * glowF * 0.35;

                        float waterAlpha = shapeMask * insideWater;
                        col.rgb = lerp(col.rgb, waterCol.rgb, waterAlpha);
                        col.a   = max(col.a, waterAlpha * waterCol.a);
                    }
                    else
                    {
                        float glowF = EdgeGlow(d,radius);
                        if (glowF>0) col.rgb += _GlowColor.rgb * glowF * 0.25;
                    }
                }
                else
                {
                    // Normal Mode
                    float value = saturate(_Value);
                    float totalFill = value * segCount;
                    int fullSegs = (int)floor(totalFill);
                    float partialFrac = frac(totalFill);

                    bool isFull = (idx < fullSegs);
                    bool isPartial = (idx == fullSegs) && (partialFrac>0.0001) && (fullSegs<segCount);

                    float4 baseCol = ActiveGradient(xNormLocal, isFull);

                    if (isPartial){
                        float cutoff = partialFrac;
                        float feather = edgeSoft/(segHalf.x*2.0);
                        float partialMask = smoothstep(cutoff - feather, cutoff + feather, xNormLocal);
                        float4 inactiveBase = ActiveGradient(xNormLocal,false);
                        baseCol = lerp(inactiveBase, baseCol, partialMask);
                    }

                    if (_InnerHighlight>0.001){
                        float vNorm = (p.y/(segHalf.y*2.0))+0.5;
                        float highlight = pow(1 - abs(vNorm-0.5)*2, 2.5);
                        baseCol.rgb += highlight * _InnerHighlight * 0.5;
                    }

                    // Noise 변조 (채워진 부분만)
                    if (isFull || isPartial){
                        ApplyNoiseColor(baseCol.rgb, noiseVal);
                    }

                    float glowF = EdgeGlow(d,radius);
                    if (glowF>0) baseCol.rgb += _GlowColor.rgb * glowF * 0.5;

                    float alpha = shapeMask;
                    col.rgb = lerp(col.rgb, baseCol.rgb, alpha);
                    col.a   = max(col.a, alpha * baseCol.a);
                }

                return ApplyGamma(col);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
