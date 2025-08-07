Shader "Custom/TennisNet"
{
    Properties
    {
        _MainColor ("Net Color", Color) = (0, 0, 0, 1)
        _Thickness ("Line Thickness", Range(0.001, 0.1)) = 0.02
        _Tiling ("Grid Tiling", Vector) = (10, 10, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" }
        LOD 100
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        AlphaToMask On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _MainColor;
            float _Thickness;
            float4 _Tiling;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 gridUV = frac(i.uv); // 0~1 นÝบน

                float lineU = step(gridUV.x, _Thickness) + step(1.0 - gridUV.x, _Thickness);
                float lineV = step(gridUV.y, _Thickness) + step(1.0 - gridUV.y, _Thickness);
                float isLine = saturate(lineU + lineV);

                if (isLine < 1.0) discard;

                return _MainColor;
            }
            ENDCG
        }
    }
    FallBack Off
}
