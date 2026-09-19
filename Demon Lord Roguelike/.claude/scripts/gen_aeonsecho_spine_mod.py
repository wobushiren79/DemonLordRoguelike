# -*- coding: utf-8 -*-
"""
AeonsEchoSpine Mod JsonText 配置生成器（Demon Lord Roguelike）

功能：
  扫描 MOD 项目 Assets/ModResource/Spine/AeonsEcho 下的资源套装，生成幻化药道具配置：
    - Mods/AeonsEchoSpine/JsonText/ItemsInfo.txt            幻化药道具行（笛卡尔积：Chess × Avator）
    - Mods/AeonsEchoSpine/JsonText/Language_ItemsInfo_*.txt 12 语言道具名（name 自ID = 道具自ID）

规则（与 aeonsecho-spine-mod SKILL 文档一致）：
  - 套装 = 数字文件夹（如 1101）；Chess* 子文件夹 = 基础 spine 变体；Avator* 子文件夹 = ui_show_spine 高清图变体
  - 无 Chess 的套装跳过（幻化药必须以 Chess 为基础形象）
  - 道具自ID = 18(道具类型 TransformPotion) + 套装号 + 2位序号(01起, Chess 优先排序)
  - other_data = "{chessRes}" 或 "{chessRes},{avatorRes}|{uiScale};{uiX},{uiY}"
    资源名 = SkeletonData 资产名（Addressables Address，构建器按资产名登记）
    uiScale 默认按 430 / Avator骨架高 校准（与主游戏 Other 角色 ui_data_b 同量级）

用法（一律走 .claude/scripts/run-python.ps1）：
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
      ".claude/scripts/gen_aeonsecho_spine_mod.py" --mod-project "<MOD项目根>"
  # 构建完成后部署到主项目：
  ... --mod-project "<MOD项目根>" --deploy-main "<主项目根>"
"""
import argparse
import json
import shutil
import sys
from pathlib import Path

MOD_NAME = "AeonsEchoSpine"
SOURCE_REL = "Assets/ModResource/Spine/AeonsEcho"
ICON_RES = "Item_TransformPotion_1"  # 复用主游戏内置幻化药图标
ITEM_TYPE_TRANSFORM_POTION = 18
LANGUAGES = ["cn", "en", "jp", "kr", "tw", "de", "fr", "ru", "es", "br", "pl", "tr"]


def natural_key(name: str):
    """自然排序键（数字段按数值）：Chess < Chess_s01 < Chess_s02，Avator_lv1 < Avator_lv1a < Avator_lv2"""
    import re
    return [int(t) if t.isdigit() else t for t in re.split(r"(\d+)", name)]


def scan_sets(source_dir: Path):
    """扫描套装目录，返回 [(setId, [chess变体], [avator变体])] 与跳过清单"""
    sets = []
    skipped = []
    for set_dir in sorted(source_dir.iterdir(), key=lambda p: natural_key(p.name)):
        if not set_dir.is_dir() or not set_dir.name.isdigit():
            continue
        chess_list = sorted(
            [d.name for d in set_dir.iterdir() if d.is_dir() and d.name.startswith("Chess")],
            key=natural_key)
        avator_list = sorted(
            [d.name for d in set_dir.iterdir() if d.is_dir() and d.name.startswith("Avator")],
            key=natural_key)
        if not chess_list:
            skipped.append((set_dir.name, [d.name for d in set_dir.iterdir() if d.is_dir()]))
            continue
        sets.append((set_dir.name, chess_list, avator_list))
    return sets, skipped


def get_spine_height(json_path: Path) -> float:
    """读 spine json 的骨架包围盒高度（用于校准详情UI缩放）"""
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
        return float(data["skeleton"]["height"])
    except Exception:
        return 0.0


def main():
    parser = argparse.ArgumentParser(description="生成 AeonsEchoSpine Mod 的 JsonText 配置")
    parser.add_argument("--mod-project", required=True, help="MOD 项目根目录（含 Assets/ModResource/Spine/AeonsEcho）")
    parser.add_argument("--deploy-main", default="", help="可选：主项目根目录，构建完成后把 Mod 整个目录部署过去")
    parser.add_argument("--ui-scale-k", type=float, default=430.0, help="详情UI缩放校准常数 K：scale=K/Avator骨架高（默认430，与 Other 角色 ui_data_b 同量级）")
    parser.add_argument("--ui-pos-y", type=float, default=-215.0, help="详情UI默认Y偏移（默认-215≈-0.5*430）")
    args = parser.parse_args()

    mod_project = Path(args.mod_project)
    source_dir = mod_project / SOURCE_REL
    if not source_dir.is_dir():
        print(f"[错误] 资源目录不存在: {source_dir}")
        sys.exit(1)

    sets, skipped = scan_sets(source_dir)
    print(f"扫描到 {len(sets)} 个有效套装（含 Chess），跳过 {len(skipped)} 个无 Chess 套装")

    items = []
    lang_rows = {lang: [] for lang in LANGUAGES}
    used_ids = set()
    warnings = []

    for set_id, chess_list, avator_list in sets:
        seq = 0
        for chess in chess_list:
            chess_asset = source_dir / set_id / chess / f"{set_id}_{chess}_SkeletonData.asset"
            if not chess_asset.exists():
                warnings.append(f"{set_id}/{chess}: 缺 SkeletonData 资产，跳过该变体")
                continue
            chess_res = f"{set_id}_{chess}_SkeletonData"
            # Avator 缺失时按 [None] 处理（每个 Chess 单独出道具）
            for avator in (avator_list if avator_list else [None]):
                seq += 1
                if seq > 99:
                    warnings.append(f"{set_id}: 组合数超过99，ID序号溢出，后续组合未生成")
                    break
                self_id = int(f"{ITEM_TYPE_TRANSFORM_POTION}{set_id}{seq:02d}")
                if self_id in used_ids:
                    warnings.append(f"ID冲突: {self_id} ({set_id} 第{seq}个组合)")
                    continue
                used_ids.add(self_id)

                other_data = chess_res
                avator_tag = ""
                if avator is not None:
                    avator_asset = source_dir / set_id / avator / f"{set_id}_{avator}_SkeletonData.asset"
                    if not avator_asset.exists():
                        warnings.append(f"{set_id}/{avator}: 缺 SkeletonData 资产，该组合按无 Avator 处理")
                    else:
                        avator_res = f"{set_id}_{avator}_SkeletonData"
                        height = get_spine_height(source_dir / set_id / avator / f"{set_id}_{avator}.json")
                        ui_scale = round(args.ui_scale_k / height, 4) if height > 0 else 0.12
                        other_data = f"{chess_res},{avator_res}|{ui_scale};0,{int(args.ui_pos_y)}"
                        avator_tag = f"×{avator}"

                items.append({
                    "item_type": ITEM_TYPE_TRANSFORM_POTION,
                    "item_weapon_type": 0,
                    "num_max": 1,
                    "creature_model_id": 0,
                    "creature_model_info_id": 0,
                    "icon_res": ICON_RES,
                    "icon_rotate_z": 0.0,
                    "attack_mode_data": "",
                    "other_data": other_data,
                    "name": self_id,
                    "remark": f"回响幻化药(AeonsEcho {set_id} {chess}{avator_tag})",
                    "reward_rarity": "",
                    "id": self_id,
                })
                for lang in LANGUAGES:
                    if lang == "cn":
                        content = f"幻化药·回响{set_id}-{seq:02d}"
                    elif lang == "tw":
                        content = f"幻化藥·迴響{set_id}-{seq:02d}"
                    else:
                        content = f"Echo Potion {set_id}-{seq:02d}"
                    lang_rows[lang].append({"id": self_id, "content": content})

    # 写出 JsonText（游戏侧 File.ReadAllText + JsonUtil.FromJsonByNet 读取，UTF-8 无 BOM）
    out_dir = mod_project / "Mods" / MOD_NAME / "JsonText"
    out_dir.mkdir(parents=True, exist_ok=True)
    dump = lambda rows: json.dumps(rows, ensure_ascii=False, separators=(",", ":"))
    (out_dir / "ItemsInfo.txt").write_text(dump(items), encoding="utf-8")
    for lang in LANGUAGES:
        (out_dir / f"Language_ItemsInfo_{lang}.txt").write_text(dump(lang_rows[lang]), encoding="utf-8")

    print(f"已生成 {len(items)} 个幻化药道具 → {out_dir}")
    print(f"  ItemsInfo.txt + Language_ItemsInfo_*.txt ×{len(LANGUAGES)}")
    if skipped:
        print(f"  跳过套装（无 Chess）: {', '.join(s for s, _ in skipped)}")
    for w in warnings:
        print(f"  [警告] {w}")

    # 部署：构建完成后把 Mod 整个目录拷贝到主项目 Mods 下
    if args.deploy_main:
        mod_out = mod_project / "Mods" / MOD_NAME
        if not (mod_out / "catalog.bin").exists():
            print(f"[错误] 未找到构建产物 {mod_out}/catalog.bin —— 请先在 MOD 项目 Unity 里执行「工具/Mod/AeonsEchoSpine/一键构建」")
            sys.exit(1)
        target = Path(args.deploy_main) / "Mods" / MOD_NAME
        if target.exists():
            shutil.rmtree(target)
        shutil.copytree(mod_out, target)
        print(f"已部署 → {target}")
        print("下一步：主项目 Unity 里用 LauncherTest 启动（内含 InitializeAllModsSync），吃幻化药验证形象/详情UI")


if __name__ == "__main__":
    main()
