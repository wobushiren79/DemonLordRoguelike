Shader "Game/Fight/DropCrystalInstanced1"
{
    // 战斗掉落魔晶专用 shader：GPU Instancing 批量渲染，由 FightDropCrystalInstanceRenderer 走 DrawMeshInstanced 使用。
    //
    // 【为什么独立 shader】替代原 ShaderGraph(SpriteCommonItemUnlit)：DrawMeshInstanced 路径只需要一个极简 Unlit+AlphaTest
    // 前向 pass，独立手写 shader 语义明确、无 ShaderGraph 兼容性不确定点；写法对标 FrameWork/URP/Shader_Mesh_TextInstanced_1。
    //
    // 【动画(对齐原 ShaderGraph 表现)】待机上下浮动：_IdleAnimPosition × sin(_Time.y × _IdleAnimSpeed)，
    // 沿相机右/上/前轴位移(与旧方案对象空间位移等价——旧 quad 本地轴经"生成时对齐相机旋转"后即相机轴)、
    // 全局时间驱动(与旧 SpriteRenderer 同材质表现一致：全场魔晶同相位)。原图无顶点自转(VertexRotateSpeed=0)故不实现。
    //
    // 【位置偏移】_PositionOffset：世界空间整体偏移(默认 (0,0.1,0))，仅视觉抬升渲染锚点——魔晶落地锚点在 +0.1y，
    // quad 半高≈0.112 再叠加待机下浮 0.05，最低点会下穿地面；抬高后最低点仍保持在地面之上。
    // 只影响渲染(逻辑位置/拾取判定仍按槽内 currentPos，不受本参数影响)。
    //
    // 【网格约定】单位 Quad(顶点 ±0.5、UV 满幅 0..1，Unity 内置 Quad 即满足)；billboard 展开用 (uv-0.5) 作角点，
    // 对网格顶点位置不敏感(任何满幅 UV 的 quad 都正确)。实例矩阵：平移=魔晶世界坐标，X/Y 列模长=世界宽/高，无旋转——
    // 朝向由本 shader 用相机右/上轴 billboard 展开，永远面向摄像头(相机拖拽/旋转/缩放均逐帧对齐)。
    //
    // 【贴图约定(_BaseMap)】魔晶 sprite 可能被图集(AtlasForGame)打包：C# Setup 时按 sprite 的 OuterUV 写 _BaseMap_ST
    // (tiling=尺寸、offset=起点)，shader 用 TRANSFORM_TEX 采样子区域；未打包时 ST 为默认值效果等同全幅。
    //
    // 【渲染状态(对齐原材质)】AlphaTest 硬边裁剪(阈值 _AlphaClipThreshold，原材质 0.1)、ZWrite On、不受影；
    // 投阴影由游戏设置「物品阴影」总开关控制(默认开启；关闭=省一遍 ShadowCaster Pass)——本 shader 带 ShadowCaster pass
    // (quad 以光源视角轴展开 billboard，影子=贴图轮廓沿光方向投影；曾"恒不投影"，应需求改为设置驱动)。
    // 深度测试/写深度由材质属性 _ZTest/_ZWrite 控制(默认 LEqual/On 与原一致；故事演出魔晶引导置顶时切 Always/Off，
    // 无视深度永远绘制在最前——见 FightDropCrystalInstanceRenderer.SetAlwaysOnTop)。
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("魔晶贴图 (图集子区域由 _BaseMap_ST 圈定, C# Setup 写入)", 2D) = "white" {}
        [MainColor]   _BaseColor ("整体染色", Color) = (1, 1, 1, 1)
        _AlphaClipThreshold ("Alpha 裁剪阈值 (原材质 0.1)", Range(0, 1)) = 0.1

        [Header(Idle Anim)]
        _IdleAnimPosition ("待机浮动幅度 (对象空间, 原材质 (0,0.05,0))", Vector) = (0, 0.05, 0, 0)
        _IdleAnimSpeed ("待机浮动速度 (弧度/秒, 原材质 5)", Float) = 5

        [Header(Position Offset)]
        _PositionOffset ("世界位置偏移 (仅视觉抬升防穿地面, 默认 +0.1y)", Vector) = (0, 0.1, 0, 0)

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试 (4=LEqual 常规, 8=Always 引导置顶)", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("写入深度 (1=On 常规, 0=Off 引导置顶)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            float  _AlphaClipThreshold;
            float4 _IdleAnimPosition;
            float  _IdleAnimSpeed;
            float4 _PositionOffset;
        CBUFFER_END
        ENDHLSL

        // 正向 Pass：颜色 pass。不做 DepthOnly(深度 pre-pass 用不上)；ShadowCaster 见下方独立 Pass(游戏设置「物品阴影」开关控制)。
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // AlphaTest 不透明裁剪：写深度(与原材质一致, 保证与场景的深度交互不变)，双面(quad 恒正面但 Cull Off 更稳)
            // _ZTest/_ZWrite 走材质属性(默认 LEqual/On 与原一致；故事魔晶引导置顶时切 Always/Off，见 SetAlwaysOnTop)
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;    // 单位 quad 顶点(±0.5)
                float2 uv         : TEXCOORD0;   // quad 角点 UV(0..1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;   // 已应用 _BaseMap_ST 的图集子区域 UV
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 实例矩阵：平移=魔晶世界锚点，X/Y 列模长=世界宽/高(无旋转,billboard 在本 shader 展开)
                float3 anchorWS = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float2 quadScale = float2(
                    length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20)),
                    length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)));

                // billboard：取相机右/上/前轴，使魔晶永远面向摄像头(相机拖拽/旋转/缩放逐帧对齐)
                float3 camRightWS   = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 camUpWS      = UNITY_MATRIX_I_V._m01_m11_m21;
                float3 camForwardWS = UNITY_MATRIX_I_V._m02_m12_m22;

                // 世界位置偏移(仅视觉抬升防浮动下穿地面;逻辑位置/拾取判定不受影响)
                anchorWS += _PositionOffset.xyz;

                // 待机浮动：沿相机三轴位移(与旧方案对象空间位移等价——旧 quad 本地轴经"对齐相机旋转"后即相机轴)
                float3 idleOffset = _IdleAnimPosition.xyz * sin(_Time.y * _IdleAnimSpeed);
                anchorWS += camRightWS * idleOffset.x + camUpWS * idleOffset.y + camForwardWS * idleOffset.z;

                // 用 (uv-0.5) 作角点沿相机右/上轴展开 quad(顶点位置不参与,任何满幅 UV 的 quad 都正确)
                float2 corner = (IN.uv - 0.5) * quadScale;
                float3 posWS = anchorWS + camRightWS * corner.x + camUpWS * corner.y;

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                clip(col.a - _AlphaClipThreshold);
                return col;
            }
            ENDHLSL
        }

        // 阴影投射 Pass：游戏设置「物品阴影」开启时，RenderAll 以 ShadowCastingMode.On 绘制走到本 Pass。
        // 顶点展开与 Forward 完全同构——⚠️阴影 pass 里 UNITY_MATRIX_I_V 是**光源视角**的逆矩阵，
        // 故同一份 billboard 代码自动把 quad 朝向光源(魔晶 billboard 无固定世界朝向，影子=贴图轮廓沿光方向的投影,这是标准做法)；
        // 位置偏移/待机浮动也一并复现，影子跟随视觉锚点同步起落。
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;    // 单位 quad 顶点(±0.5)
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 复刻 URP ShadowCasterPass 的偏移 + 近裁剪夹紧逻辑
            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            Varyings ShadowPassVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 与 Forward 相同的 billboard 展开(此处的"相机轴"实为阴影 pass 的光源轴,见 Pass 注释)
                float3 anchorWS = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float2 quadScale = float2(
                    length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20)),
                    length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)));

                float3 camRightWS   = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 camUpWS      = UNITY_MATRIX_I_V._m01_m11_m21;
                float3 camForwardWS = UNITY_MATRIX_I_V._m02_m12_m22;

                // 位置偏移 + 待机浮动(与 Forward 一致,影子跟随视觉同步起落)
                anchorWS += _PositionOffset.xyz;
                float3 idleOffset = _IdleAnimPosition.xyz * sin(_Time.y * _IdleAnimSpeed);
                anchorWS += camRightWS * idleOffset.x + camUpWS * idleOffset.y + camForwardWS * idleOffset.z;

                float2 corner = (IN.uv - 0.5) * quadScale;
                float3 posWS = anchorWS + camRightWS * corner.x + camUpWS * corner.y;

                // 法线取朝向光源(quad 面向光)用于阴影偏移
                OUT.positionHCS = GetShadowPositionHClip(posWS, -camForwardWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowPassFragment (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _AlphaClipThreshold);   // 按贴图轮廓镂空,与 Forward 同阈值
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
