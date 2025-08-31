Shader "UI/CenterHoleWithTexture"
{
    Properties
    {
        _MainTex("Background Texture", 2D) = "white" {}
        _CenterTex("Center Texture", 2D) = "white" {}
        _HoleSize("Hole Size", Range(0, 2.1)) = 2.1//Start 1.59
        _BackgroundColor("Background Color", Color) = (0,0,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanvasOverlay"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            Name "CenterHolePass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CenterTex;
            float _HoleSize;
            float4 _BackgroundColor;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // �߽� ���� ��ǥ (-0.5 ~ 0.5)
                float2 centeredUV = i.uv - 0.5;

                // ȸ�� ����: _HoleSize�� ���� 0~360��(2*PI)�� ��ȭ
                float angle = _HoleSize * 6.2831853; // 0~2PI (360��)
                float s = sin(angle);
                float c = cos(angle);

                // ȸ�� ����
                float2 rotatedUV;
                rotatedUV.x = centeredUV.x * c - centeredUV.y * s;
                rotatedUV.y = centeredUV.x * s + centeredUV.y * c;

                // �׸� ���� ���� ��� (ȸ���� ��ǥ ����)
                float halfHole = _HoleSize * 0.5;

                bool inHole = abs(rotatedUV.x) <= halfHole && abs(rotatedUV.y) <= halfHole;

                if (inHole)
                {
                    // �׸� ����: CenterTex ��� (���� 1)
                    float2 centerUV = (rotatedUV + halfHole) / (_HoleSize); // 0~1�� ����ȭ
                    half4 centerCol = tex2D(_CenterTex, centerUV);
                    centerCol.a = 1 - centerCol.a;

                    return half4(_BackgroundColor.rgb, centerCol.a);
                }
                
                else
                {
                    // �ٱ� ����: ���� �Ǵ� MainTex
                    half4 bgCol = tex2D(_MainTex, i.uv) * _BackgroundColor;
                    return half4(bgCol.rgb, bgCol.a);
                }
            }
            ENDHLSL
        }
    }
}
