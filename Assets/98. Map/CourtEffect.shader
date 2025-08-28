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

        _Amplitude        ("Base Amplitude (Screen Distort)", Float) = 0.035
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

        // --- Vertex ripple 추가 ---
        _VertexEnable      ("Enable Vertex Ripple (0/1)", Float) = 1
        _VertexAmplitude   ("Vertex Displace Amplitude", Float) = 0.08
        _VertexRadialRatio ("Vertex Radial Mix (0=Normal)", Range(0,1)) = 0.35
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

            // vertex ripple
            float  _VertexEnable;
            float  _VertexAmplitude;
            float  _VertexRadialRatio;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 localPos   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
            };

            // ----------------- Noise -----------------
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

            // ----------------- Helpers -----------------
            float2 selectPlane(float3 worldPos, float3 localPos, bool useLocal, bool planeXZ)
            {
                if (useLocal)
                    return planeXZ ? float2(localPos.x, localPos.z) : localPos.xy;
                else
                    return planeXZ ? float2(worldPos.x, worldPos.z) : worldPos.xy;
            }

            float2 impactOnPlane(bool planeXZ)
            {
                return planeXZ ? float2(_ImpactPos.x, _ImpactPos.z) : _ImpactPos.xy;
            }

            float2 expandToPlane(float2 v2, bool planeXZ)
            {
                // 2D 평면 방향을 3D로 확장(XY or XZ)
                return planeXZ ? float2(v2.x, v2.y) : v2; // helper for casting; not used directly
            }

            // 공통 파형 계산: 프래그/버텍스 모두 사용
            // 반환: wave(노이즈/감쇠 포함), frontEdge(하이라이트용), dir(방사 방향), visibility(알파용)
            void ComputeRipple(float2 p, float2 c, out float wave, out float frontEdge, out float2 dir, out float visibility)
            {
                float dist = distance(p, c);

                // 범위 밖이면 0 반환
                if (_CurrentRadius <= 0.0001 || dist > _CurrentRadius || _CurrentRadius > _MaxRadius)
                {
                    wave = 0; frontEdge = 0; dir = float2(0,0); visibility = 0;
                    return;
                }

                float frontBehind = _CurrentRadius - dist;
                float time = _Time.y;

                float phase    = frontBehind * _Freq - time * _PhaseSpeed * _Freq;
                float baseWave = sin(phase);

                float radialDamp = exp(-frontBehind * _RadialDamping);
                float growthDamp = exp(-_CurrentRadius * _GrowthDamping);
                float damp       = radialDamp * growthDamp;

                // front edge & interior
                frontEdge = 1.0 - smoothstep(_FrontWidth - _FrontFeather, _FrontWidth, frontBehind);
                float interior = pow(saturate(frontBehind / max(_CurrentRadius, 1e-4)), _InteriorFadePow);
                visibility = max(frontEdge, interior);

                dir = dist > 1e-5 ? (p - c) / dist : float2(0,0);

                float n = noise2d(p * _NoiseScale + dir * (time * _NoiseSpeed));
                n = (n - 0.5) * 2.0 * _NoiseStrength;

                wave = (baseWave + n) * damp;
            }

            // 2D 방사방향을 3D로 확장
            float3 Dir2DTo3D(float2 d, bool planeXZ)
            {
                return planeXZ ? float3(d.x, 0, d.y) : float3(d.x, d.y, 0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;

                float3 posOS = IN.positionOS.xyz;
                float3 posWS = TransformObjectToWorld(posOS);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);

                // 평면/좌표 선택
                bool planeXZ  = (_PlaneMode > 0.5);
                bool useLocal = (_UseLocal > 0.5);
                float2 p = selectPlane(posWS, posOS, useLocal, planeXZ);
                float2 c = impactOnPlane(planeXZ);

                // 파형 계산
                float wave, fe, vis; float2 dir2;
                ComputeRipple(p, c, wave, fe, dir2, vis);

                // 버텍스 변위
                if (_VertexEnable > 0.5 && vis > 0.0)
                {
                    float edgeBoost = 1.0 + fe * (_EdgeBoost - 1.0);
                    float ampV = _VertexAmplitude * edgeBoost;

                    float3 radialWS = Dir2DTo3D(dir2, planeXZ);
                    float3 dispWS   = normalize(nrmWS) * (1.0 - _VertexRadialRatio) + normalize(radialWS) * _VertexRadialRatio;
                    dispWS = normalize(dispWS) * (wave * ampV);

                    posWS += dispWS;
                }

                o.worldPos = posWS;
                o.localPos = IN.positionOS.xyz; // 필요 시 로컬 원본도 프래그에서 사용

                o.positionCS = TransformWorldToHClip(posWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                return o;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                bool planeXZ  = (_PlaneMode > 0.5);
                bool useLocal = (_UseLocal > 0.5);
                float2 p = selectPlane(IN.worldPos, IN.localPos, useLocal, planeXZ);
                float2 c = impactOnPlane(planeXZ);

                float wave, frontEdge, visibility;
                float2 dir;
                ComputeRipple(p, c, wave, frontEdge, dir, visibility);

                if (visibility <= 0.0)
                    return float4(0,0,0,0);

                // 화면 굴절(기존 로직)
                float2 tangent = float2(-dir.y, dir.x);
                float edgeBoost = 1.0 + frontEdge * (_EdgeBoost - 1.0);
                float amp = _Amplitude * edgeBoost;

                float2 radialOffset     = dir * wave * amp;
                float2 tangentialOffset = tangent * wave * amp * _TangentialRatio;
                float2 offset = (radialOffset + tangentialOffset) * _DistortStrength;

                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float2 uvDistorted = uv + offset;
                float4 sceneCol = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvDistorted);

                // 엣지 색
                float edgeFactor = pow(frontEdge, 1.5);
                float3 finalRGB = sceneCol.rgb + _EdgeColor.rgb * edgeFactor * _EdgeColorStrength;

                // 알파
                float waveAlpha = saturate(abs(wave) * _AlphaBoost);
                float alpha = _Alpha * visibility * max(waveAlpha, _MinAlpha);

                return float4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
