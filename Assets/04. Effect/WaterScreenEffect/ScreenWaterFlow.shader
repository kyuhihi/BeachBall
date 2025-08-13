Shader "Custom/ScreenWaterFlow"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _FlowSpeed("Flow Speed", Float) = 0.5
        _Distortion("Distortion Strength", Float) = 0.02
        _Color("Water Color", Color) = (0.0,0.4,0.8,0.3)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float _FlowSpeed;
            float _Distortion;
            float4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;

                // Noise 텍스쳐 기반 UV 왜곡
                float noise = tex2D(_NoiseTex, i.uv * 5 + float2(t, t)).r;
                float2 uv = i.uv + float2(noise * _Distortion, 0);

                return _Color;
            }
            ENDHLSL
        }
    }
}
