Shader "Game/PodiumMetal"
{
    Properties
    {
        [KeywordEnum(Gold,Silver)] _MetalType("Metal Type", Float) = 0
        _BaseTint("Optional Tint (RGB)", Color) = (1,1,1,1)
        _Roughness("Roughness (0=Polished,1=Diffusey)", Range(0.02,1)) = 0.25
        _AnisoStrength("Directional Brushed Strength", Range(0,1)) = 0.35
        _FresnelBoost("Fresnel Boost", Range(0,3)) = 1.2
        _Brightness("Overall Brightness", Range(0,3)) = 1.0
        _NoiseScale("Subtle Noise Scale", Range(0.5,10)) = 4
        _NoiseStrength("Noise Strength", Range(0,0.3)) = 0.08
        _EdgeWearHeight("World Y Start Wear", Float) = 0.4
        _EdgeWearFade("Wear Fade Range", Float) = 0.6
        _WearStrength("Wear Strength", Range(0,1)) = 0.4
        _Occlusion("Fake AO", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP 다중 조명/쉐도우 기본 매크로
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ _LIGHT_LAYERS

            #pragma shader_feature _METALTYPE_GOLD _METALTYPE_SILVER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 tangentWS  : TEXCOORD2;
                float3 bitangentWS: TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTint;
                float _Roughness;
                float _AnisoStrength;
                float _FresnelBoost;
                float _Brightness;
                float _NoiseScale;
                float _NoiseStrength;
                float _EdgeWearHeight;
                float _EdgeWearFade;
                float _WearStrength;
                float _Occlusion;
            CBUFFER_END

            // 해시/노이즈 (아티팩트 최소화용 간단)
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }
            float noise(float3 p)
            {
                float n = dot(p, float3(1,57,113));
                return hash11(n);
            }

            // 브러시 효과용 방향(월드 XZ 평면)
            float brushedPattern(float3 n, float3 posWS, float strength, float scale)
            {
                // 탄젠트 공간 근사: 월드 x 방향
                float dir = dot(normalize(float3(1,0,0)), n);
                float stripe = sin(posWS.z * scale * 6.2831 + dir * 4);
                return stripe * 0.5 * strength;
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS   = nInputs.normalWS;
                o.tangentWS  = nInputs.tangentWS;
                o.bitangentWS= nInputs.bitangentWS;
                return o;
            }

            // GGX NDF
            float MyD_GGX(float a, float NdotH)
            {
                float a2 = a * a;
                float d = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
                return a2 / (PI * d * d + 1e-5);
            }
            float MyV_SmithGGXCorrelated(float a, float NdotV, float NdotL)
            {
                float a2 = a*a;
                float gv = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
                float gl = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);
                return 0.5 / (gv + gl + 1e-5);
            }
            float3 MyFresnelSchlick(float3 F0, float cosTheta)
            {
                return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
            }

            float3 GetMetalBaseColor()
            {
            #if defined(_METALTYPE_SILVER)
                // 실버 (선형 근사)
                float3 metal = float3(0.972, 0.960, 0.915);
            #else
                // 골드
                float3 metal = float3(1.000, 0.766, 0.336);
            #endif
                return metal * _BaseTint.rgb;
            }

            float wearMask(float y)
            {
                // 위쪽 혹은 특정 높이 기준으로 페이드 (단상 위 표면 살짝 마모)
                float t = saturate( (y - _EdgeWearHeight) / max(_EdgeWearFade, 0.0001) );
                return t;
            }

            float3 ApplyLights(float3 albedo, float rough, float3 N, float3 V, float3 posWS)
            {
                float3 color = 0;
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);

                float NdotL = saturate(dot(N,L));
                float NdotV = saturate(dot(N,V));
                float NdotH = saturate(dot(N,H));
                float VdotH = saturate(dot(V,H));

                float a = rough * rough;
                float3 F0 = albedo;

                float3  F  = MyFresnelSchlick(F0, VdotH);
                float   D  = MyD_GGX(a, NdotH);
                float   Vis= MyV_SmithGGXCorrelated(a, NdotV, NdotL);

                float3 spec = D * Vis * F;
                float3 diff = 0;
                float3 brdf = (diff + spec) * NdotL * mainLight.color;
                color += brdf;

            #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                [loop] for (uint i=0;i<count;i++)
                {
                    Light l = GetAdditionalLight(i, posWS);
                    float3 L2 = normalize(l.direction);
                    float3 H2 = normalize(L2 + V);
                    float NdotL2 = saturate(dot(N,L2));
                    float NdotH2 = saturate(dot(N,H2));
                    float VdotH2 = saturate(dot(V,H2));

                    float D2   = MyD_GGX(a, NdotH2);
                    float Vis2 = MyV_SmithGGXCorrelated(a, NdotV, NdotL2);
                    float3 F2  = MyFresnelSchlick(F0, VdotH2);
                    float3 spec2 = D2 * Vis2 * F2;
                    float3 brdf2 = spec2 * NdotL2 * l.color;
                    color += brdf2;
                }
            #endif
                return color;
            }

            float3 EnvReflection(float3 N, float3 V, float rough, float3 F0)
            {
                float3 R = reflect(-V, N);
            #if defined(UNITY_SPECCUBE_LOD_STEPS)
                float mip = rough * UNITY_SPECCUBE_LOD_STEPS;
                float4 env = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, R, mip);
            #else
                float4 env = SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, R);
            #endif
                float3 prefiltered = DecodeHDREnvironment(env, unity_SpecCube0_HDR);
                float3 fres = MyFresnelSchlick(F0, saturate(dot(N, V)));
                return prefiltered * fres;
            }

            float3 SubtleNoise(float3 posWS)
            {
                float n = noise(posWS * _NoiseScale);
                // 2차 가공
                float n2 = noise((posWS + 17.13) * (_NoiseScale * 1.7));
                float m = (n * 0.6 + n2 * 0.4);
                return (m - 0.5).xxx; // -0.5~0.5 근사
            }

            float3 DoAnisotropic(float3 N, float3 T, float3 B, float3 V, float anisoStrength)
            {
                // 뷰 방향 투영 기반 간단한 하이라이트 왜곡
                float3 Vt = normalize(float3(dot(V,T), dot(V,B), dot(V,N)));
                float stretch = saturate(1 - abs(Vt.x)) * anisoStrength;
                return stretch.xxx;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float3 baseMetal = GetMetalBaseColor();

                // 러프니스 / 마모 / 노이즈 반영
                float wear = wearMask(IN.positionWS.y);
                float wearAtten = lerp(1, 1 - _WearStrength, wear);

                float rough = saturate(_Roughness + noise(IN.positionWS * (_NoiseScale*0.5))*_NoiseStrength);
                rough = saturate(rough * wearAtten);

                // 브러시 + 노이즈
                float brushed = brushedPattern(N, IN.positionWS, _AnisoStrength, _NoiseScale*0.15);
                float3 nNoise = SubtleNoise(IN.positionWS) * _NoiseStrength;
                float3 modColor = baseMetal + (brushed + nNoise) * baseMetal;

                // 명암
                float3 lit = ApplyLights(modColor, rough, N, V, IN.positionWS);

                // 환경 반사
                float3 env = EnvReflection(N, V, rough, baseMetal);

                // 프레넬 보강
                float fres = pow(1 - saturate(dot(N,V)), 5) * _FresnelBoost;

                // AO
                float ao = 1 - _Occlusion;

                float3 col = (lit + env * 0.6) * ao;
                col = lerp(col, col * (1 + fres), 0.5);

                col *= _Brightness;

                return float4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
