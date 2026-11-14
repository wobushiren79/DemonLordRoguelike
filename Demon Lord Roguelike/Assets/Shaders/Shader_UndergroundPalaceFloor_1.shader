Shader "Game/Scene/UndergroundPalaceFloor"
{
    Properties
    {
        [MainTexture] _BaseMap ("地砖贴图", 2D) = "white" {}
        [MainColor] _BaseColor ("地砖颜色", Color) = (1, 1, 1, 1)
        _Smoothness ("光滑度", Range(0, 1)) = 0.5
        _ReflectionStrength ("倒影强度", Range(0, 1)) = 0.58
        [HideInInspector] _Metallic ("金属度", Float) = 0
        [HideInInspector] _Cull ("剔除模式", Float) = 2
        [HideInInspector] _Surface ("表面类型", Float) = 0
        [HideInInspector] _WaterPlanarReflectionTex ("平面倒影纹理", 2D) = "black" {}
        [HideInInspector] _WaterPlanarReflectionActive ("平面倒影是否就绪", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }
        Cull [_Cull]
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_WaterPlanarReflectionTex);
            float4 _WaterPlanarReflectionTex_TexelSize;
            half _WaterPlanarReflectionActive;
            half _ReflectionStrength;

            half3 SampleFloorReflection(float2 uv)
            {
                float2 blur = 0.85 * _WaterPlanarReflectionTex_TexelSize.xy;
                half3 horizontal = SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv + float2(blur.x, 0)).rgb;
                horizontal += SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv - float2(blur.x, 0)).rgb;
                half3 vertical = SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv + float2(0, blur.y)).rgb;
                vertical += SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv - float2(0, blur.y)).rgb;
                return (horizontal + vertical) * 0.25h;
            }

            half4 ShadeReflectiveFloor(InputData lighting, SurfaceData surface)
            {
                half4 color = UniversalFragmentPBR(lighting, surface);
                if (_WaterPlanarReflectionActive > 0.5h)
                {
                    float4 screen = ComputeScreenPos(TransformWorldToHClip(lighting.positionWS));
                    float2 uv = screen.xy / screen.w;
                    // 反射相机重建了右手坐标系，采样时横向翻回。
                    uv.x = 1.0 - uv.x;

                    half grazing = 1.0h - saturate(dot(lighting.normalWS, lighting.viewDirectionWS));
                    half strength = _ReflectionStrength * lerp(0.18h, 0.92h, grazing * grazing);
                    half3 reflection = SampleFloorReflection(uv) * half3(0.83h, 0.80h, 0.78h);
                    color.rgb = lerp(color.rgb, reflection, strength);
                }
                return color;
            }

            // 沿用 URP Lit 的材质、灯光和雾，仅在雾化前混入倒影。
            #define UniversalFragmentPBR ShadeReflectiveFloor
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #undef UniversalFragmentPBR
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
