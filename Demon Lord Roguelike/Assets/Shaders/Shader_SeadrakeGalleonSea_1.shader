Shader "Game/Scene/SeadrakeGalleonSea"
{
    Properties
    {
        [Header(Water)]
        _DeepColor ("浪谷颜色", Color) = (0.1490, 0.3922, 0.5412, 1)
        _OceanColor ("海水颜色", Color) = (0.2941, 0.6824, 0.8627, 1)
        _CrestColor ("迎光浪面颜色", Color) = (0.6902, 0.9176, 1, 1)
        _FoamColor ("浪花颜色", Color) = (0.9569, 0.9804, 1, 1)
        _DistanceColor ("远海颜色", Color) = (0.6431, 0.7843, 0.8549, 1)
        [Header(Waves)]
        _WaveScale ("波纹密度", Range(0.4, 2.5)) = 1
        _WaveSpeed ("水面流速", Range(0, 2)) = 0.65
        _Sparkle ("日光粼闪强度", Range(0, 1)) = 0.95
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "Ocean"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OceanVertex
            #pragma fragment OceanFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/FrameWork/Shader/Common/Noise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor, _OceanColor, _CrestColor, _FoamColor, _DistanceColor;
                float _WaveScale, _WaveSpeed, _Sparkle;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings OceanVertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            // 每一格独立放置一段偏斜浪脊，轮廓在格边前闭合。
            float3 Wavelet(float2 grid, float footprint)
            {
                float row = floor(grid.y);
                grid.x += Hash21(float2(row, 19.7));
                float2 cell = floor(grid);
                float shape = Hash21(cell + 2.7);
                float variation = Hash21(cell + 31.4);
                float2 local = frac(grid) - 0.5;
                float u = (local.x - (shape - 0.5) * 0.14) / lerp(0.23, 0.41, variation);
                float taper = saturate(1.0 - abs(u));
                float ridge = (variation - 0.5) * 0.30 + (shape - 0.5) * u * 0.18 - u * u * 0.075;
                float h = local.y - ridge;
                float edge = smoothstep(0.05, 0.42, taper);
                float aa = clamp(footprint * 0.45, 0.012, 0.10);
                float facet = (1.0 - smoothstep(0.09, 0.21, abs(h + 0.09))) * edge;
                float shadow = (1.0 - smoothstep(0.04, 0.15 + aa, abs(h - 0.12))) * edge;
                float shoulder = (1.0 - smoothstep(0.075, 0.18 + aa, abs(h + 0.035))) * edge;
                float coreWidth = max(lerp(0.036, 0.077, variation) * taper, min(0.13, footprint * 0.78));
                float core = (1.0 - smoothstep(coreWidth * 0.65, coreWidth + aa, abs(h))) * smoothstep(0.14, 0.53, taper);
                float peak = saturate(1.0 - abs(u - (variation - 0.5) * 0.68) * lerp(1.4, 3.8, shape));
                float pulse = 0.83 + 0.17 * cos(_Time.y * _WaveSpeed * 1.3 + shape * 15.0 + variation * 24.0);
                core *= lerp(0.16, 1.0, smoothstep(0.0, 1.0, peak)) * pulse;
                float lit = smoothstep(0.30, 0.82, shape);
                return float3(facet * (0.22 + 0.60 * shape) - shadow * 0.72, shoulder * lit, core * lit);
            }

            // 次一级浪列在纵深的间隔扩大四倍，横向只扩大两倍；这样远处仍是短亮片。
            float3 WaveletLevel(float2 grid, float footprint, float level)
            {
                float scaleX = exp2(-level);
                float scaleZ = scaleX * scaleX;
                float2 shifted = grid * float2(scaleX, scaleZ) + level * float2(18.73, 7.41);
                return Wavelet(shifted, footprint * scaleZ);
            }

            half4 OceanFragment(Varyings input) : SV_Target
            {
                float2 world = input.positionWS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float2 drift = world + t * float2(0.17, -0.34);
                float2 wind = float2(drift.x + drift.y * 0.21, drift.y - drift.x * 0.09);
                float2 patchGrid = wind * float2(0.48, 1.10);
                float swell = ValueNoise2D(patchGrid);
                float2 chopGrid = float2(wind.x - wind.y * 0.34, wind.y + wind.x * 0.19) *
                    float2(1.12, 2.15) + float2(swell * 1.2, t * 0.20);
                float2 chopWidth = fwidth(chopGrid);
                float chopDetail = 1.0 - smoothstep(0.16, 0.78, max(chopWidth.x, chopWidth.y));
                float chop = (ValueNoise2D(chopGrid) - 0.5) * chopDetail;
                float2 swellWidth = fwidth(patchGrid);
                float swellDetail = 1.0 - smoothstep(0.25, 0.92, max(swellWidth.x, swellWidth.y));

                float2 waveGrid = wind * float2(0.73, 2.35) + float2(chop * 0.14, swell * 0.65);
                float2 waveWidth = fwidth(waveGrid);
                float footprint = max(waveWidth.x, waveWidth.y);
                // 根据屏幕像素在水面纵深的跨度过渡到更大的错位浪片，避免整片海在中景就失去细节。
                float lod = clamp(0.5 * log2(max(1.0, footprint / 0.10)), 0.0, 3.0);
                float level = floor(lod);
                float3 waveA = WaveletLevel(waveGrid, footprint, level);
                float3 waveB = WaveletLevel(waveGrid, footprint, level + 1.0);
                float blend = smoothstep(0.10, 0.88, frac(lod));
                float ridge = lerp(waveA.x, waveB.x, blend);
                float shoulder = max(waveA.y * (1.0 - blend), waveB.y * blend);
                float flash = max(waveA.z * (1.0 - blend), waveB.z * blend);
                float light = saturate(0.53 + (swell - 0.5) * 0.80 * swellDetail + chop * 0.84 + ridge * 0.43);
                half3 color = lerp(_DeepColor.rgb, _OceanColor.rgb, smoothstep(0.16, 0.53, light));
                color = lerp(color, _CrestColor.rgb, smoothstep(0.43, 0.80, light));

                // 短浪脊反射天空与直射日光，太阳方位决定更亮的一侧。
                float3 positionVS = TransformWorldToView(input.positionWS);
                float eyeDepth = -positionVS.z;
                Light sun = GetMainLight();
                float solarAxis = -0.12 + 0.16 * TransformWorldToViewDir(sun.direction).x;
                float acrossPath = abs(positionVS.x / max(eyeDepth, 0.01) - solarAxis);
                float lightPath = 1.0 - smoothstep(0.11, 0.90, acrossPath);
                float sunlight = saturate(Luminance(sun.color) * 1.5);
                float glint = saturate(flash * _Sparkle * 1.95 * sunlight * lerp(0.32, 1.0, lightPath));
                color = lerp(color, _CrestColor.rgb * 1.16, shoulder * 0.71);
                color = lerp(color, _FoamColor.rgb * (1.32 + lightPath * 0.94), glint);

                float haze = smoothstep(44.0, 172.0, eyeDepth);
                color = lerp(color, _DistanceColor.rgb, haze * 0.92);
                // 大水面的雾按像素视深计算，避免在超远顶点上截断后插值。
                return half4(MixFog(color, ComputeFogFactorZ0ToFar(max(eyeDepth - _ProjectionParams.y, 0.0))), 1);
            }
            ENDHLSL
        }
    }
}
