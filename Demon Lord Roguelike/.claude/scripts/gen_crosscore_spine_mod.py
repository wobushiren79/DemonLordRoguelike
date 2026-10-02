# -*- coding: utf-8 -*-
"""
CrossCoreSpine Mod 数据生成器（Demon Lord Roguelike）

两段式流水线（Excel 为唯一真实源，与主项目配置表惯例一致）：
  scan   扫描 MOD 项目 Assets/ModResource/Spine/CrossCore 资源目录 → 重建道具 Excel（合并保留语言 Excel 的人工改名）
  export 读道具/语言 Excel → 导出 Mod JsonText（ItemsInfo.txt + Language_ItemsInfo_*.txt ×12）
  all    scan + export（默认）

资源结构（2026-09-29 全量扫描确认）：
  - 数字目录 107 个（10010~6003003 不连续，5~7 位数字）：目录下全是一层子目录
    （skin_* 196 个 / break_* 54 个 / synchro_* 3 个 / 其他零星），每个子目录一套完整 spine 导出物
  - 命名目录 169 个（acheron/alps/cg00010_intoxicatedshadow_spine 等）：目录内直接放 spine 导出物，可多个
  - 共 656 个 SkeletonData，其中特效层 234 个（用户规则：背景不生成幻化药也不导出；形态含 _effect_B/F 169 个
    + _effectB/F、_effect _B、-effect B 等变体 65 个，跳过规则见 EFFECT_LAYER_PATTERN）
  - 本体 422 个 = 422 个幻化药；feili 目录只有散装素材无 SkeletonData，整目录不出药

与 GirlWarsSpine/NikkeSpine 的差异：
  - 两种目录形态混合（数字目录两层 / 命名目录一层），扫描统一按顶层目录 rglob 递归
  - 背景层跳过规则 = 文件名中 effect 前有分隔符（`_effect_B/F`、`_effectB/F`、`_effect _B`、`-effect B` 等变体全算），
    不是 GirlWars 的 Back/Front/BG
  - 待机动画走标准 pick_idle_anim 规则（用户规则「动作都用idle」：idle 本身是主项目标准待机候选 #1，
    405 个骨架命中标准候选（绝大多数靠精确 idle）→ 不写键、运行时框架自动播 idle；9 个只有
    idle2/idle3/idle5 等替代的写 ui_show_idle_anim 键；8 个只有 click*/guochang/animation → 不写键+警告=详情UI静态显示）
  - 全部幻化药只改 ui_show（详情UI高清展示），不动 show/world——other_data 只含 ui_show_res/ui_show_data/ui_show_idle_anim 三键
  - Spine JSON 全部为真 4.3.26（488 个本体 JSON 扫描确认：无旧 linkedmesh、无顶层分离约束数组），无需格式转换。
    新增资源时仍需校验：若出现旧 linkedmesh（skins 内含 "parent"）或顶层 "ik"/"transform"/"path" 约束数组，须先转换（参照 aeonsecho-spine-mod SKILL）

other_data 键值格式（& 拆项、每项首个 : 拆键值，同主项目 attack_mode other_data 规约，缺省键省略）：
  ui_show_res:10010_skin_Alps03_SkeletonData&ui_show_data:0.7534;0,0
  ui_show_res  ui_show_spine 高清展示资源名（详情UI，isUIShow=true 时使用）
  ui_show_data 详情UI尺寸「scale;x,y」（生成器按 645/骨架高 校准 scale，默认位移 0,0；scan 重建时默认按 id 保留手调值，--reset-layout 强制重算）
  ui_show_idle_anim 替代待机动画名（仅骨架无标准候选但有含 idle 动画时生成，如 idle2；命中候选/全无 idle 时不生成）

道具自ID规则：`18` + 套装号 + 2位序号(01 起,套装内按相对路径自然序)；name 自ID=道具自ID。
  套装号：数字目录=目录数字本身（10010/6003003 不定长）；命名目录=独立编号段 9000001 起按目录自然序分配
  （9000001 > 数字目录最大值 6003003，物理防冲突；used_ids 另有冲突告警兜底）。
  例：目录 10010 第 1 个本体 → 181001001；命名目录 acheron（第 1 个命名目录）→ 18900000101。

Excel 位置（MOD 项目，可用 Excel/WPS 直接打开查看/调整；与其他 Mod 的 Excel 相互独立）：
  Assets/Data/Excel/excel_mod_items_info_crosscorespine[Mod道具信息-CrossCoreSpine].xlsx - 道具配置（3 行表头，name[language] 标记列）
  Assets/Data/Excel/excel_mod_language_crosscorespine[Mod多语言-CrossCoreSpine].xlsx     - 道具名多语言（id + content_{12语言}）

注意：
  - scan 会全量重建道具表数据行（覆盖前先备份到 MOD项目/ExcelBackup/），但 **ui_show_data 手调值按 id 保留**
    （新增资源按骨架校准默认值；--reset-layout 可强制全部重算）；改了道具表参数后请只跑 export
  - 语言表按 id 合并保留人工内容：新增道具补默认名，删除的道具行自动清理
  - 命名目录编号段按目录自然序分配：新增命名目录会挤占后续编号导致 id 漂移（同 id=同资源不再成立），
    此时 ui_show_data 手调值按 id 保留会错位——新增命名目录后建议 --reset-layout 全量重算
  - 规则详见主项目 .claude/skills/crosscore-spine-mod/SKILL.md

用法（一律走主项目 .claude/scripts/run-python.ps1 包装）：
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
      ".claude/scripts/gen_crosscore_spine_mod.py" all --mod-project "<MOD项目根>"
  # 构建完成后部署到主项目：
  ... export --mod-project "<MOD项目根>" --deploy-main "<主项目根>"
"""
import argparse
import json
import re
import shutil
import sys
from pathlib import Path

import openpyxl

MOD_NAME = "CrossCoreSpine"
SOURCE_REL = "Assets/ModResource/Spine/CrossCore"
ICON_RES = "Item_Potion_1"  # 复用主游戏内置幻化药图标
ITEM_TYPE_TRANSFORM_POTION = 18
LANGUAGES = ["cn", "en", "jp", "kr", "tw", "de", "fr", "ru", "es", "br", "pl", "tr"]

# 道具表：列名/类型/说明（布局与主项目 excel_items_info 一致，name 带 [language] 标记列）
ITEM_SHEET = "ItemsInfo"
# 与主项目 TestTransformPotionGUI.GetModItemsExcelRelPath 的约定一致：excel_mod_items_info_{modName小写}[Mod道具信息-{modName}].xlsx
ITEM_EXCEL_REL = "Assets/Data/Excel/excel_mod_items_info_crosscorespine[Mod道具信息-CrossCoreSpine].xlsx"
ITEM_COLUMNS = [
    ("id", "long", "道具ID(key)=18+套装号(数字目录=目录数字/命名目录=9000001起编号段)+2位序号"),
    ("item_type", "int", "道具类型(18=幻化药)"),
    ("item_weapon_type", "int", "武器类型(仅武器有效)"),
    ("num_max", "int", "堆叠上限(1=不堆叠)"),
    ("creature_model_id", "long", "生物模组信息ID(0=通用)"),
    ("creature_model_info_id", "long", "生物模组详细信息ID"),
    ("icon_res", "string", "图标资源"),
    ("icon_rotate_z", "float", "图标旋转"),
    ("attack_mode_data", "string", "攻击模式数据(幻化药不用)"),
    ("other_data", "string", "形象键值串:ui_show_res:X&ui_show_data:scale;x,y[&ui_show_idle_anim:名](&拆项,:拆键值,缺省键省略;只改详情UI,不动show/world;待机动画idle命中标准候选不写键)"),
    ("name[language]", "long", "道具名textId(=道具id,文本在excel_mod_language_crosscorespine)"),
    ("remark", "string", "备注"),
    ("reward_rarity", "string", "奖励稀有度白名单(空=全适配;消耗品不进装备池)"),
    ("source", "string", "道具来源(逗号分隔枚举值,空=默认来源) 1=征服模式奖励"),
]

# 语言表：id + content_{lang}（与主项目 excel_language 的 sheet 布局一致；与其他 Mod 的语言 Excel 相互独立）
LANG_SHEET = "ItemsInfo"
LANG_EXCEL_REL = "Assets/Data/Excel/excel_mod_language_crosscorespine[Mod多语言-CrossCoreSpine].xlsx"

# 数字资源目录名规则：纯数字（如 10010、6003003）；数字直接拼进道具ID
SET_DIR_PATTERN = re.compile(r"^(\d+)$")
# 背景/特效层跳过规则（用户规则 2026-09-29：「带 _effect_ 的都是背景不用生成」，按意图覆盖全部 effect 变体）：
# SkeletonData 名中 effect 前有分隔符（_/-/空格）即视为特效层——实际命中形态：_effect_B/_effect_F（169 个，
# 如 Acheron_effect_B）、_effectB/_effectF（60 个，如 Skadi_effectB）、_effect _B（带空格，如 Badlands_effect _B）、
# -effect B/-effectB（连字符，如 Rex-effect B、melody-effectB）。共 234 个不处理不导出
EFFECT_LAYER_PATTERN = re.compile(r"[_\-\s]effect[_\-\s]?", re.IGNORECASE)
# 命名目录（acheron/cg00010_... 等非纯数字）套装号起始段：9000001 起按目录自然序递增分配
# （9000001 > 数字目录最大值 6003003，与数字目录 ID 物理防冲突；同 browndust 非 char 目录编号段思路）
NAMED_SET_NUM_BASE = 9000000
# 标准待机动画候选的兜底值（=主项目 excel_spine_animation_state id=10001 的 res 字段；读不到配置时用）
STD_IDLE_FALLBACK = ["idle", "wait", "idle1", "wait1", "stand"]


def natural_key(name: str):
    """自然排序键（数字段按数值）：10010 < 6003003；同目录内 ...03a < ...03b < ...10"""
    return [int(t) if t.isdigit() else t for t in re.split(r"(\d+)", name)]


def default_name(lang: str, set_label: str, seq: int, total_in_set: int) -> str:
    """道具名默认值：cn/tw 中文，其余语言英文兜底；套装内本体 >1 时全部带 -NN 序号区分，单本体不带"""
    suffix = f"-{seq:02d}" if total_in_set > 1 else ""
    if lang == "cn":
        return f"幻化药·交错战线{set_label}{suffix}"
    if lang == "tw":
        return f"幻化藥·交錯戰線{set_label}{suffix}"
    return f"CrossCore Potion {set_label}{suffix}"


def get_spine_height(json_path: Path) -> float:
    """读 spine json 的骨架包围盒高度（用于校准详情UI缩放）"""
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
        return float(data["skeleton"]["height"])
    except Exception:
        return 0.0


def load_std_idle_candidates() -> list:
    """读主项目 SpineAnimationState.txt 的 Idle(id=10001) 候选名列表（配置表为唯一真实源,不硬编码；
    按脚本位置推导主项目根=.claude/scripts/ 向上两级）；读失败回退 STD_IDLE_FALLBACK"""
    try:
        main_root = Path(__file__).resolve().parents[2]
        rows = json.loads((main_root / "Assets/Resources/JsonText/SpineAnimationState.txt").read_text(encoding="utf-8"))
        for row in rows:
            if int(row.get("id", 0)) == 10001:
                candidates = [s.strip() for s in str(row.get("res", "")).split(",") if s.strip()]
                if candidates:
                    return candidates
    except Exception:
        pass
    return STD_IDLE_FALLBACK


def get_spine_anims(json_path: Path) -> list:
    """读 spine json 的动画名列表（4.x 格式 animations 为 dict,取 keys 保持声明顺序）；异常返回空列表"""
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
        anims = data.get("animations")
        if isinstance(anims, dict):
            return list(anims.keys())
    except Exception:
        pass
    return []


def pick_idle_anim(anims: list, std_candidates: list) -> str:
    """检测骨架动画列表的待机动画（idle 动画替代规则,新 Mod 生成脚本同样继承）：
    命中标准候选(大小写不敏感全等)→返回""(不需替代,运行时走框架候选解析；CrossCore 442 个骨架靠精确 idle 命中)；
    否则取首个小写含 idle 的动画原始名(保留大小写,Spine SetAnimation 需精确名)；
    完全没有含 idle 动画→返回""(不生成 idle 键,详情UI静态显示+警告)"""
    lowers = {a.lower() for a in anims}
    for c in std_candidates:
        if c.lower() in lowers:
            return ""
    for a in anims:
        if "idle" in a.lower():
            return a
    return ""


def check_spine_json_format(json_path: Path) -> str:
    """校验 spine json 是否为真 4.3 格式：旧 linkedmesh（skins 内 attachment 含 "parent"）或顶层分离约束数组
    （"ik"/"transform"/"path"）时返回警告描述，否则返回空串（参照 aeonsecho-spine-mod SKILL「Spine JSON 格式陷阱」）"""
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
    except Exception:
        return ""
    skins = data.get("skins")
    skin_list = skins if isinstance(skins, list) else (list(skins.values()) if isinstance(skins, dict) else [])
    for skin in skin_list:
        if not isinstance(skin, dict):
            continue
        for slot in (skin.get("attachments") or {}).values():
            entries = slot if isinstance(slot, list) else (list(slot.values()) if isinstance(slot, dict) else [])
            for att in entries:
                if isinstance(att, dict) and "parent" in att:
                    return "含旧 linkedmesh(parent)"
    for key in ("ik", "transform", "path"):
        if key in data:
            return f"含顶层分离约束数组({key})"
    return ""


def backup_excel(excel_path: Path, mod_project: Path):
    """覆盖 Excel 前备份到 MOD项目/ExcelBackup/（Assets 之外，不被导出工具扫描、不产生 .meta）；
    滚动复用 .bak.1~.bak.3（1=最新）——移位覆盖旧文件不新增，目录里永远只有最近 3 份，并顺带清掉旧版时间戳命名备份"""
    if not excel_path.exists():
        return
    backup_dir = mod_project / "ExcelBackup"
    backup_dir.mkdir(exist_ok=True)
    name = excel_path.name
    # 移位覆盖：bak.3←bak.2、bak.2←bak.1（replace 直接覆盖复用旧文件，不产生新文件）
    for i in range(3, 1, -1):
        newer = backup_dir / f"{name}.bak.{i - 1}"
        if newer.exists():
            newer.replace(backup_dir / f"{name}.bak.{i}")
    shutil.copy2(excel_path, backup_dir / f"{name}.bak.1")
    # 清理非滚动命名的旧备份（时间戳版等），保证永远都只有 3 份
    keep = {f"{name}.bak.{i}" for i in range(1, 4)}
    for old in backup_dir.iterdir():
        if old.is_file() and old.name.startswith(f"{name}.bak.") and old.name not in keep:
            old.unlink()


def build_other_data(ui_show_res: str, ui_show_data: str = "", ui_show_idle_anim: str = "") -> str:
    """拼 other_data 键值串：& 拆项、每项首个 : 拆键值（同主项目 attack_mode other_data 规约），缺省键省略；
    CrossCoreSpine 药只改 ui_show（详情UI），不动 show/world，每本体一药不指定皮肤（骨架默认皮肤）；
    ui_show_idle_anim 仅骨架无标准候选但有含 idle 动画时生成（如 idle2），命中候选（idle/idle1 等）不生成"""
    segs = []
    if ui_show_res:
        segs.append(f"ui_show_res:{ui_show_res}")
    if ui_show_data:
        segs.append(f"ui_show_data:{ui_show_data}")
    if ui_show_idle_anim:
        segs.append(f"ui_show_idle_anim:{ui_show_idle_anim}")
    return "&".join(segs)


def read_preserved_ui_show_data(excel_path: Path) -> dict:
    """读现有道具表提取各行 other_data 的 ui_show_data 键 → {道具id: ui_show_data值}；
    scan 全量重建时按 id 保留测试面板手调的详情UI尺寸（同 id=同目录同资源,骨架未变,手调值仍有效；新增资源按骨架校准）"""
    preserved = {}
    if not excel_path.exists():
        return preserved
    wb = openpyxl.load_workbook(excel_path, read_only=True)
    if ITEM_SHEET not in wb.sheetnames:
        wb.close()
        return preserved
    ws = wb[ITEM_SHEET]
    header = [c.value for c in ws[1]]
    if "id" not in header or "other_data" not in header:
        wb.close()
        return preserved
    col_id = header.index("id")
    col_other = header.index("other_data")
    for row in ws.iter_rows(min_row=4, values_only=True):
        if row[0] is None or row[col_other] is None:
            continue
        for seg in str(row[col_other]).split("&"):
            if seg.startswith("ui_show_data:"):
                preserved[int(row[col_id])] = seg[len("ui_show_data:"):].strip()
                break
    wb.close()
    return preserved


def collect_set_skeletons(set_dir: Path):
    """收集一个顶层套装目录下全部 SkeletonData 资产（rglob 递归，兼容数字目录两层/命名目录一层形态），
    返回 [(资产路径, 资源名=文件名去扩展名)] 按相对路径自然序排序；调用方负责 _effect_ 跳过"""
    sds = sorted(set_dir.rglob("*_SkeletonData.asset"),
                 key=lambda p: natural_key(p.relative_to(set_dir).as_posix()))
    return [(p, p.name[: -len(".asset")]) for p in sds]


def compute_items(source_dir: Path, ui_scale_k: float, ui_pos_y: float, preserved_ui: dict = None):
    """扫描资源目录计算道具行，返回 (道具行列表, 跳过清单, 警告清单, idle统计dict)；道具行 key 为干净字段名；
    preserved_ui=scan 前从旧道具表读出的 ui_show_data 手调值（按 id 保留）"""
    preserved_ui = preserved_ui or {}
    items = []
    used_ids = set()
    warnings = []
    skipped = []
    # idle 检测统计：std=命中标准候选（不写键,框架自动播 idle/idle1 等）, alt=替代动画（写 ui_show_idle_anim 键）, none=无 idle（不写键+警告）
    anim_stats = {"std": 0, "alt": 0, "none": 0}
    std_candidates = load_std_idle_candidates()

    # 命名目录套装号：9000001 起按目录自然序分配（先收集再编号,保证分配稳定可复现）
    top_dirs = sorted([p for p in source_dir.iterdir() if p.is_dir()], key=lambda p: natural_key(p.name))
    named_set_nums = {}
    for p in top_dirs:
        if not SET_DIR_PATTERN.match(p.name):
            named_set_nums[p.name] = str(NAMED_SET_NUM_BASE + len(named_set_nums) + 1)

    for set_dir in top_dirs:
        m = SET_DIR_PATTERN.match(set_dir.name)
        # 套装号：数字目录=目录数字；命名目录=编号段。套装标签：数字目录=目录名,命名目录=目录名原样（供默认道具名）
        set_num = m.group(1) if m else named_set_nums[set_dir.name]
        set_label = set_dir.name
        # 收集目录内本体资源：每个 *_SkeletonData.asset 出 1 个药（含 _effect_ 的背景/特效层跳过）
        # 资源名=资产文件名去扩展名(保留 _SkeletonData, 与构建器 Address=Path.GetFileNameWithoutExtension 及其他 Mod 约定一致)；
        # 同名 spine json=文件名去 _SkeletonData.asset + .json
        all_sds = collect_set_skeletons(set_dir)
        main_sds = []
        for sd_file, res_name in all_sds:
            if EFFECT_LAYER_PATTERN.search(res_name):
                skipped.append(f"{set_dir.name}/{res_name}(背景/特效层)")
            else:
                main_sds.append((sd_file, res_name))
        if not all_sds:
            skipped.append(f"{set_dir.name}(无 spine 资源)")
            continue
        if not main_sds:
            skipped.append(f"{set_dir.name}(仅背景/特效层,无本体出药)")
            continue
        # 先按套装内总数确定命名是否要带序号，再逐个出药
        total_in_set = len(main_sds)
        seq = 0
        for sd_file, res_name in main_sds:
            json_path = sd_file.parent / f"{sd_file.name[: -len('_SkeletonData.asset')]}.json"
            if not json_path.exists():
                warnings.append(f"{set_dir.name}: {res_name} 缺同名 spine json,跳过该资源")
                continue
            seq += 1
            if seq > 99:
                warnings.append(f"{set_dir.name}: 资源数超过99,ID序号溢出,后续资源未生成")
                break
            self_id = int(f"{ITEM_TYPE_TRANSFORM_POTION}{set_num}{seq:02d}")
            if self_id in used_ids:
                warnings.append(f"ID冲突: {self_id} ({set_dir.name} 第{seq}个资源)")
                continue
            used_ids.add(self_id)
            # Spine JSON 格式校验（真 4.3 可直接用；旧 linkedmesh/分离约束数组需先转换）
            fmt_warn = check_spine_json_format(json_path)
            if fmt_warn:
                warnings.append(f"{set_dir.name}: {res_name} {fmt_warn},主项目 spine-csharp 4.3.39 可能不兼容,需先转换")
            # ui_show_data: 手调值按 id 保留优先,否则按骨架高校准 scale=K/骨架高
            ui_show_data = preserved_ui.get(self_id, "")
            if not ui_show_data:
                height = get_spine_height(json_path)
                ui_scale = round(ui_scale_k / height, 4) if height > 0 else 0.12
                ui_show_data = f"{ui_scale};0,{int(ui_pos_y)}"
            # idle 动画检测（标准 pick_idle_anim 规则）：命中候选不写键；否则取首个含 idle 动画写键；全无→不写键+警告
            anims = get_spine_anims(json_path)
            ui_show_idle_anim = pick_idle_anim(anims, std_candidates)
            if ui_show_idle_anim:
                anim_stats["alt"] += 1
            elif any(c.lower() in {a.lower() for a in anims} for c in std_candidates):
                anim_stats["std"] += 1
            else:
                anim_stats["none"] += 1
                warnings.append(f"{set_dir.name}: {res_name} 无任何含 idle 动画(实际={anims[:8]}),未生成 ui_show_idle_anim 键(详情UI将静态显示)")
            other_data = build_other_data(res_name, ui_show_data, ui_show_idle_anim)
            items.append({
                "id": self_id,
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
                "remark": f"交错战线幻化药(CrossCore {set_dir.name} {res_name})",
                "reward_rarity": "",
                # 来源=征服模式奖励(ItemSourceEnum.ConquerReward)：征服通关领奖的魔晶位之一随机替换为幻化药
                "source": "1",
                "_set_label": set_label,  # 内部字段：供语言默认名使用，不进 Excel/JSON
                "_seq": seq,
                "_total_in_set": total_in_set,
            })
    return items, skipped, warnings, anim_stats


def write_item_excel(excel_path: Path, items, mod_project: Path):
    """全量重建道具表（3 行表头：列名/类型/说明；数据从第 4 行起）"""
    backup_excel(excel_path, mod_project)
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = ITEM_SHEET
    for col, (name, type_name, comment) in enumerate(ITEM_COLUMNS, start=1):
        ws.cell(row=1, column=col, value=name)
        ws.cell(row=2, column=col, value=type_name)
        ws.cell(row=3, column=col, value=comment)
    for row, item in enumerate(items, start=4):
        for col, (name, _, _) in enumerate(ITEM_COLUMNS, start=1):
            key = name.replace("[language]", "")
            ws.cell(row=row, column=col, value=item[key])
    excel_path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(excel_path)


def merge_language_excel(excel_path: Path, items, mod_project: Path):
    """语言表按 id 合并：保留人工改名，新道具补默认名，失效 id 清理"""
    existing = {}  # id -> {lang: content}
    if excel_path.exists():
        wb = openpyxl.load_workbook(excel_path, read_only=True)
        if LANG_SHEET in wb.sheetnames:
            ws = wb[LANG_SHEET]
            header = [c.value for c in ws[1]]
            for row in ws.iter_rows(min_row=4, values_only=True):
                if row[0] is None:
                    continue
                row_id = int(row[0])
                existing[row_id] = {}
                for i, col_name in enumerate(header):
                    if col_name and col_name.startswith("content_") and i < len(row):
                        lang = col_name.replace("content_", "")
                        if row[i] is not None and str(row[i]).strip() != "":
                            existing[row_id][lang] = str(row[i])
        wb.close()

    backup_excel(excel_path, mod_project)
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = LANG_SHEET
    ws.cell(row=1, column=1, value="id")
    ws.cell(row=2, column=1, value="long")
    ws.cell(row=3, column=1, value="道具名textId(key)=道具id")
    for col, lang in enumerate(LANGUAGES, start=2):
        ws.cell(row=1, column=col, value=f"content_{lang}")
        ws.cell(row=2, column=col, value="string")
        ws.cell(row=3, column=col, value="道具名(空=英文兜底)")
    kept = 0
    for row, item in enumerate(items, start=4):
        self_id = item["id"]
        ws.cell(row=row, column=1, value=self_id)
        old = existing.get(self_id, {})
        for col, lang in enumerate(LANGUAGES, start=2):
            content = old.get(lang) or default_name(lang, item["_set_label"], item["_seq"], item["_total_in_set"])
            if old.get(lang):
                kept += 1
            ws.cell(row=row, column=col, value=content)
    excel_path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(excel_path)
    return kept


def read_item_excel(excel_path: Path):
    """读道具表 → JSON 行（key 剥离 [language] 标记，按类型还原数值）"""
    wb = openpyxl.load_workbook(excel_path, read_only=True)
    ws = wb[ITEM_SHEET]
    rows = list(ws.iter_rows(values_only=True))
    wb.close()
    header = rows[0]
    types = rows[1]
    result = []
    for row in rows[3:]:
        if row[0] is None:
            continue
        item = {}
        for i, col_name in enumerate(header):
            if col_name is None:
                continue
            key = str(col_name).replace("[language]", "")
            type_name = str(types[i]) if types[i] else "string"
            value = row[i] if i < len(row) else None
            if type_name in ("long", "int"):
                item[key] = int(value) if value is not None and str(value).strip() != "" else 0
            elif type_name == "float":
                item[key] = float(value) if value is not None and str(value).strip() != "" else 0.0
            else:
                item[key] = "" if value is None else str(value)
        result.append(item)
    result.sort(key=lambda x: x["id"])
    return result


def read_language_excel(excel_path: Path):
    """读语言表 → {lang: [{id, content}]}（空内容按 cn→en 顺序兜底）"""
    wb = openpyxl.load_workbook(excel_path, read_only=True)
    ws = wb[LANG_SHEET]
    rows = list(ws.iter_rows(values_only=True))
    wb.close()
    header = [str(c) if c else "" for c in rows[0]]
    per_lang = {lang: {} for lang in LANGUAGES}
    for row in rows[3:]:
        if row[0] is None:
            continue
        row_id = int(row[0])
        contents = {}
        for i, col_name in enumerate(header):
            if col_name.startswith("content_") and i < len(row) and row[i] is not None and str(row[i]).strip() != "":
                contents[col_name.replace("content_", "")] = str(row[i])
        for lang in LANGUAGES:
            # 兜底链：本语言 → cn → en → id
            content = contents.get(lang) or contents.get("cn") or contents.get("en") or str(row_id)
            per_lang[lang][row_id] = content
    return per_lang


def main():
    parser = argparse.ArgumentParser(description="CrossCoreSpine Mod 数据生成器（Excel→JsonText 两段式）")
    parser.add_argument("command", nargs="?", default="all", choices=["scan", "export", "all"],
                        help="scan=资源→Excel；export=Excel→JsonText；all=两者（默认）")
    parser.add_argument("--mod-project", required=True, help="MOD 项目根目录")
    parser.add_argument("--deploy-main", default="", help="可选：主项目根目录，构建完成后把 Mod 整个目录部署过去")
    parser.add_argument("--ui-scale-k", type=float, default=645.0, help="详情UI缩放校准常数 K：scale=K/骨架高（默认645，与其他 Mod 同基准）")
    parser.add_argument("--ui-pos-y", type=float, default=0.0, help="详情UI默认Y偏移（默认0）")
    parser.add_argument("--reset-layout", action="store_true",
                        help="重建时重新设置卡片位置/大小（ui_show_data 全部按骨架重算）；默认关闭=按 id 保留已有手调值，仅新资源计算默认值")
    args = parser.parse_args()

    mod_project = Path(args.mod_project)
    source_dir = mod_project / SOURCE_REL
    item_excel = mod_project / ITEM_EXCEL_REL
    lang_excel = mod_project / LANG_EXCEL_REL

    if args.command in ("scan", "all"):
        if not source_dir.is_dir():
            print(f"[错误] 资源目录不存在: {source_dir}")
            sys.exit(1)
        preserved_ui = {} if args.reset_layout else read_preserved_ui_show_data(item_excel)
        items, skipped, warnings, anim_stats = compute_items(source_dir, args.ui_scale_k, args.ui_pos_y, preserved_ui)
        write_item_excel(item_excel, items, mod_project)
        kept = merge_language_excel(lang_excel, items, mod_project)
        print(f"[scan] 重建道具表 {len(items)} 行 → {item_excel}")
        print(f"[scan] 合并语言表（保留人工内容 {kept} 格）→ {lang_excel}")
        print(f"[scan] idle 动画检测：命中标准候选 {anim_stats['std']}（不写键,框架自动播），替代动画 {anim_stats['alt']}（写 ui_show_idle_anim 键），无 idle {anim_stats['none']}（不写键+警告）")
        if args.reset_layout:
            print("[scan] --reset-layout：ui_show_data 全部按骨架重算")
        elif preserved_ui:
            print(f"[scan] 按 id 保留手调 ui_show_data {len(preserved_ui)} 行（--reset-layout 可强制重算）")
        if skipped:
            print(f"[scan] 跳过 {len(skipped)} 项（_effect_ 背景/特效层、无本体目录）")
        for w in warnings:
            print(f"[scan] [警告] {w}")

    if args.command in ("export", "all"):
        if not item_excel.exists() or not lang_excel.exists():
            print("[错误] Excel 不存在，请先运行 scan")
            sys.exit(1)
        items = read_item_excel(item_excel)
        per_lang = read_language_excel(lang_excel)

        out_dir = mod_project / "Mods" / MOD_NAME / "JsonText"
        out_dir.mkdir(parents=True, exist_ok=True)
        dump = lambda rows: json.dumps(rows, ensure_ascii=False, separators=(",", ":"))
        (out_dir / "ItemsInfo.txt").write_text(dump(items), encoding="utf-8")
        for lang in LANGUAGES:
            rows = [{"id": k, "content": v} for k, v in sorted(per_lang[lang].items())]
            (out_dir / f"Language_ItemsInfo_{lang}.txt").write_text(dump(rows), encoding="utf-8")
        print(f"[export] 导出 {len(items)} 个道具 → {out_dir}")
        print(f"[export] ItemsInfo.txt + Language_ItemsInfo_*.txt ×{len(LANGUAGES)}")

    # 部署：构建完成后把 Mod 整个目录拷贝到主项目 Mods 下
    if args.deploy_main:
        mod_out = mod_project / "Mods" / MOD_NAME
        if not (mod_out / "catalog.bin").exists():
            print(f"[错误] 未找到构建产物 {mod_out}/catalog.bin —— 请先在 MOD 项目 Unity 里执行「工具/Mod/CrossCoreSpine/一键构建」")
            sys.exit(1)
        target = Path(args.deploy_main) / "Mods" / MOD_NAME
        if target.exists():
            shutil.rmtree(target)
        shutil.copytree(mod_out, target)
        print(f"[deploy] 已部署 → {target}")
        print("下一步：主项目 Unity 里用 LauncherTest 启动（内含 InitializeAllModsSync），吃幻化药验证详情UI形象")


if __name__ == "__main__":
    main()
