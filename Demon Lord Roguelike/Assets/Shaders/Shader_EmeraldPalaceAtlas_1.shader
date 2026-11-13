Shader "Game/Scene/EmeraldPalaceAtlas"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _BaseMap ("宫殿图集", 2D) = "white" {}
        [HideInInspector] _WaterPlanarReflectionTex ("平面倒影纹理", 2D) = "black" {}
        [HideInInspector] _WaterPlanarReflectionActive ("平面倒影是否就绪", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseMap_TexelSize;
            float4 _WaterPlanarReflectionTex_TexelSize;
            float _WaterPlanarReflectionActive;
        CBUFFER_END

        struct PalaceAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 lightmapUV : TEXCOORD1;
            float2 atlasOrigin : TEXCOORD2;
            float2 atlasSize : TEXCOORD3;
            float4 albedo : TEXCOORD4;
            float4 emissionAndMetallic : TEXCOORD5;
            float2 surface : TEXCOORD6;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 PalaceMaterialColor(float3 color)
        {
            #if !defined(UNITY_COLORSPACE_GAMMA)
                return SRGBToLinear(color);
            #else
                return color;
            #endif
        }

        half3 SamplePalaceAtlas(float2 uv, float4 atlasRect)
        {
            float2 atlasUV = atlasRect.xy + frac(uv) * atlasRect.zw;
            float2 gradientX = ddx(uv) * atlasRect.zw;
            float2 gradientY = ddy(uv) * atlasRect.zw;
            float footprint = max(length(gradientX * _BaseMap_TexelSize.zw), length(gradientY * _BaseMap_TexelSize.zw));
            // 图块各边有 32 像素循环留边，将采样限制在 mip 4 以免混入邻块。
            float gradientScale = 16.0 / max(16.0, footprint);
            return PLATFORM_SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, atlasUV, gradientX * gradientScale, gradientY * gradientScale).rgb;
        }

        void InitializePalaceSurface(float2 uv, float4 atlasRect, half4 tintAndSmoothness, half4 emissionAndMetallic, out SurfaceData surface)
        {
            half3 texel = SamplePalaceAtlas(uv, atlasRect);
            surface = (SurfaceData)0;
            surface.albedo = texel * tintAndSmoothness.rgb;
            surface.alpha = 1.0h;
            surface.normalTS = half3(0.0h, 0.0h, 1.0h);
            surface.emission = texel * emissionAndMetallic.rgb;
            surface.metallic = emissionAndMetallic.a;
            surface.smoothness = tintAndSmoothness.a;
            surface.occlusion = 1.0h;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PalaceAtlasVertex
            #pragma fragment PalaceAtlasFragment
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
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            // 复用 URP 的顶点、灯光和 GI 辅助逻辑，内置片元入口不参与宫殿绘制。
            #define InitializeStandardLitSurfaceData(uv, surface) InitializePalaceSurface(uv, float4(0, 0, 1, 1), half4(1, 1, 1, 0), half4(0, 0, 0, 0), surface)
            #define _Surface 0.0h
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #undef _Surface
            #undef InitializeStandardLitSurfaceData

            TEXTURE2D(_WaterPlanarReflectionTex);

            struct PalaceAtlasVaryings
            {
                Varyings lit;
                nointerpolation float4 atlasRect : TEXCOORD11;
                nointerpolation half4 tintAndSmoothness : TEXCOORD12;
                nointerpolation half4 emissionAndMetallic : TEXCOORD13;
            };

            PalaceAtlasVaryings PalaceAtlasVertex(PalaceAttributes input)
            {
                Attributes lit = (Attributes)0;
                lit.positionOS = input.positionOS;
                lit.normalOS = input.normalOS;
                lit.tangentOS = input.tangentOS;
                lit.texcoord = input.uv;
                lit.staticLightmapUV = input.lightmapUV;
                lit.dynamicLightmapUV = input.lightmapUV;
                UNITY_TRANSFER_INSTANCE_ID(input, lit);

                PalaceAtlasVaryings output;
                output.lit = LitPassVertex(lit);
                output.atlasRect = float4(input.atlasOrigin, input.atlasSize);
                output.tintAndSmoothness = half4(PalaceMaterialColor(input.albedo.rgb), input.surface.x);
                output.emissionAndMetallic = half4(PalaceMaterialColor(input.emissionAndMetallic.rgb), input.emissionAndMetallic.a);
                return output;
            }

            half3 SamplePalaceReflection(float2 uv)
            {
                float2 blur = 0.85 * _WaterPlanarReflectionTex_TexelSize.xy;
                half3 horizontal = SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv + float2(blur.x, 0)).rgb;
                horizontal += SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv - float2(blur.x, 0)).rgb;
                half3 vertical = SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv + float2(0, blur.y)).rgb;
                vertical += SAMPLE_TEXTURE2D(_WaterPlanarReflectionTex, sampler_LinearClamp, uv - float2(0, blur.y)).rgb;
                return (horizontal + vertical) * 0.25h;
            }

            void PalaceAtlasFragment(PalaceAtlasVaryings input, out half4 outColor : SV_Target0
                #ifdef _WRITE_RENDERING_LAYERS
                    , out uint outRenderingLayers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input.lit);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input.lit);

                SurfaceData surface;
                InitializePalaceSurface(input.lit.uv, input.atlasRect, input.tintAndSmoothness, input.emissionAndMetallic, surface);
                InputData lighting;
                InitializeInputData(input.lit, surface.normalTS, lighting);

                #if defined(_DBUFFER)
                    ApplyDecalToSurfaceData(input.lit.positionCS, surface, lighting);
                #endif

                InitializeBakedGIData(input.lit, lighting);
                half4 color = UniversalFragmentPBR(lighting, surface);
                if (_WaterPlanarReflectionActive > 0.5h && lighting.normalWS.y > 0.5h)
                {
                    float4 screen = ComputeScreenPos(TransformWorldToHClip(lighting.positionWS));
                    float2 uv = screen.xy / screen.w;
                    uv.x = 1.0 - uv.x;

                    half grazing = 1.0h - saturate(dot(lighting.normalWS, lighting.viewDirectionWS));
                    half intensity = 0.58h * lerp(0.18h, 0.92h, grazing * grazing);
                    half3 reflection = SamplePalaceReflection(uv) * half3(0.74h, 0.81h, 0.82h);
                    color.rgb = lerp(color.rgb, reflection, intensity);
                }
                outColor = half4(MixFog(color.rgb, lighting.fogCoord), 1.0h);

                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PalaceAtlasMetaVertex
            #pragma fragment PalaceAtlasMetaFragment
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

            struct PalaceAtlasMetaVaryings
            {
                Varyings lit;
                nointerpolation float4 atlasRect : TEXCOORD3;
                nointerpolation half4 tintAndSmoothness : TEXCOORD4;
                nointerpolation half4 emissionAndMetallic : TEXCOORD5;
            };

            PalaceAtlasMetaVaryings PalaceAtlasMetaVertex(PalaceAttributes input)
            {
                Attributes lit = (Attributes)0;
                lit.positionOS = input.positionOS;
                lit.uv0 = input.uv;
                lit.uv1 = input.lightmapUV;
                lit.uv2 = input.lightmapUV;

                PalaceAtlasMetaVaryings output;
                output.lit = UniversalVertexMeta(lit);
                output.atlasRect = float4(input.atlasOrigin, input.atlasSize);
                output.tintAndSmoothness = half4(PalaceMaterialColor(input.albedo.rgb), input.surface.x);
                output.emissionAndMetallic = half4(PalaceMaterialColor(input.emissionAndMetallic.rgb), input.emissionAndMetallic.a);
                return output;
            }

            half4 PalaceAtlasMetaFragment(PalaceAtlasMetaVaryings input) : SV_Target
            {
                SurfaceData surface;
                InitializePalaceSurface(input.lit.uv, input.atlasRect, input.tintAndSmoothness, input.emissionAndMetallic, surface);

                BRDFData brdf;
                InitializeBRDFData(surface.albedo, surface.metallic, surface.specular, surface.smoothness, surface.alpha, brdf);

                MetaInput metaInput;
                metaInput.Albedo = brdf.diffuse + brdf.specular * brdf.roughness * 0.5h;
                metaInput.Emission = surface.emission;
                return UniversalFragmentMeta(input.lit, metaInput);
            }
            ENDHLSL
        }
    }
}
