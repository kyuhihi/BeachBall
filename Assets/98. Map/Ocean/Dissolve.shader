Shader "Custom/SimpleDissolveColor"
{
    Properties
    {
        _BaseColor     ("Base Color", Color) = (1,1,1,1)
        _DissolveTex   ("Dissolve Texture", 2D) = "gray" {}
        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0.0
        _EdgeColor     ("Edge Color", Color) = (1,1,1,1)
        _EdgeWidth     ("Edge Width", Range(0,0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float  _DissolveAmount;
                float  _EdgeWidth;
            CBUFFER_END

            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, IN.uv).r;

                // µðÁ¹ºê ÄÆ
                clip(noise - _DissolveAmount);

                // ¿§Áö °­Á¶
                float edge = smoothstep(_DissolveAmount, _DissolveAmount + _EdgeWidth, noise);
                float4 col = lerp(_EdgeColor, _BaseColor, edge);
                return col;
            }
            ENDHLSL
        }
    }
}
