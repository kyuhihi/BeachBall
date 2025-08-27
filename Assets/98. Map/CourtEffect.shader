// 물방울이 떨어져 동심원 다중 리플이 안쪽에서 계속 생기며 바깥으로 진행.
// _CurrentRadius = 파동 전면(프론트) 위치. 프론트 안쪽(dist <= _CurrentRadius)에 다중 사인 파형 형성.
Shader "Custom/URP/CourtEffectPaintingRipple"
{
    Properties
    {
        _ImpactPos        ("Impact World Pos", Vector) = (0,0,0,0)
        _CurrentRadius    ("Current Wave Radius", Float) = 0
        _MaxRadius        ("Max Radius (Fade Out)", Float) = 15

        _Freq             ("Wave Frequency", Float) = 12
        _PhaseSpeed       ("Phase Scroll Speed", Float) = 2.0

        _Amplitude        ("Base Amplitude", Float) = 0.035
        _EdgeBoost        ("Front Edge Boost", Float) = 1.6
        _RadialDamping    ("Radial Damping (inside)", Float) = 0.25
        _GrowthDamping    ("Growth Damping (as radius grows)", Float) = 0.05

        _FrontWidth       ("Front Edge Width", Float) = 1.0
        _FrontFeather     ("Front Edge Feather", Float) = 0.5
        _InteriorFadePow  ("Interior Fade Power", Float) = 1.2

        _NoiseScale       ("Noise Scale", Float) = 1.1
        _NoiseSpeed       ("Noise Speed", Float) = 0.4
        _NoiseStrength    ("Noise Strength", Float) = 0.20

        _DistortStrength  ("Distortion Strength", Float) = 1.0
        _TangentialRatio  ("Tangential Mix (0=Pure Radial)", Range(0,1)) = 0.25

        _EdgeColor        ("Edge Highlight Color", Color) = (0.6,0.85,1,1)
        _EdgeColorStrength("Edge Color Strength", Float) = 0.5

        _Alpha            ("Global Alpha", Range(0,1)) = 0.85
        _MinAlpha         ("Min Alpha Floor", Range(0,1)) = 0.10
        _AlphaBoost       ("Alpha Boost", Float) = 1.2

        _PlaneMode        ("Plane Mode (0=XY 1=XZ)", Float) = 0
        _UseLocal         ("Use Local Pos (0/1)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Ripple"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Opaque Texture (URP 설정에서 활성화 필수)
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float4 _ImpactPos;
            float  _CurrentRadius;
            float  _MaxRadius;

            float  _Freq;
            float  _PhaseSpeed;

            float  _Amplitude;
            float  _EdgeBoost;
            float  _RadialDamping;
            float  _GrowthDamping;

            float  _FrontWidth;
            float  _FrontFeather;
            float  _InteriorFadePow;

            float  _NoiseScale;
            float  _NoiseSpeed;
            float  _NoiseStrength;

            float  _DistortStrength;
            float  _TangentialRatio;

            float4 _EdgeColor;
            float  _EdgeColorStrength;

            float  _Alpha;
            float  _MinAlpha;
            float  _AlphaBoost;

            float  _PlaneMode;
            float  _UseLocal;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 localPos   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
            };

            float2 hash2(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float noise2d(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f*f*(3.0 - 2.0*f);

                float2 a = hash2(i);
                float2 b = hash2(i + float2(1,0));
                float2 c = hash2(i + float2(0,1));
                float2 d = hash2(i + float2(1,1));

                float va = dot(a, f);
                float vb = dot(b, f - float2(1,0));
                float vc = dot(c, f - float2(0,1));
                float vd = dot(d, f - float2(1,1));

                return lerp(lerp(va,vb,u.x), lerp(vc,vd,u.x), u.y)*0.5+0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.localPos = IN.positionOS.xyz;
                float3 w = TransformObjectToWorld(IN.positionOS.xyz);
                o.worldPos = w;
                o.positionCS = TransformWorldToHClip(w);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            float2 selectPlane(float3 worldPos, float3 localPos, bool useLocal, bool planeXZ)
            {
                if (useLocal)
                    return planeXZ ? float2(localPos.x, localPos.z) : localPos.xy;
                else
                    return planeXZ ? float2(worldPos.x, worldPos.z) : worldPos.xy;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // 1. 좌표/거리
                bool planeXZ  = (_PlaneMode > 0.5);
                bool useLocal = (_UseLocal > 0.5);
                float2 p = selectPlane(IN.worldPos, IN.localPos, useLocal, planeXZ);
                float2 c = planeXZ ? float2(_ImpactPos.x, _ImpactPos.z) : _ImpactPos.xy;

                float dist = distance(p, c);

                // 2. 범위 / 종료 조건
                if (_CurrentRadius <= 0.0001 || dist > _CurrentRadius || _CurrentRadius > _MaxRadius)
                {
                    // 아무 영향 없음 (투명)
                    return float4(0,0,0,0);
                }

                // 3. 기본 파형 계산
                float frontBehind = _CurrentRadius - dist; // 0=전면, 커질수록 중심 방향
                float time = _Time.y;
                // 내부 다중 링: frontBehind * freq 로 동심 사인, 시간(phase scroll) 포함
                float phase = frontBehind * _Freq - time * _PhaseSpeed * _Freq;
                float baseWave = sin(phase);

                // 4. 감쇠
                float radialDamp  = exp(-frontBehind * _RadialDamping);
                float growthDamp  = exp(-_CurrentRadius * _GrowthDamping);
                float damp        = radialDamp * growthDamp;

                // 5. 전면 강조 마스크
                float frontEdge = 1.0 - smoothstep(_FrontWidth - _FrontFeather,
                                                   _FrontWidth,
                                                   frontBehind);
                // 내부 살리기 (중심으로 갈수록 감소): interior = (frontBehind/_CurrentRadius)^pow
                float interior = pow(saturate(frontBehind / max(_CurrentRadius, 1e-4)), _InteriorFadePow);
                float visibility = max(frontEdge, interior);

                // 6. 노이즈
                float2 dir = dist > 1e-5 ? (p - c) / dist : float2(0,0);
                float2 tangent = float2(-dir.y, dir.x);
                float n = noise2d(p * _NoiseScale + dir * (time * _NoiseSpeed));
                n = (n - 0.5) * 2.0 * _NoiseStrength;

                // 7. 전체 파형 (노이즈 추가)
                float wave = (baseWave + n) * damp;

                // 8. 굴절 벡터 (radial + tangential 비율)
                float edgeBoost = 1.0 + frontEdge * (_EdgeBoost - 1.0);
                float amp = _Amplitude * edgeBoost;

                float2 radialOffset     = dir * wave * amp;
                float2 tangentialOffset = tangent * wave * amp * _TangentialRatio;
                float2 offset = (radialOffset + tangentialOffset) * _DistortStrength;

                // 9. Opaque Texture UV
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float2 uvDistorted = uv + offset;
                float4 sceneCol = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvDistorted);

                // 10. 엣지 컬러 (frontEdge 기반)
                float edgeFactor = pow(frontEdge, 1.5);
                float3 finalRGB = sceneCol.rgb + _EdgeColor.rgb * edgeFactor * _EdgeColorStrength;

                // 11. 알파
                float waveAlpha = saturate(abs(wave) * _AlphaBoost);
                float alpha = _Alpha * visibility * max(waveAlpha, _MinAlpha);

                return float4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
