// Built - in RP 전용. 텍스처 없어도 Tint 색만으로 동작.
// Magenta(분홍) → 컴파일 에러 / 머티리얼 셰이더 미할당 / 그래픽스 파이프라인 불일치.
// 이 버전은 Surface Shader 단일 Pass (GrabPass, 투명패스 없음) 로 안정화.

Shader "Custom/URP/MeltWaveClip_World"
{
    Properties
    {
        _BaseColor        ("Base Color", Color) = (1,0.6,0.2,1)
        _MeltColor        ("Melt Color", Color) = (0.9,0.15,0.02,1)
        _MeltAmount       ("Melt Amount (0-1)", Range(0,1)) = 0

        _HeightMin        ("Height Min (Local Y)", Float) = 0
        _HeightMax        ("Height Max (Local Y)", Float) = 1

        _WaveAmpFrac      ("Wave Amplitude Fraction", Range(0,0.5)) = 0.02
        _WaveFreq         ("Wave Frequency", Float) = 6
        _WaveSpeed        ("Wave Speed", Float) = 1.2
        _ExtraWaveWorld   ("Extra Wave World (Abs Units)", Float) = 0

        _EdgeFeatherFrac  ("Edge Feather Fraction", Range(0.0001,0.3)) = 0.04

        _FlowSpeed        ("Flow Speed", Float) = 0.6
        _FlowScale        ("Flow Vertical Scale", Float) = 3
        _FlowNoiseScale   ("Flow Noise Scale", Float) = 5
        _FlowNoiseAmp     ("Flow Noise Amp", Float) = 0.4
        _MeltColorPower   ("Melt Color Power", Range(0.2,4)) = 1.3

        _BypassClip       ("Debug (Show Red Above)", Float) = 0
        _TopFillColor     ("Top Fill Color", Color) = (0,0,0,1)

        // -------- Outline --------
        [Header(Outline)]
        [HDR]_OutlineColor        ("Outline Color", Color) = (0,0,0,1)
        _OutlineIntensity         ("Outline Intensity", Range(0,10)) = 2
        _OutlineWidthWorld        ("Outline Width World", Range(0,0.2)) = 0.02
        _OutlineWidthPixels       ("Outline Width Screen (px)", Range(0,20)) = 4
        _OutlineUseScreenSpace    ("Use Screen Space Width (0/1)", Float) = 0
        _OutlineZOffset           ("Outline Z Offset", Range(-0.1,0.1)) = 0
        _OutlineAlpha             ("Outline Alpha", Range(0,1)) = 1
        _OutlineUseVertexColor    ("Multiply Vertex Color (0/1)", Float) = 0
        _OutlineClipWithMelt      ("Clip With Melt (0/1)", Float) = 1
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        // ================= Outline Pass =================
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MeltColor;
                float _MeltAmount;
                float _HeightMin;
                float _HeightMax;
                float _WaveAmpFrac;
                float _WaveFreq;
                float _WaveSpeed;
                float _ExtraWaveWorld;
                float _EdgeFeatherFrac;
                float _FlowSpeed;
                float _FlowScale;
                float _FlowNoiseScale;
                float _FlowNoiseAmp;
                float _MeltColorPower;
                float _BypassClip;
                float4 _TopFillColor;

                // Outline
                float4 _OutlineColor;
                float  _OutlineIntensity;
                float  _OutlineWidthWorld;
                float  _OutlineWidthPixels;
                float  _OutlineUseScreenSpace;
                float  _OutlineZOffset;
                float  _OutlineAlpha;
                float  _OutlineUseVertexColor;
                float  _OutlineClipWithMelt;
            CBUFFER_END

            struct Attributes{
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                float4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings{
                float4 positionHCS:SV_POSITION;
                float3 worldPos:TEXCOORD0;
                float3 localPos:TEXCOORD1;
                float4 vColor:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float hash11(float p){
                p=frac(p*0.1031); p*=p+33.33; p*=p+p; return frac(p);
            }
            inline float noise2(float2 p){
                float2 i=floor(p), f=frac(p);
                float a=hash11(dot(i,float2(1,57)));
                float b=hash11(dot(i+float2(1,0),float2(1,57)));
                float c=hash11(dot(i+float2(0,1),float2(1,57)));
                float d=hash11(dot(i+float2(1,1),float2(1,57)));
                float2 u=f*f*(3-2*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float WorldSizePerPixel(float viewZ){
                float proj11 = UNITY_MATRIX_P._m11;
                return (2.0 * viewZ) / (proj11 * _ScreenParams.y);
            }

            Varyings vert(Attributes IN){
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));

                float3 viewPos = TransformWorldToView(worldPos);
                float viewDepth = -viewPos.z;

                float widthWorld = (_OutlineUseScreenSpace > 0.5)
                    ? WorldSizePerPixel(viewDepth) * _OutlineWidthPixels
                    : _OutlineWidthWorld;

                worldPos += worldNormal * widthWorld;

                float4 posCS = TransformWorldToHClip(worldPos);
                posCS.z += _OutlineZOffset * posCS.w;

                o.positionHCS = posCS;
                o.worldPos = worldPos;
                o.localPos = IN.positionOS.xyz;
                o.vColor = IN.color;
                return o;
            }

            half4 frag(Varyings IN):SV_Target
            {
                // Melt 클리핑(옵션)
                if (_OutlineClipWithMelt > 0.5)
                {
                    float3 sy = float3(unity_ObjectToWorld._21, unity_ObjectToWorld._22, unity_ObjectToWorld._23);
                    float scaleY = max(length(sy), 1e-8);
                    float spanLocal = _HeightMax - _HeightMin;
                    float spanWorld = max(spanLocal * scaleY, 1e-8);
                    float pivotWorldY = unity_ObjectToWorld._24;
                    float bottomWorld = pivotWorldY + _HeightMin * scaleY;
                    float topWorld    = pivotWorldY + _HeightMax * scaleY;

                    float melt = saturate(_MeltAmount);
                    float frontWorld = lerp(topWorld, bottomWorld, melt);

                    // 파동 동일 적용
                    float waveAmpWorld = spanWorld * _WaveAmpFrac + _ExtraWaveWorld;
                    if (waveAmpWorld > 0){
                        float s1 = sin(IN.localPos.x * _WaveFreq + _Time.y * _WaveSpeed);
                        float s2 = sin(IN.localPos.z * (_WaveFreq * 0.8) + _Time.y * _WaveSpeed * 1.13);
                        float lowN = noise2(IN.localPos.xz * 0.4 + _Time.y * 0.18) - 0.5;
                        float wave = (s1+s2)*0.5 + lowN;
                        frontWorld += wave * waveAmpWorld;
                    }

                    float diff = frontWorld - IN.worldPos.y;
                    float featherWorld = spanWorld * _EdgeFeatherFrac;
                    float mask = saturate(diff / max(featherWorld,1e-8));
                    if (mask <= 0) discard;
                }

                float4 col = _OutlineColor * _OutlineIntensity;
                if (_OutlineUseVertexColor > 0.5)
                    col.rgb *= IN.vColor.rgb;
                col.a *= _OutlineAlpha;
                if (col.a <= 0) discard;
                return col;
            }
            ENDHLSL
        }

        // ================= Main Melt Pass =================
        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MeltColor;
                float _MeltAmount;
                float _HeightMin;
                float _HeightMax;
                float _WaveAmpFrac;
                float _WaveFreq;
                float _WaveSpeed;
                float _ExtraWaveWorld;
                float _EdgeFeatherFrac;
                float _FlowSpeed;
                float _FlowScale;
                float _FlowNoiseScale;
                float _FlowNoiseAmp;
                float _MeltColorPower;
                float _BypassClip;
                float4 _TopFillColor;
            CBUFFER_END

            struct Attributes{
                float4 positionOS:POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings{
                float4 positionHCS:SV_POSITION;
                float3 localPos:TEXCOORD0;
                float3 worldPos:TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float hash11(float p){
                p=frac(p*0.1031); p*=p+33.33; p*=p+p; return frac(p);
            }
            inline float noise2(float2 p){
                float2 i=floor(p), f=frac(p);
                float a=hash11(dot(i,float2(1,57)));
                float b=hash11(dot(i+float2(1,0),float2(1,57)));
                float c=hash11(dot(i+float2(0,1),float2(1,57)));
                float d=hash11(dot(i+float2(1,1),float2(1,57)));
                float2 u=f*f*(3-2*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            Varyings vert(Attributes IN){
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 lp = IN.positionOS.xyz;
                o.localPos = lp;
                o.worldPos = TransformObjectToWorld(lp);
                o.positionHCS = TransformWorldToHClip(o.worldPos);
                return o;
            }

            half4 frag(Varyings IN):SV_Target
            {
                float3 sy = float3(unity_ObjectToWorld._21, unity_ObjectToWorld._22, unity_ObjectToWorld._23);
                float scaleY = max(length(sy), 1e-8);
                float spanLocal = _HeightMax - _HeightMin;
                float spanWorld = max(spanLocal * scaleY, 1e-8);
                float pivotWorldY = unity_ObjectToWorld._24;
                float bottomWorld = pivotWorldY + _HeightMin * scaleY;
                float topWorld    = pivotWorldY + _HeightMax * scaleY;

                float melt = saturate(_MeltAmount);
                float frontWorld = lerp(topWorld, bottomWorld, melt);

                float waveAmpWorld = spanWorld * _WaveAmpFrac + _ExtraWaveWorld;
                float wave = 0;
                if (waveAmpWorld > 0){
                    float s1 = sin(IN.localPos.x * _WaveFreq + _Time.y * _WaveSpeed);
                    float s2 = sin(IN.localPos.z * (_WaveFreq * 0.8) + _Time.y * _WaveSpeed * 1.13);
                    float lowN = noise2(IN.localPos.xz * 0.4 + _Time.y * 0.18) - 0.5;
                    wave = (s1+s2)*0.5 + lowN;
                    wave *= waveAmpWorld;
                }

                float frontW = frontWorld + wave;
                float worldY = IN.worldPos.y;
                float diff = frontW - worldY;
                float featherWorld = spanWorld * _EdgeFeatherFrac;
                float mask = saturate(diff / max(featherWorld,1e-8));

                if (mask <= 0.0)
                {
                    if (_BypassClip > 0.5) return half4(1,0,0,1);
                    return half4(_TopFillColor.rgb, 1);
                }

                float depth = saturate((frontW - worldY)/spanWorld);
                float flowT = (worldY - bottomWorld)/spanWorld * _FlowScale - _Time.y * _FlowSpeed;
                float lineVal = sin(flowT * 6)*0.5 + 0.5;
                float n = noise2(float2(IN.localPos.x, flowT) * _FlowNoiseScale);
                float flowPattern = lineVal * 0.6 + n * _FlowNoiseAmp;

                float meltFactor = pow(saturate(depth + flowPattern*0.4), _MeltColorPower) * mask;

                float3 col = lerp(_BaseColor.rgb, _MeltColor.rgb, meltFactor);
                return half4(col,1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
