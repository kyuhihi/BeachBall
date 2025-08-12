Shader "Universal Render Pipeline/SimpleDissolveColorLit"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (1,1,1,1)
        _DissolveTex    ("Dissolve Texture", 2D) = "gray" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0.0
        _EdgeColor      ("Edge Color", Color) = (1,1,1,1)
        _EdgeWidth      ("Edge Width", Range(0,0.2)) = 0.05
        _MultipleBaseColor ("Multiple Base Color", Range(0,5)) = 1.0

        // Lit ????
        _SpecColor   ("Specular Color", Color) = (1,1,1,1)
        _Smoothness  ("Smoothness", Range(0,1)) = 0.5

        // Surface Options (Shader Graph ??)
        [Enum(UnityEngine.Rendering.CullMode)]         _Cull    ("Render Face (Cull)", Float) = 2      // Back
        [Enum(UnityEngine.Rendering.BlendMode)]        _SrcBlend("Src Blend", Float) = 5               // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)]        _DstBlend("Dst Blend", Float) = 10              // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CompareFunction)]  _ZTest   ("Depth Test", Float) = 4              // LEqual
        [Toggle]                                       _ZWrite  ("Depth Write", Float) = 0
        [Toggle(_ALPHACLIP)]                           _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull   [_Cull]
            ZWrite [_ZWrite]
            ZTest  [_ZTest]
            Blend  [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // ??
            #pragma multi_compile _ _ALPHACLIP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord : TEXCOORD3;
                #endif
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float  _DissolveAmount;
                float  _EdgeWidth;
                float  _Cutoff;
                float  _MultipleBaseColor;

                float4 _SpecColor;
                float  _Smoothness;
            CBUFFER_END

            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS   = TransformWorldToHClip(positionWS);
                OUT.positionWS    = positionWS;
                OUT.normalWS      = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv            = IN.uv;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    OUT.shadowCoord = TransformWorldToShadowCoord(positionWS);
                #endif
                return OUT;
            }

            // Blinn-Phong ????? ?? ??
            float SpecPowerFromSmoothness(float s)
            {
                // 0~1 -> 8~512 ?? ??
                return lerp(8.0, 512.0, saturate(s));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if(_DissolveAmount>0.99f)
                    discard;
                // ???
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // ??? ???
                float noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, IN.uv).r;

                // ??? ? + ????
                clip(noise - _DissolveAmount);
                #ifdef _ALPHACLIP
                    clip(_BaseColor.a - _Cutoff);
                #endif

                // ?? ?? + ??
                float edge = smoothstep(_DissolveAmount, _DissolveAmount + _EdgeWidth, noise);
                float4 baseCol = lerp(_EdgeColor, _BaseColor, edge);
                baseCol.rgb *= _MultipleBaseColor;

                // ?? ???
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    Light mainLight = GetMainLight(IN.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float3 L = mainLight.direction;
                float ndotl = saturate(dot(N, L));

                // ??? + SH ????
                float3 diffuse = baseCol.rgb * (mainLight.color * ndotl);
                float3 ambient = baseCol.rgb * SampleSH(N);

                // ?? ??? ????(Blinn-Phong)
                float3 H = normalize(L + V);
                float specPower = SpecPowerFromSmoothness(_Smoothness);
                float spec = pow(saturate(dot(N, H)), specPower);
                float3 specular = _SpecColor.rgb * spec * mainLight.color;

                // ?? ???
                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, IN.positionWS);
                    float3 Ladd = addLight.direction;
                    float ndotlAdd = saturate(dot(N, Ladd));
                    diffuse  += baseCol.rgb * (addLight.color * ndotlAdd);

                    float3 Hadd = normalize(Ladd + V);
                    float specAdd = pow(saturate(dot(N, Hadd)), specPower);
                    specular += _SpecColor.rgb * specAdd * addLight.color;
                }
                #endif

                float3 color = ambient + diffuse + specular;

                return float4(color, baseCol.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
