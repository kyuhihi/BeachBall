Shader "Hidden/NewImageEffectShader"
{
    Properties
    {
        _MainTex        ("Texture", 2D) = "white" {}
        _LineColor      ("Line Color", Color) = (0,0,0,1)

        _UseAlphaMask   ("Use Alpha (0=R,1=Alpha)", Float) = 0
        _Threshold      ("Threshold", Range(0,1)) = 0.5

        _ThicknessFactor("Thickness Factor (fwidth mul)", Range(0.05,2)) = 1
        _Binary         ("Binary (1=Hard Edge)", Float) = 0

        _MultiplySrcAlpha ("Multiply Src Alpha (0/1)", Float) = 1
        _AlphaCutoff    ("Alpha Cut (when UseAlphaMask)", Range(0,1)) = 0

        _DebugMode      ("Debug (0 Off /1 Value /2 Dist /3 Coverage)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _LineColor;
            float  _UseAlphaMask;
            float  _Threshold;
            float  _ThicknessFactor;
            float  _Binary;
            float  _MultiplySrcAlpha;
            float  _AlphaCutoff;
            float  _DebugMode;

            struct appdata {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
            };
            struct v2f {
                float4 pos:SV_POSITION;
                float2 uv:TEXCOORD0;
            };

            v2f vert(appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i):SV_Target
            {
                float4 texc = tex2D(_MainTex, i.uv);
                float val = (_UseAlphaMask > 0.5) ? texc.a : texc.r;

                if (_UseAlphaMask > 0.5 && texc.a < _AlphaCutoff) discard;

                // 화면 미분 기반 폭 (한 픽셀 값 변화량)
                float fw = fwidth(val);              // 주변 픽셀과 값 차이
                fw = max(fw, 1e-6);                  // 0 방지
                float halfWidth = fw * _ThicknessFactor * 0.5;

                float dist = abs(val - _Threshold);  // 임계값으로부터 거리
                float coverage = saturate(1 - dist / halfWidth); // 0~1 커버리지

                // 디버그
                if (_DebugMode > 0.5){
                    if (_DebugMode < 1.5) return float4(val,val,val,1);
                    else if (_DebugMode < 2.5) return float4(dist/dist, dist/dist, dist/dist,1); // 항상 1 (참고용)
                    else return float4(coverage,coverage,coverage,1);
                }

                // Binary 모드: 하드 에지
                if (_Binary > 0.5){
                    if (coverage < 0.5) discard;
                    float a = _LineColor.a;
                    if (_MultiplySrcAlpha > 0.5) a *= texc.a;
                    if (a <= 0.0001) discard;
                    return float4(_LineColor.rgb, a);
                }

                // Soft AA 모드(초미세 두께 가능)
                if (coverage <= 0.0001) discard;
                float aSoft = coverage * _LineColor.a;
                if (_MultiplySrcAlpha > 0.5) aSoft *= texc.a;
                if (aSoft <= 0.0001) discard;

                float3 rgb = _LineColor.rgb;
                return float4(rgb, aSoft);
            }
            ENDHLSL
        }
    }
}
