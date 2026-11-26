Shader "Game/Scene/SeadrakeGalleonSky"
{
    Properties
    {
        [NoScaleOffset] _SkyboxCubemap ("晴空立方体", Cube) = "grey" {}
        _Tint ("天空颜色", Color) = (1, 1, 1, 1)
        _Exposure ("天空曝光", Float) = 1
        _RotateX ("绕 X 轴旋转", Float) = -15
        _RotateY ("绕 Y 轴旋转", Float) = 0
        _RotateZ ("绕 Z 轴旋转", Float) = 0
        _FogAltitude ("全雾仰角（度）", Range(-5, 0)) = -1.2
        _ClearAltitude ("晴空仰角（度）", Range(0.1, 6)) = 2.6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sea horizon"
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_SkyboxCubemap);
            SAMPLER(sampler_SkyboxCubemap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Exposure, _RotateX, _RotateY, _RotateZ, _FogAltitude, _ClearAltitude;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 cubemapDir : TEXCOORD0;
                float3 worldDir : TEXCOORD1;
                float2 band : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.worldDir = TransformObjectToWorldDir(input.positionOS, false);
                output.band = sin(radians(float2(_FogAltitude, _ClearAltitude)));

                // 与项目晴空图一致，依次绕物体 X、Y、Z 轴以右手旋转采样方向。
                float3 s, c;
                sincos(radians(float3(_RotateX, _RotateY, _RotateZ)), s, c);
                float3 d = input.positionOS;
                d.yz = float2(c.x * d.y - s.x * d.z, s.x * d.y + c.x * d.z);
                d.xz = float2(c.y * d.x + s.y * d.z, -s.y * d.x + c.y * d.z);
                d.xy = float2(c.z * d.x - s.z * d.y, s.z * d.x + c.z * d.y);
                output.cubemapDir = d;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 sky = SAMPLE_TEXTURECUBE_LOD(_SkyboxCubemap, sampler_SkyboxCubemap, input.cubemapDir, 0).rgb * _Tint.rgb * _Exposure;
                // 直接沿用水面的 URP 雾色和颜色空间。运行时关闭雾后完整显示原始晴空。
                half3 horizon = MixFog(sky, ComputeFogFactorZ0ToFar(_ProjectionParams.z));
                float clear = smoothstep(input.band.x, input.band.y, normalize(input.worldDir).y);
                return half4(lerp(horizon, sky, clear), 1);
            }
            ENDHLSL
        }
    }
}
