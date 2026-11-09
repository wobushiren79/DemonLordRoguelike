Shader "Game/Scene/WildernessCanyonSand"
{
    Properties
    {
        [MainTexture] _BaseMap ("砂地底色", 2D) = "white" {}
        [MainColor] _BaseColor ("底色染色", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap ("地表法线", 2D) = "bump" {}
        _BumpScale ("法线强度", Range(0, 2)) = 0.72
        _Smoothness ("光滑度", Range(0, 1)) = 0.11
        [HideInInspector] _Metallic ("金属度", Float) = 0
        [HideInInspector] _Cull ("剔除模式", Float) = 2
        [HideInInspector] _Surface ("表面类型", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }
        Cull [_Cull]

        HLSLINCLUDE
        #define InitializeStandardLitSurfaceData InitializeOriginalLitSurfaceData
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #undef InitializeStandardLitSurfaceData
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData surface)
        {
            InitializeOriginalLitSurfaceData(uv, surface);

            // Coarser mip supplies the tone offset; full-resolution albedo keeps the grain and cracks.
            half3 sand = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, uv, 3.0).rgb;
            half3 albedo = surface.albedo;
            #if !defined(UNITY_COLORSPACE_GAMMA)
                sand = LinearToSRGB(sand);
                albedo = LinearToSRGB(albedo);
            #endif

            half luminance = dot(sand, half3(0.2126h, 0.7152h, 0.0722h));
            half lift = max(0.0h, 0.506h - luminance) * 0.55h;
            albedo = saturate(albedo + lift * half3(1.08h, 1.0h, 0.88h));
            #if !defined(UNITY_COLORSPACE_GAMMA)
                albedo = SRGBToLinear(albedo);
            #endif
            surface.albedo = albedo;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma shader_feature_local _NORMALMAP
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
            #pragma multi_compile _ _LIGHT_LAYERS
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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }
}
