Shader "Custom/MeshOutline"
{
    Properties
    {
        // Base
        _BaseColor          ("Base Color", Color) = (1,1,1,1)
        _BaseMap            ("Base Map", 2D) = "white" {}
        _BaseAlpha          ("Base Alpha", Range(0,1)) = 1

        // Outline
        [HDR]_OutlineColor  ("Outline Color", Color) = (0,0,0,1)
        _OutlineIntensity   ("Outline Intensity", Range(0,10)) = 2.0
        _OutlineWidthWorld  ("Outline Width World (units)", Range(0,0.2)) = 0.02
        _OutlineWidthPixels ("Outline Width Screen (px)", Range(0,20)) = 4
        _OutlineUseScreenSpace ("Use Screen Space Width (0/1)", Range(0,1)) = 0
        _OutlineZOffset     ("Outline Z Offset", Range(-0.1,0.1)) = 0
        _OutlineAlpha       ("Outline Alpha", Range(0,1)) = 1
        _OutlineUseVertexColor ("Multiply Vertex Colors (0/1)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        // ---------- Outline Pass ----------
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
            };

            float4 _OutlineColor;
            float  _OutlineIntensity;
            float  _OutlineWidthWorld;
            float  _OutlineWidthPixels;
            float  _OutlineUseScreenSpace;
            float  _OutlineZOffset;
            float  _OutlineAlpha;
            float  _OutlineUseVertexColor;

            float WorldSizePerPixel(float viewZ)
            {
                // viewZ ?? (??? ?)
                float proj11 = UNITY_MATRIX_P._m11; // 1/tan(fov/2)
                return (2.0 * viewZ) / (proj11 * _ScreenParams.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));

                float3 viewPos = TransformWorldToView(worldPos);
                float  viewDepth = -viewPos.z;

                float widthWorld = (_OutlineUseScreenSpace > 0.5)
                    ? WorldSizePerPixel(viewDepth) * _OutlineWidthPixels
                    : _OutlineWidthWorld;

                float3 extrudedWorldPos = worldPos + worldNormal * widthWorld;

                float4 posCS = TransformWorldToHClip(extrudedWorldPos);
                posCS.z += _OutlineZOffset * posCS.w;

                OUT.positionHCS = posCS;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float4 col = _OutlineColor * _OutlineIntensity;
                if (_OutlineUseVertexColor > 0.5)
                    col.rgb *= IN.color.rgb;
                col.a *= _OutlineAlpha;
                return col;
            }
            ENDHLSL
        }

        // ---------- Base Pass ----------
        Pass
        {
            Name "Base"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _BaseAlpha;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float4 c = tex * _BaseColor * IN.color;
                c.a *= _BaseAlpha;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
