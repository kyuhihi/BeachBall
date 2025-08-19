Shader "Custom/URPOutline"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.05
        _OutlineIntensity ("Outline Intensity", Range(0,10)) = 2.0

    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass // Outline Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;
            float  _OutlineIntensity;  // ¹à±â

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 norm = normalize(IN.normalOS);
                float3 pos = IN.positionOS.xyz + norm * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(float4(pos, 1.0));
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb * _OutlineIntensity, 1);
            }
            ENDHLSL
        }

        Pass // ±âº» »ö Pass
        {
            Name "Base"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _BaseColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
