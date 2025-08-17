Shader "Custom/PlayerBottomCircle"
{
    Properties
    {
        _Color       ("Color (RGB) + Alpha", Color) = (1,1,1,1)
        _Intensity   ("Brightness", Range(5, 10)) = 5 
        _RingDensity ("Ring Density", Range(1,200)) = 40
        _Speed       ("Scroll Speed", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float  _Intensity;
            float  _RingDensity;
            float  _Speed;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float2 uv  : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 d  = i.uv - float2(0.5, 0.5);
                float  rn = length(d) * 2.0;

                float soft   = fwidth(rn) * 2.0;
                float inner  = smoothstep(0.6 - soft, 0.6 + soft, rn);
                float outer  = 1.0 - smoothstep(1.0 - soft, 1.0 + soft, rn);
                float window = saturate(inner * outer);

                float phase = -_Time.y * _Speed;
                float s     = 0.5 + 0.5 * sin(rn * _RingDensity * 6.2831853 + phase);
                float lines = smoothstep(0.85, 1.0, s);

                // 최소 강도 바닥값(완전히 사라지지 않게)
                lines = max(lines, 0.08); // 필요시 0.05~0.12 사이로 조절

                float alpha = window * lines * _Color.a;
                // RGB만 밝게(알파는 그대로)
                return float4(_Color.rgb * _Intensity, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
