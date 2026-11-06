Shader "Game/Building/Atlas"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _BaseMap ("建筑图集", 2D) = "white" {}
        [HideInInspector] _BaseColor ("基础颜色", Color) = (1, 1, 1, 1)
        [HideInInspector] _Cull ("剔除模式", Float) = 0
        [HideInInspector] _ZWrite ("深度写入", Float) = 1
        [HideInInspector] _Surface ("表面类型", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Off

        HLSLINCLUDE
        #define InitializeStandardLitSurfaceData InitializeOriginalLitSurfaceData
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #undef InitializeStandardLitSurfaceData

        float2 BuildingAtlasUV(float2 uv, float4 rect)
        {
            return rect.xy + frac(uv) * rect.zw;
        }

        void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData surface)
        {
            half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
            // The atlas packs the old material settings in alpha. Geometry stays opaque.
            uint flags = (uint)round((1.0 - texel.a) * 255.0);
            uint glow = flags & 3u;
            uint smooth = (flags >> 3u) & 3u;
            half strength = glow == 3u ? 9.0h : (glow == 2u ? 5.0h : (glow == 1u ? 0.65h : 0.0h));

            surface = (SurfaceData)0;
            surface.albedo = texel.rgb;
            surface.alpha = 1.0h;
            surface.normalTS = half3(0, 0, 1);
            surface.metallic = (flags & 4u) != 0u ? 0.65h : 0.0h;
            surface.smoothness = smooth == 2u ? 0.5h : (smooth == 1u ? 0.3h : 0.15h);
            surface.occlusion = 1.0h;
            surface.emission = texel.rgb * strength;
        }

        struct BuildingAtlasAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 lightmapUV : TEXCOORD1;
            float2 atlasOrigin : TEXCOORD2;
            float2 atlasScale : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BuildingAtlasVertex
            #pragma fragment BuildingAtlasFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
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

            struct BuildingAtlasVaryings
            {
                Varyings lit;
                nointerpolation float4 atlasRect : TEXCOORD11;
            };

            BuildingAtlasVaryings BuildingAtlasVertex(BuildingAtlasAttributes input)
            {
                Attributes lit = (Attributes)0;
                lit.positionOS = input.positionOS;
                lit.normalOS = input.normalOS;
                lit.tangentOS = input.tangentOS;
                lit.texcoord = input.uv;
                lit.staticLightmapUV = input.lightmapUV;
                UNITY_TRANSFER_INSTANCE_ID(input, lit);

                BuildingAtlasVaryings output;
                output.lit = LitPassVertex(lit);
                output.atlasRect = float4(input.atlasOrigin, input.atlasScale);
                return output;
            }

            void BuildingAtlasFragment(BuildingAtlasVaryings input, out half4 color : SV_Target0
                #ifdef _WRITE_RENDERING_LAYERS
                , out uint renderingLayers : SV_Target1
                #endif
            )
            {
                input.lit.uv = BuildingAtlasUV(input.lit.uv, input.atlasRect);
                LitPassFragment(input.lit, color
                    #ifdef _WRITE_RENDERING_LAYERS
                    , renderingLayers
                    #endif
                );
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BuildingAtlasMetaVertex
            #pragma fragment BuildingAtlasMetaFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

            struct BuildingAtlasMetaVaryings
            {
                Varyings lit;
                nointerpolation float4 atlasRect : TEXCOORD3;
            };

            BuildingAtlasMetaVaryings BuildingAtlasMetaVertex(BuildingAtlasAttributes input)
            {
                Attributes lit = (Attributes)0;
                lit.positionOS = input.positionOS;
                lit.uv0 = input.uv;
                lit.uv1 = input.lightmapUV;

                BuildingAtlasMetaVaryings output;
                output.lit = UniversalVertexMeta(lit);
                output.atlasRect = float4(input.atlasOrigin, input.atlasScale);
                return output;
            }

            half4 BuildingAtlasMetaFragment(BuildingAtlasMetaVaryings input) : SV_Target
            {
                input.lit.uv = BuildingAtlasUV(input.lit.uv, input.atlasRect);
                return UniversalFragmentMetaLit(input.lit);
            }
            ENDHLSL
        }
    }
}
