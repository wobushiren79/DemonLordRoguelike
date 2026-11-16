Shader "Game/Fight/FallingBow_1"
{
    Properties
    {
        [HDR] _OuterColor ("外焰颜色", Color) = (1.1, 0.14, 0.02, 1)
        [HDR] _FlowColor ("气流颜色", Color) = (1.25, 0.49, 0.06, 1)
        [HDR] _HotColor ("前缘高光", Color) = (1.8, 1.43, 0.72, 1)
        _Opacity ("气罩不透明度", Range(0, 1)) = 0.82
        _FlowRate ("气流滚动频率", Range(1, 20)) = 11
        _BowShockScale ("气罩尺寸", Range(0.5, 1)) = 1
        [HideInInspector] _AttackModeVisualTime ("战斗表现时间", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+8" }
        Pass
        {
            Name "BowShock"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OuterColor;
                half4 _FlowColor;
                half4 _HotColor;
                half _Opacity;
                half _FlowRate;
                half _BowShockScale;
                float _AttackModeVisualTime;
            CBUFFER_END

            struct Attributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs position = GetVertexPositionInputs(input.position.xyz * _BowShockScale);
                output.position = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float theta = input.uv.x * TWO_PI;
                float dist = input.uv.y;
                // Keep the pulse frequency of the original six-second BowShock particle.
                float phase = _AttackModeVisualTime * (_FlowRate / 6.0) * TWO_PI;
                float fold = sin(theta * 7.0 + dist * 10.0 - phase);
                float crest = sin(theta * 13.0 - dist * 19.0 + phase * 1.24);
                float streak = pow(saturate(0.58 + 0.30 * fold + 0.24 * crest), 4.0);
                float raggedEnd = 0.79 + 0.12 * sin(theta * 5.0 + phase * 0.32) + 0.06 * fold;

                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float tangent = 1.0 - saturate(abs(dot(normalize(input.normalWS), viewDir)));
                float rim = smoothstep(0.18, 0.82, tangent);
                float front = (1.0 - smoothstep(0.26, 0.58, dist)) * smoothstep(0.005, 0.085, dist);
                float bow = 1.0 - smoothstep(0.045, 0.21, abs(dist - (0.20 + 0.035 * fold)));
                float endFade = 1.0 - smoothstep(raggedEnd - 0.21, raggedEnd, dist);
                float edgeFlow = rim * (0.35 + 0.65 * streak);

                float alpha = saturate(front * (0.19 + 0.25 * streak)
                                      + edgeFlow * 0.62
                                      + bow * (0.22 + rim * 0.43));
                alpha *= endFade * smoothstep(0.0, 0.05, dist) * _Opacity;

                half3 color = lerp(_OuterColor.rgb, _FlowColor.rgb, saturate(front * 0.56 + streak * 0.52 + 0.14));
                float whiteRim = saturate(bow * (0.27 + 0.63 * rim) + front * streak * 0.30);
                color = lerp(color, _HotColor.rgb, whiteRim);
                return half4(MixFog(color, input.fog), alpha);
            }
            ENDHLSL
        }
    }
}

