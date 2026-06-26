---
name: reference_spine_outline_vs_rim
description: 平面 Spine 精灵高亮要用 OutlineOnly 描边而非 Rim 边缘光（固定法线导致 Rim 恒为 0）
metadata:
  type: reference
---

战斗生物的 Spine 材质(如 `Goblin_Material.mat`)用 `Universal Render Pipeline/Spine/Sprite` shader + `_FIXED_NORMALS_VIEWSPACE` + `_FixedNormal=(0,0,1)`（整张精灵一块正对相机的平面、法线处处相同）。

- **Rim 边缘光不可用**：`Spine-Sprite` 的 rim 公式是 `rim=(1-saturate(dot(法线,视线)))^power`（见 `Assets/Shaders/Include/SpineCoreShaders/SpriteLighting.cginc` `applyRimLighting`）。法线恒正对相机 → `dot≈1` → `rim≈0`，平面精灵上**整片无边缘光**，开 `_RIM_LIGHTING` 也看不到。Rim 只对法线有起伏(3D/带法线贴图)的精灵有效。
- **要"高亮边框"用 OutlineOnly 真描边**：URP 包只提供 `Universal Render Pipeline/Spine/Outline/Skeleton-OutlineOnly`（`Assets/Shaders/Outline/`，**只画轮廓不画本体**，绝不能直接替换本体材质），没有"本体+描边"二合一 URP 版。

本项目实现（场上魔物悬停高亮）：共享单例预览预制 `FightCreature_OutlinePreview.prefab`（复制 `FightCreature_SelectPreview`，Spine MeshRenderer 挂描边材质 `MatSpriteCreatureOutline.mat`，颜色由材质决定不写死），经 `SkeletonAnimation.CustomMaterialOverride`(每帧重建仍生效，比直接改 `meshRenderer.material` 稳)把目标图集材质替换为描边材质、`_MainTex` 填目标图集纹理，移动到目标生物处。**职责拆分**：`CreatureManager` 只懒加载预制+取组件；显示/材质/跟随逻辑全在 `CreatureSpineOutlineFollow.Show/Hide`。

**描边要跟上目标动画**（否则定格首帧会与播动画的本体脱节）：`CreatureSpineOutlineFollow`(`Assets/Scripts/Game/Fight/`)订阅自身 `SkeletonAnimation.UpdateLocal`(在 `state.Apply` 之后、`UpdateWorldTransform` 之前触发) 逐根复制目标 `skeleton.Bones` 的本地 SRT，`LateUpdate` 再同步根位置与 Spine `localScale`(含左右翻转)。同一生物→同一 SkeletonData→骨骼数量/顺序一致才能逐根对应。
