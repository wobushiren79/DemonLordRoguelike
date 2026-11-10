Shader "Game/Fight/ForestMoonlight_1"
{
    Properties
    {
        [HDR] _BeamColor ("月光颜色", Color) = (0.28, 0.79, 1.13, 1)
        _Opacity ("空中光束强度", Range(0, 1)) = 0.43
        [HDR] _GroundColor ("地面柔光颜色", Color) = (0.17, 0.61, 0.74, 1)
        _GroundStrength ("地面柔光亮度", Range(0, 1)) = 0.26
        _BeamTightness ("光束收束强度（越高越窄）", Range(0, 1)) = 0
        _CycleSeconds ("漂移周期（秒）", Range(4, 30)) = 16
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-1" "RenderType" = "Transparent" }
        Pass
        {
            Name "Moonlight"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                half4 _GroundColor;
                half _Opacity;
                half _GroundStrength;
                half _BeamTightness;
                float _CycleSeconds;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
                half2 motion : TEXCOORD1;
                half ground : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                // 顶点红色通道为空中 0、地面 1。
                output.ground = input.color.r;
                // 地面收束较少，让落光自然衔接。
                half focusScale = 1.0h + _BeamTightness * lerp(1.0h, 0.6h, output.ground);
                output.uv.x = (input.uv.x * 2.0h - 1.0h) * focusScale;
                float phase = _Time.y * (TWO_PI / _CycleSeconds);
                output.motion.x = 0.055h * sin(phase - input.uv.y * 1.4h);
                output.motion.y = input.color.a * (0.94h + 0.06h * sin(phase + 0.8h));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half crossBeam = input.uv.x;
                half edge = 1.0h - smoothstep(0.16h, 1.0h, abs(crossBeam));
                half core = 1.0h - smoothstep(0.0h, 0.5h, abs(crossBeam + input.motion.x));
                half fadeStart = lerp(0.68h, 0.54h, input.ground);
                half fadeEnd = lerp(1.0h, 0.94h, input.ground);
                half lengthFade = smoothstep(0.0h, 0.14h, input.uv.y) * (1.0h - smoothstep(fadeStart, fadeEnd, input.uv.y));
                half mask = input.motion.y * lengthFade * edge * (0.62h + 0.38h * core);
                half3 lightColor = lerp(_BeamColor.rgb, _GroundColor.rgb, input.ground);
                half strength = lerp(_Opacity, _GroundStrength, input.ground);
                half sourceBias = lerp(lerp(1.60h, 0.80h, input.uv.y), 1.0h, input.ground);
                // 地面柔光接近加色，保留原草纹与棋盘反差。
                half extinction = strength * lerp(0.40h, 0.08h, input.ground);
                return half4(lightColor * (strength * mask * sourceBias), extinction * mask);
            }
            ENDHLSL
        }
    }
}