// Heartfelt(https://www.shadertoy.com/view/ltffzl) - by Martijn Steinrucken aka BigWings - 2017
// countfrolic@gmail.com
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

Shader "Custom/Raindrop" {
    Properties {
        iChannel0("Albedo (RGB)", 2D) = "white" {}
        _RainAmount ("Rain Amount", Range(0,1)) = 0.8
        _DistortionPixels ("Distortion (pixels)", Range(0,30)) = 10
        _Speed ("Speed", Float) = 1.0
        _Opacity ("Opacity (overall alpha)", Range(0,1)) = 0.7   // 추가
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Pass{
            // 투명 블렌딩 설정
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D iChannel0;
            float _RainAmount;
            float _DistortionPixels;
            float _Speed;
            float _Opacity;

            #define S(a, b, t) smoothstep(a, b, t)
            #define CHEAP_NORMALS

            float3 N13(float p) {
                //  from DAVE HOSKINS
                float3 p3 = frac(float3(p, p, p) * float3(.1031, .11369, .13787));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac(float3((p3.x + p3.y)*p3.z, (p3.x + p3.z)*p3.y, (p3.y + p3.z)*p3.x));
            }

            float4 N14(float t) {
                return frac(sin(t*float4(123., 1024., 1456., 264.))*float4(6547., 345., 8799., 1564.));
            }
            float N(float t) {
                return frac(sin(t*12345.564)*7658.76);
            }

            float Saw(float b, float t) {
                return S(0., b, t)*S(1., b, t);
            }

            float2 DropLayer2(float2 uv, float t) {
                float2 UV = uv;

                uv.y += t*0.75;
                float2 a = float2(6., 1.);
                float2 grid = a*2.;
                float2 id = floor(uv*grid);

                float colShift = N(id.x);
                uv.y += colShift;

                id = floor(uv*grid);
                float3 n = N13(id.x*35.2 + id.y*2376.1);
                float2 st = frac(uv*grid) - float2(.5, 0);

                float x = n.x - .5;

                float y = UV.y*20.;
                float wiggle = sin(y + sin(y));
                x += wiggle*(.5 - abs(x))*(n.z - .5);
                x *= .7;
                float ti = frac(t + n.z);
                y = (Saw(.85, ti) - .5)*.9 + .5;
                float2 p = float2(x, y);

                float d = length((st - p)*a.yx);

                float mainDrop = S(.4, .0, d);

                float r = sqrt(S(1., y, st.y));
                float cd = abs(st.x - x);
                float trail = S(.23*r, .15*r*r, cd);
                float trailFront = S(-.02, .02, st.y - y);
                trail *= trailFront*r*r;

                y = UV.y;
                float trail2 = S(.2*r, .0, cd);
                float droplets = max(0., (sin(y*(1. - y)*120.) - st.y))*trail2*trailFront*n.z;
                y = frac(y*10.) + (st.y - .5);
                float dd = length(st - float2(x, y));
                droplets = S(.3, 0., dd);
                float m = mainDrop + droplets*r*trailFront;

                //m += st.x>a.y*.45 || st.y>a.x*.165 ? 1.2 : 0.;
                return float2(m, trail);
            }

            float StaticDrops(float2 uv, float t) {
                uv *= 40.;

                float2 id = floor(uv);
                uv = frac(uv) - .5;
                float3 n = N13(id.x*107.45 + id.y*3543.654);
                float2 p = (n.xy - .5)*.7;
                float d = length(uv - p);

                float fade = Saw(.025, frac(t + n.z));
                float c = S(.3, 0., d)*frac(n.z*10.)*fade;
                return c;
            }

            float2 Drops(float2 uv, float t, float l0, float l1, float l2) {
                float s = StaticDrops(uv, t)*l0;
                float2 m1 = DropLayer2(uv, t)*l1;
                float2 m2 = DropLayer2(uv*1.85, t)*l2;

                float c = s + m1.x + m2.x;
                c = S(.3, 1., c);

                return float2(c, max(m1.y*l0, m2.y*l1));
            }


            fixed4 frag(v2f_img i) : SV_Target{

                // 화면 중심 정규화 좌표
                float2 uv = ((i.uv * _ScreenParams.xy) - .5*_ScreenParams.xy) / _ScreenParams.y;
                float2 UV = i.uv;

                // 고정된 시간/강도
                float T = _Time.y * _Speed;
                float t = T * 0.2;
                float rainAmount = _RainAmount;

                // 줌/하트/스토리 모두 제거
                float zoom = 1.0;
                uv *= zoom;
                UV = (UV - .5) * (1.0) + .5;

                // 레이어 계산
                float staticDrops = S(-.5, 1., rainAmount) * 2.;
                float layer1 = S(.25, .75, rainAmount);
                float layer2 = S(.0,  .5,  rainAmount);

                float2 c = Drops(uv, t, staticDrops, layer1, layer2);

                // 노멀 근사(빠름)
                float2 n = float2(ddx(c.x), ddy(c.x));

                // 픽셀 → UV 스케일로 굴절
                float2 px = 1.0 / _ScreenParams.xy;
                float2 duv = saturate(UV + n * (_DistortionPixels * px));

                float4 col = tex2D(iChannel0, duv);

                // 물방울 마스크 기반 알파(텍스처 알파 대신 사용)
                float dropletMask = saturate(c.x);         // 물방울 영역 0~1
                float a = _Opacity * dropletMask;          // 전체 투명도 조절
                return float4(0,col.g,col.b, a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}