---
name: project_mod_empty_build_selfheal
description: Mod 空构建事故链与自愈机制：构建中断残留分组 IncludeInBuild=false → 下次构建 0.07s 空跑「成功」→ 部署空 catalog 覆盖好产物 → 主项目 InvalidKeyException；12 个 Mod 构建器已加自愈（本组残留 false 自动恢复+0 条目中止），ModManager 空 catalog 直接判失败
metadata:
  type: project
---

# Mod 空构建事故链与自愈机制（2026-10-02 AeonsEchoSpine 事故）

## 症状（诊断特征）

主项目 Play 报 `InvalidKeyException: No Union of Assets between Keys= with Type=System.Object`（ModManager.LoadModCatalog* 的 DownloadDependenciesAsync），日志「[Mod] 依赖同步加载失败: <Mod名>」。**Keys= 后空白 = catalog 0 条目 = 空构建产物**。

## 根因链（四处防线同时缺失）

1. MOD 项目一次构建被中断（2026-10-01 凌晨 OtherSpine 构建崩溃，crash dump 在 `%TEMP%\Unity\Editor\Crashes`），`DisableOtherGroups` 把全部分组 `IncludeInBuild=false` 后 `RestoreOtherGroups` 没来得及跑 → **全分组 false 残留持久化**。
2. 构建器只「排除其他分组」、**从不确保本分组 IncludeInBuild=true** → 下次构建（10-02 AeonsEchoSpine）本分组也被排除 → `BuildPlayerContent` 0.074s 空跑却打日志「构建完成：466 个资源」（466 只是分组条目数，不是构建产物）。
3. 部署校验只看 `catalog.bin` 存在 → 空产物（530B 空 catalog + 0 bundle）**覆盖**主项目可用的旧部署。
4. 主项目 `DownloadDependenciesAsync(空keys, Union)` 抛无指向性的 InvalidKeyException。

**Why**: 任一环节补上都能避免事故；批量构建（-batchmode）被中断（崩溃/杀进程）是常态，快照-恢复模式必须假设 restore 可能永远不执行。

**How to apply**:
- 诊断空构建：`Mods/<Mod>/` 下 bundle 数=0 且 catalog.bin 仅几百字节；构建日志「Addressable content successfully built (duration: 0:00:00.0x)」秒级完成=空跑。
- 修复残留：MOD 项目 `Assets/AddressableAssetsData/AssetGroups/Schemas/*_BundledAssetGroupSchema.asset` 的 `m_IncludeInBuild` 全恢复 1（MOD 项目未开 Unity 时可直接文本编辑），再重跑一键构建。
- 已落地的自愈（2026-10-02）：12 个 `*ModBuilder.cs` 的 `EnsureGroupSchema` 自动恢复本分组 false（残留时 LogWarning）+ `entryCount==0` 中止构建（不清理旧产物、不产空包）；主项目 `ModManager.LoadModCatalog*` 三变体空 catalog 判失败并给出明确报错。

## 关联

- Mod 系统全貌见 mod-system skill；各 Mod 构建流程见 *-spine-mod skill。
- 同类「构建/导出产物被坏产物覆盖」教训：[[reference_epplus_stream_write_noop]]、[[reference_mod_excel_source]]。
