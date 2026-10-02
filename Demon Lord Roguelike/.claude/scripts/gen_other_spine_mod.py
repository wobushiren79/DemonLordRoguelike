# -*- coding: utf-8 -*-
"""
OtherSpine Mod 数据生成器（Demon Lord Roguelike）

两段式流水线（Excel 为唯一真实源，与主项目配置表惯例一致）：
  scan   扫描 MOD 项目 Assets/ModResource/Spine/Other 资源目录(角色名目录) → 重建道具 Excel（合并保留语言 Excel 的人工改名）
  export 读道具/语言 Excel → 导出 Mod JsonText（ItemsInfo.txt + Language_ItemsInfo_*.txt ×12）
  all    scan + export（默认）

资源约定与道具规则（2026-09-29 与用户确认）：
  - 资源结构=角色名目录（Amelia/Luna/guanchazhe 等 59 个，字母/拼音命名，无数字套装号）；
    资源名=SkeletonData 资产文件名(去扩展名，保留 _SkeletonData 后缀)
  - **Avator 规则**：SkeletonData 名带 `Avator` = ui_show（详情UI高清展示段）；不带的 = 基础 show 段
    （世界/战斗/小卡形象）。配对按同目录内前缀：Avator 名截到 `_Avator` 前 = 基础名
    （如 Amelia_Avator↔Amelia、Luna_2_Avator↔Luna_2）；**无前缀匹配的基础回落配对目录主 Avator**
    （base_key==目录名优先，否则排序首个）——如 Aoliweiya 目录 1 Avator+2 基础 → A+Avator 与 B+Avator 各出药（2026-09-29 与用户确认）
  - **出药**：每个基础 SkeletonData 出药（有配对 Avator 则带上 ui_show 段键）；
    未被任何基础配对的 Avator 单独出仅详情UI幻化药；
    **多皮肤拆分**：Avator 骨架 skins 里的每个具名皮肤（≠default）单独出 1 个幻化药，
    带 `ui_show_skin:皮肤名` 键（ArkReSpine 同规则）；**多皮肤时不生成默认 default 皮肤药**，
    仅 default 单皮肤的 Avator 才出 1 个普通药（2026-09-29 与用户确认）
  - **待机动画**：走 mod-system SKILL 通用 idle 检测（load_std_idle_candidates/pick_idle_anim）——
    当前 104 个骨架全部命中标准待机候选（idle/wait/idle1/wait1/stand，2026-09-29 全量扫描确认，
    其中 13 个 Avator 的待机=wait，主项目 SpineAnimationState id=10001 候选已含 wait 无需改表），
    均不生成 idle_anim/ui_show_idle_anim 键；未来新增资源无标准候选时取首个含 idle 动画名写键
  - Spine JSON 全部为真 4.3.26（2026-09-29 全量 104 个 JSON 扫描确认：无旧 linkedmesh、无顶层分离约束数组），无需格式转换。
    新增资源时仍需校验：若出现旧 linkedmesh（skins 内含 "parent"）或顶层 "ik"/"transform"/"path" 约束数组，须先转换（参照 aeonsecho-spine-mod SKILL）
  - 贴图已确认 PMA（材质 _StraightAlphaInput=0 且 PNG 透明区纯黑，2026-09-29 抽样）；新增资源入库须复查（见 browndust-spine-mod SKILL 白边事故记录）

other_data 键值格式（& 拆项、每项首个 : 拆键值，同主项目 attack_mode other_data 规约，缺省键省略）：
  基础+Avator 配对药：show_res:Amelia_SkeletonData&show_data:0.8415;0,-120&ui_show_res:Amelia_Avator_SkeletonData&ui_show_data:0.1717;0,0
  仅基础药：         show_res:Baolilong_SkeletonData&show_data:...（无 ui_show 段键；详情UI回落 show 形象，
                     其尺寸可经测试面板手调补 ui_show_data 键——scan 重建按资源身份保留，同 world_data 机制）
  仅详情UI幻化药：    ui_show_res:Anniboni_Avator_SkeletonData&ui_show_data:...（无 show 段键）
  皮肤药：           配对药全部键 + ui_show_skin:皮肤名
  show_res       默认展示形象资源名（世界/战斗/小卡）
  show_data      小卡UI尺寸「scale;x,y」（生成器按 3159/骨架高 校准 scale，默认位移 0,-120）
  ui_show_res    ui_show_spine 高清展示资源名（详情UI，isUIShow=true 时使用）
  ui_show_data   详情UI尺寸「scale;x,y」（生成器按 645/骨架高 校准 scale，默认位移 0,0）
  ui_show_skin   ui_show 资源内指定皮肤名（仅皮肤药带）
  idle_anim / ui_show_idle_anim  替代待机动画名（仅骨架无标准待机候选时生成）
  world_data     世界显示尺寸/偏移（无校准来源不生成，测试面板手调；scan 重建按「目录名/资源token」保留）
  scan 全量重建时 show_data/ui_show_data/world_data 手调值按「目录名/资源token」(remark 资源身份)保留
  ——按资源身份而非道具id保留，出药规则变化/资源增减导致 id 漂移时保留值也不会贴错道具（--reset-layout 可强制重算）

道具自ID规则：`18` + `4位目录序号`(0001 起,按目录名自然序分配) + `2位序号`(01 起,目录内出药顺序)。
  目录序号在 scan 重建时按目录名从旧道具表 remark 回收保留（新增目录取 max+1 不复用已释放号），保证已发道具 id 不漂移。
  name 自ID=道具自ID。例：Amelia 目录(序号0001) → 18000101(基础药)、18000102(皮肤药)…

Excel 位置（MOD 项目，可用 Excel/WPS 直接打开查看/调整；与其他 Mod 的 Excel 相互独立）：
  Assets/Data/Excel/excel_mod_items_info_otherspine[Mod道具信息-OtherSpine].xlsx - 道具配置（3 行表头，name[language] 标记列）
  Assets/Data/Excel/excel_mod_language_otherspine[Mod多语言-OtherSpine].xlsx     - 道具名多语言（id + content_{12语言}）

注意：
  - scan 会全量重建道具表数据行（覆盖前先备份到 MOD项目/ExcelBackup/），但布局三键手调值按 remark 资源身份、目录序号按目录名保留；改了道具表参数后请只跑 export
  - 语言表按 id 合并保留人工内容：新增道具补默认名，删除的道具行自动清理
  - 规则详见主项目 .claude/skills/other-spine-mod/SKILL.md

用法（一律走主项目 .claude/scripts/run-python.ps1 包装）：
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
      ".claude/scripts/gen_other_spine_mod.py" all --mod-project "<MOD项目根>"
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

MOD_NAME = "OtherSpine"
SOURCE_REL = "Assets/ModResource/Spine/Other"
ICON_RES = "Item_Potion_1"  # 复用主游戏内置幻化药图标
ITEM_TYPE_TRANSFORM_POTION = 18
LANGUAGES = ["cn", "en", "jp", "kr", "tw", "de", "fr", "ru", "es", "br", "pl", "tr"]

# 道具表：列名/类型/说明（布局与主项目 excel_items_info 一致，name 带 [language] 标记约定）
ITEM_SHEET = "ItemsInfo"
# 与主项目 TestTransformPotionGUI.GetModItemsExcelRelPath 的约定一致：excel_mod_items_info_{modName小写}[Mod道具信息-{modName}].xlsx
ITEM_EXCEL_REL = "Assets/Data/Excel/excel_mod_items_info_otherspine[Mod道具信息-OtherSpine].xlsx"
ITEM_COLUMNS = [
    ("id", "long", "道具ID(key)=18+4位目录序号+2位序号"),
    ("item_type", "int", "道具类型(18=幻化药)"),
    ("item_weapon_type", "int", "武器类型(仅武器有效)"),
    ("num_max", "int", "堆叠上限(1=不堆叠)"),
    ("creature_model_id", "long", "生物模组信息ID(0=通用)"),
    ("creature_model_info_id", "long", "生物模组详细信息ID"),
    ("icon_res", "string", "图标资源"),
    ("icon_rotate_z", "float", "图标旋转"),
    ("attack_mode_data", "string", "攻击模式数据(幻化药不用)"),
    ("other_data", "string", "形象键值串:show_res/ui_show_res=X_SkeletonData&show_data/ui_show_data=scale;x,y&ui_show_skin=皮肤名(&拆项,:拆键值,缺省键省略;带Avator=ui_show详情UI段,不带=基础show段;多皮肤Avator按具名皮肤拆药)"),
    ("name[language]", "long", "道具名textId(=道具id,文本在excel_mod_language_otherspine)"),
    ("remark", "string", "备注"),
    ("reward_rarity", "string", "奖励稀有度白名单(空=全适配;消耗品不进装备池)"),
    ("source", "string", "道具来源(逗号分隔枚举值,空=默认来源) 1=征服模式奖励"),
]

# 语言表：id + content_{lang}（与主项目 excel_language 的 sheet 布局一致；与其他 Mod 的语言 Excel 相互独立）
LANG_SHEET = "ItemsInfo"
LANG_EXCEL_REL = "Assets/Data/Excel/excel_mod_language_otherspine[Mod多语言-OtherSpine].xlsx"

# Avator 判定：SkeletonData 名含 Avator 子串 = ui_show 详情UI段；不带 = 基础 show 段
AVATOR_MARK = "Avator"
# Avator→基础配对：Avator 名截到 _Avator 前 = 基础名（Amelia_Avator→Amelia、Luna_2_Avator→Luna_2）
AVATOR_SUFFIX_PATTERN = re.compile(r"_Avator")
# 布局手调值保留键（scan 重建按「目录名/资源token」保留，与 remark 同源）
LAYOUT_KEYS = ("show_data", "ui_show_data", "world_data")
# remark 完整解析：格式「Other幻化药(目录名/资源token)」，token=基础stem 或 AvatorStem skin:皮肤名（含空格）
REMARK_FULL_PATTERN = re.compile(r"Other幻化药\(([^/]+)/([^)]+)\)")


def natural_key(name: str):
    """自然排序键（数字段按数值）：Amelia < Luna_2 < Luna_10"""
    return [int(t) if t.isdigit() else t for t in re.split(r"(\d+)", name)]


def default_name(lang: str, base_title: str, skin: str) -> str:
    """道具名默认值：cn/tw 中文，其余语言英文兜底；base_title=目录名（目录内第 2 个起的基础药已带 -NN 后缀）；皮肤药=基础名+皮肤名"""
    skin_suffix = f" {skin}" if skin else ""
    if lang == "cn":
        return f"幻化药·Other {base_title}{skin_suffix}"
    if lang == "tw":
        return f"幻化藥·Other {base_title}{skin_suffix}"
    return f"Other Potion {base_title}{skin_suffix}"


def get_spine_height(json_path: Path) -> float:
    """读 spine json 的骨架包围盒高度（用于校准UI缩放）"""
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
        return float(data["skeleton"]["height"])
    except Exception:
        return 0.0


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


def get_spine_skins(json_path: Path) -> list:
    """读 spine json 的皮肤名列表（4.x 格式 skins 为对象数组取 name，旧格式为 dict 取 keys），保持声明顺序；异常返回空列表"""
    try:
        data = json.loads(json_path.read_text(encoding="utf-8"))
        skins = data.get("skins")
        if isinstance(skins, list):
            return [s.get("name", "") for s in skins if isinstance(s, dict)]
        if isinstance(skins, dict):
            return list(skins.keys())
    except Exception:
        pass
    return []


def load_std_idle_candidates() -> list:
    """读主项目 SpineAnimationState.txt 的 id=10001 标准待机候选（配置表为唯一真实源，读失败回退硬编码同值）；
    主项目根按脚本位置推导（.claude/scripts/ 向上两级）"""
    try:
        main_root = Path(__file__).resolve().parents[2]
        txt = main_root / "Assets" / "Resources" / "JsonText" / "SpineAnimationState.txt"
        for row in json.loads(txt.read_text(encoding="utf-8")):
            if int(row.get("id", 0)) == 10001:
                return [s.strip() for s in str(row.get("res", "")).split(",") if s.strip()]
    except Exception:
        pass
    return ["idle", "wait", "idle1", "wait1", "stand"]


def pick_idle_anim(anims: list, std_candidates: list):
    """待机检测：命中标准候选（大小写不敏感）→ 返回 None=不生成键走框架候选解析；
    无标准候选取首个小写含 idle 的动画原始名（保留大小写，Spine SetAnimation 需精确名）；
    完全没有含 idle 动画 → 返回空串=不生成键+调用方警告"""
    lowers = [a.lower() for a in anims]
    if any(c in lowers for c in std_candidates):
        return None
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


def build_other_data(show_res: str = "", show_data: str = "", idle_anim: str = "",
                     ui_show_res: str = "", ui_show_data: str = "", ui_show_skin: str = "",
                     ui_show_idle_anim: str = "", world_data: str = "") -> str:
    """拼 other_data 键值串：& 拆项、每项首个 : 拆键值（同主项目 attack_mode other_data 规约），缺省键省略"""
    segs = []
    if show_res:
        segs.append(f"show_res:{show_res}")
    if show_data:
        segs.append(f"show_data:{show_data}")
    if idle_anim:
        segs.append(f"idle_anim:{idle_anim}")
    if ui_show_res:
        segs.append(f"ui_show_res:{ui_show_res}")
    if ui_show_data:
        segs.append(f"ui_show_data:{ui_show_data}")
    if ui_show_skin:
        segs.append(f"ui_show_skin:{ui_show_skin}")
    if ui_show_idle_anim:
        segs.append(f"ui_show_idle_anim:{ui_show_idle_anim}")
    if world_data:
        segs.append(f"world_data:{world_data}")
    return "&".join(segs)


def read_preserved_rows(excel_path: Path):
    """读现有道具表 → (布局手调值 {(目录名,资源token): {布局键: 值}}, 目录序号 {目录名: 序号})；
    scan 全量重建时：布局三键(show_data/ui_show_data/world_data)按「目录名/资源token」(remark 资源身份)保留测试面板手调值
    ——按资源身份而非道具id保留，出药规则变化/资源增减导致 id 漂移时保留值也不会贴错道具；
    目录序号按 remark 里的目录名回收，保证新增目录插入后旧道具 id 不漂移"""
    preserved_layout = {}
    dir_nums = {}
    if not excel_path.exists():
        return preserved_layout, dir_nums
    wb = openpyxl.load_workbook(excel_path, read_only=True)
    if ITEM_SHEET not in wb.sheetnames:
        wb.close()
        return preserved_layout, dir_nums
    ws = wb[ITEM_SHEET]
    header = [c.value for c in ws[1]]
    if "id" not in header or "other_data" not in header:
        wb.close()
        return preserved_layout, dir_nums
    col_id = header.index("id")
    col_other = header.index("other_data")
    col_remark = header.index("remark") if "remark" in header else -1
    for row in ws.iter_rows(min_row=4, values_only=True):
        if row[0] is None or row[col_other] is None:
            continue
        row_id = int(row[col_id])
        layout = {}
        for seg in str(row[col_other]).split("&"):
            for key in LAYOUT_KEYS:
                if seg.startswith(f"{key}:"):
                    layout[key] = seg[len(key) + 1:].strip()
        if col_remark >= 0 and row[col_remark]:
            m = REMARK_FULL_PATTERN.search(str(row[col_remark]))
            if m:
                preserve_key = (m.group(1), m.group(2).strip())
                if layout and preserve_key not in preserved_layout:
                    preserved_layout[preserve_key] = layout
                dir_name = m.group(1)
                dir_num = (row_id // 100) % 10000
                if dir_name not in dir_nums:
                    dir_nums[dir_name] = dir_num
    wb.close()
    return preserved_layout, dir_nums


def alloc_dir_nums(dir_names: list, preserved_dir_nums: dict) -> dict:
    """分配目录序号：旧目录保留原序号；新目录按自然序取 max(已用)+1 起递增（不复用已释放号，防存档 id 串目录）"""
    result = dict(preserved_dir_nums)
    next_num = max(result.values(), default=0) + 1
    for name in dir_names:
        if name in result:
            continue
        result[name] = next_num
        next_num += 1
    return result


def compute_items(source_dir: Path, ui_scale_k: float, ui_pos_y: float, show_scale_k: float, show_pos_y: float,
                  preserved_layout: dict, preserved_dir_nums: dict, std_idle: list):
    """扫描资源目录计算道具行，返回 (道具行列表, 统计dict, 警告清单)；道具行 key 为干净字段名；
    preserved_layout/preserved_dir_nums=scan 前从旧道具表读出的手调值（按「目录名/资源token」）与目录序号（按目录名）"""
    items = []
    used_ids = set()
    warnings = []
    # 统计：配对成功/仅基础/仅Avator/皮肤药；idle 检测 std=命中标准候选, alt=替代, none=无idle
    stats = {"paired": 0, "base_only": 0, "avator_only": 0, "skin": 0,
             "idle_std": 0, "idle_alt": 0, "idle_none": 0}

    dir_names = sorted([p.name for p in source_dir.iterdir() if p.is_dir()], key=natural_key)
    dir_nums = alloc_dir_nums(dir_names, preserved_dir_nums)

    def make_layout(json_path: Path, scale_k: float, pos_y: float, preserve_key, layout_key: str, fallback_scale: float):
        """布局键取值：手调保留(按「目录名/资源token」)优先，否则按骨架高校准 scale=K/骨架高"""
        old = preserved_layout.get(preserve_key, {})
        if layout_key in old:
            return old[layout_key]
        height = get_spine_height(json_path)
        scale = round(scale_k / height, 4) if height > 0 else fallback_scale
        return f"{scale};0,{int(pos_y)}"

    # idle/皮肤检测结果缓存（同一 Avator 骨架被多个基础配对时不重复检测/计数）
    idle_cache = {}
    skins_cache = {}

    def detect_idle(json_path: Path, res_label: str) -> str:
        """idle 检测：返回写入键的动画名（None→"" 不生成）；无 idle 动画时警告"""
        key = str(json_path)
        if key in idle_cache:
            return idle_cache[key]
        anims = get_spine_anims(json_path)
        picked = pick_idle_anim(anims, std_idle)
        result = ""
        if picked is None:
            stats["idle_std"] += 1
        elif picked == "":
            stats["idle_none"] += 1
            warnings.append(f"{res_label} 无任何 idle 动画(实际={anims[:8]}),未生成 idle 键(将静态显示)")
        else:
            stats["idle_alt"] += 1
            result = picked
        idle_cache[key] = result
        return result

    def avator_skins(json_path: Path) -> list:
        """读骨架的具名皮肤列表（≠default，skins 数组声明顺序），带缓存"""
        key = str(json_path)
        if key not in skins_cache:
            skins_cache[key] = [s for s in get_spine_skins(json_path) if s and s != "default"]
        return skins_cache[key]

    def append_item(dir_name: str, dir_num: int, seq: int, name_title: str, skin: str, remark_token: str, **data_keys):
        """组装单个道具行（含 ID 分配/冲突检测/other_data 拼接/命名内部字段）"""
        if seq > 99:
            warnings.append(f"{dir_name}: 目录内组合数超过99,ID序号溢出,后续组合未生成")
            return False
        self_id = int(f"{ITEM_TYPE_TRANSFORM_POTION}{dir_num:04d}{seq:02d}")
        if self_id in used_ids:
            warnings.append(f"ID冲突: {self_id} ({dir_name} 第{seq}个组合)")
            return False
        used_ids.add(self_id)
        # 保留的 world_data（无校准来源，只可能来自手调；按「目录名/资源token」查找）
        wd = preserved_layout.get((dir_name, remark_token), {}).get("world_data", "")
        if wd and not data_keys.get("world_data"):
            data_keys["world_data"] = wd
        # 保留手调的 ui_show_data（仅基础药无配对 Avator 时生成流程不出此键，但详情UI回落 show 形象仍消费它，
        # 测试面板可手调补上；与 world_data 同机制按「目录名/资源token」找回——配对/仅Avator药 data_keys
        # 已带 make_layout 校准值（其内部已先查保留），不会走到这个兜底）
        usd = preserved_layout.get((dir_name, remark_token), {}).get("ui_show_data", "")
        if usd and not data_keys.get("ui_show_data"):
            data_keys["ui_show_data"] = usd
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
            "other_data": build_other_data(**data_keys),
            "name": self_id,
            "remark": f"Other幻化药({dir_name}/{remark_token})",
            "reward_rarity": "",
            # 来源=征服模式奖励(ItemSourceEnum.ConquerReward)：征服通关领奖的魔晶位之一随机替换为幻化药
            "source": "1",
            "_name_title": name_title,  # 内部字段：供语言默认名使用，不进 Excel/JSON
            "_skin": skin,
        })
        return True

    for dir_name in dir_names:
        set_dir = source_dir / dir_name
        dir_num = dir_nums[dir_name]
        # 收集目录内资源：Avator 名=ui_show 段，其余=基础 show 段；资源名=资产文件名去扩展名(保留 _SkeletonData)
        bases = []    # [(base_stem, res_name, json_path)]
        avators = []  # [(avator_stem, res_name, json_path, base_key)]
        for sd_file in sorted(set_dir.glob("*_SkeletonData.asset"), key=lambda p: natural_key(p.name)):
            res_name = sd_file.name[: -len(".asset")]
            stem = sd_file.name[: -len("_SkeletonData.asset")]
            json_path = set_dir / f"{stem}.json"
            if not json_path.exists():
                warnings.append(f"{dir_name}: {res_name} 缺同名 spine json,跳过该资源")
                continue
            fmt_warn = check_spine_json_format(json_path)
            if fmt_warn:
                warnings.append(f"{dir_name}: {res_name} {fmt_warn},主项目 spine-csharp 4.3.39 可能不兼容,需先转换")
            if AVATOR_MARK in stem:
                base_key = AVATOR_SUFFIX_PATTERN.split(stem)[0]
                avators.append((stem, res_name, json_path, base_key))
            else:
                bases.append((stem, res_name, json_path))
        # 排序：与目录同名（的配对）排首位=主形象拿 01 序号与纯目录名标题，变体（Luna_2/Aoliweiya_ChongChong 等）排后带 -NN
        bases.sort(key=lambda t: (t[0] != dir_name, natural_key(t[0])))
        avators.sort(key=lambda t: (t[3] != dir_name, natural_key(t[3])))
        # 目录主 Avator：base_key==目录名 的优先，否则排序首个（无前缀匹配的基础回落配对到它）
        primary_av = next((a for a in avators if a[3] == dir_name), avators[0] if avators else None)
        paired_av = set()

        seq = 0
        base_count = 0
        # ① 每个基础资源出药：配对=前缀精确匹配（X_Avator↔X）→ 无匹配回落目录主 Avator（如 Aoliweiya 双基础共享同一 Avator）；
        #    配对 Avator 含具名皮肤（≠default）时不生成默认皮肤药、每皮肤 1 药；仅默认皮肤时出 1 普通药
        for stem, res_name, json_path in bases:
            base_count += 1
            # 目录内多个基础药时第 2 个起名字带 -NN 区分（NN=基础药序号）
            title = dir_name if base_count == 1 else f"{dir_name}-{base_count:02d}"
            idle_anim = detect_idle(json_path, f"{dir_name}/{stem}")
            av_match = next((a for a in avators if a[3] == stem), None) or primary_av
            av_stem = av_res = av_json = None
            ui_idle = ""
            skins = []
            if av_match:
                av_stem, av_res, av_json, _ = av_match
                paired_av.add(av_stem)
                stats["paired"] += 1
                ui_idle = detect_idle(av_json, f"{dir_name}/{av_stem}")
                skins = avator_skins(av_json)
            else:
                stats["base_only"] += 1
            for skin in (skins if skins else [""]):
                seq += 1
                self_id = int(f"{ITEM_TYPE_TRANSFORM_POTION}{dir_num:04d}{seq:02d}")
                # 资源身份token(=remark token=布局保留键)：皮肤药=AvatorStem skin:皮肤名，否则=基础stem
                token = f"{av_stem} skin:{skin}" if (av_match and skin) else stem
                preserve_key = (dir_name, token)
                data = {"show_res": res_name,
                        "show_data": make_layout(json_path, show_scale_k, show_pos_y, preserve_key, "show_data", 0.84),
                        "idle_anim": idle_anim}
                if av_match:
                    data["ui_show_res"] = av_res
                    data["ui_show_data"] = make_layout(av_json, ui_scale_k, ui_pos_y, preserve_key, "ui_show_data", 0.17)
                    data["ui_show_idle_anim"] = ui_idle
                    if skin:
                        data["ui_show_skin"] = skin
                        stats["skin"] += 1
                append_item(dir_name, dir_num, seq, title, skin, token, **data)
        # ② 未被任何基础配对的 Avator 单独出仅详情UI幻化药（多皮肤同样不生成默认皮肤药）
        for av_stem, av_res, av_json, _ in avators:
            if av_stem in paired_av:
                continue
            stats["avator_only"] += 1
            base_count += 1
            title = dir_name if base_count == 1 else f"{dir_name}-{base_count:02d}"
            ui_idle = detect_idle(av_json, f"{dir_name}/{av_stem}")
            skins = avator_skins(av_json)
            for skin in (skins if skins else [""]):
                seq += 1
                self_id = int(f"{ITEM_TYPE_TRANSFORM_POTION}{dir_num:04d}{seq:02d}")
                # 资源身份token(=remark token=布局保留键)：皮肤药=AvatorStem skin:皮肤名，否则=AvatorStem
                token = f"{av_stem} skin:{skin}" if skin else av_stem
                preserve_key = (dir_name, token)
                data = {"ui_show_res": av_res,
                        "ui_show_data": make_layout(av_json, ui_scale_k, ui_pos_y, preserve_key, "ui_show_data", 0.17),
                        "ui_show_idle_anim": ui_idle}
                if skin:
                    data["ui_show_skin"] = skin
                    stats["skin"] += 1
                append_item(dir_name, dir_num, seq, title, skin, token, **data)
    return items, stats, warnings


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
            content = old.get(lang) or default_name(lang, item["_name_title"], item["_skin"])
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
    parser = argparse.ArgumentParser(description="OtherSpine Mod 数据生成器（Excel→JsonText 两段式）")
    parser.add_argument("command", nargs="?", default="all", choices=["scan", "export", "all"],
                        help="scan=资源→Excel；export=Excel→JsonText；all=两者（默认）")
    parser.add_argument("--mod-project", required=True, help="MOD 项目根目录")
    parser.add_argument("--deploy-main", default="", help="可选：主项目根目录，构建完成后把 Mod 整个目录部署过去")
    parser.add_argument("--ui-scale-k", type=float, default=645.0, help="详情UI缩放校准常数 K：scale=K/骨架高（默认645，与其他 Mod 同基准）")
    parser.add_argument("--ui-pos-y", type=float, default=0.0, help="详情UI默认Y偏移（默认0）")
    parser.add_argument("--show-scale-k", type=float, default=3159.0, help="小卡UI缩放校准常数 K：scale=K/骨架高（默认3159，与 AeonsEchoSpine 同基准）")
    parser.add_argument("--show-pos-y", type=float, default=-120.0, help="小卡UI默认Y偏移（默认-120，与 AeonsEchoSpine 同基准）")
    parser.add_argument("--reset-layout", action="store_true",
                        help="重建时重新设置卡片位置/大小（show_data/ui_show_data 全部按骨架重算）；默认关闭=按 id 保留已有手调值，仅新资源计算默认值")
    args = parser.parse_args()

    mod_project = Path(args.mod_project)
    source_dir = mod_project / SOURCE_REL
    item_excel = mod_project / ITEM_EXCEL_REL
    lang_excel = mod_project / LANG_EXCEL_REL

    if args.command in ("scan", "all"):
        if not source_dir.is_dir():
            print(f"[错误] 资源目录不存在: {source_dir}")
            sys.exit(1)
        preserved_layout, preserved_dir_nums = ({}, {}) if args.reset_layout else read_preserved_rows(item_excel)
        std_idle = load_std_idle_candidates()
        items, stats, warnings = compute_items(source_dir, args.ui_scale_k, args.ui_pos_y,
                                               args.show_scale_k, args.show_pos_y,
                                               preserved_layout, preserved_dir_nums, std_idle)
        write_item_excel(item_excel, items, mod_project)
        kept = merge_language_excel(lang_excel, items, mod_project)
        print(f"[scan] 重建道具表 {len(items)} 行 → {item_excel}")
        print(f"[scan] 合并语言表（保留人工内容 {kept} 格）→ {lang_excel}")
        print(f"[scan] 出药统计：基础+Avator配对 {stats['paired']}，仅基础 {stats['base_only']}，"
              f"仅Avator {stats['avator_only']}，皮肤药 {stats['skin']}")
        print(f"[scan] 待机检测（标准候选={','.join(std_idle)}）：命中标准 {stats['idle_std']}，替代 {stats['idle_alt']}，无idle {stats['idle_none']}")
        if args.reset_layout:
            print("[scan] --reset-layout：布局三键全部按骨架重算，目录序号重新分配")
        elif preserved_layout:
            print(f"[scan] 按 remark 资源身份(目录名/资源token)保留手调布局 {len(preserved_layout)} 行、按目录名保留目录序号 {len(preserved_dir_nums)} 个（--reset-layout 可强制重算）")
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
            print(f"[错误] 未找到构建产物 {mod_out}/catalog.bin —— 请先在 MOD 项目 Unity 里执行「工具/Mod/OtherSpine/一键构建」")
            sys.exit(1)
        target = Path(args.deploy_main) / "Mods" / MOD_NAME
        if target.exists():
            shutil.rmtree(target)
        shutil.copytree(mod_out, target)
        print(f"[deploy] 已部署 → {target}")
        print("下一步：主项目 Unity 里用 LauncherTest 启动（内含 InitializeAllModsSync），吃幻化药验证形象")


if __name__ == "__main__":
    main()
