---
name: localization-system
description: Demon Lord Roguelike 游戏的多语言(Localization)系统开发指南。使用此SKILL当需要添加新的多语言文本、创建带多语言的配置表、在UI中显示多语言文本、切换语言等。
watched_files:
  - Assets/FrameWork/Scripts/Bean/MVC/LanguageBean.cs
  - Assets/FrameWork/Scripts/Bean/MVC/LanguageBeanPartial.cs
  - Assets/FrameWork/Scripts/Bean/MVC/UITextBean.cs
  - Assets/FrameWork/Scripts/Bean/GameConfigBean.cs
  - Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs
  - Assets/FrameWork/Scripts/Component/Manager/TextManager.cs
  - Assets/FrameWork/Scripts/Component/Handler/TextHandler.cs
  - Assets/FrameWork/Scripts/Component/UI/UITextLanguageView.cs
  - Assets/Resources/JsonText/
---

# 多语言系统开发指南

## 核心概念

### 系统架构

```
LanguageBean              - 多语言数据基础类
LanguageCfg               - 多语言配置管理（按语言/配置表分类存储）
TextManager               - 文本管理器（底层获取逻辑）
TextHandler               - 文本处理器（上层接口）
UITextLanguageView        - UI多语言组件（自动更新Text）
LanguageEnum              - 语言类型枚举
```

### 多语言文件存储

```
Assets/Resources/JsonText/
├── Language_UIText_cn.txt              - UI通用文本（中文）
├── Language_UIText_en.txt              - UI通用文本（英文）
├── Language_BuffInfo_cn.txt            - BUFF名称描述（中文）
├── Language_BuffInfo_en.txt            - BUFF名称描述（英文）
├── Language_ItemsInfo_cn.txt           - 道具名称（中文）
├── Language_ItemsInfo_en.txt           - 道具名称（英文）
└── Language_{CfgName}_{lang}.txt       - 通用命名格式（lang ∈ cn/en/jp/kr/tw/de/fr/ru/es/br/pl/tr）
```

> ⚠️ **真实源是 Excel，不是 `.txt`**：`Language_{CfgName}_{lang}.txt` 都是从 **`excel_language[多语言_FrameWork].xlsx` 里与 `{CfgName}` 同名的工作表**导出的产物（每个工作表列：`id / content_{lang} / content_1_{lang} / remark`，lang 含 cn/en/jp/kr/tw/de/fr/ru/es/br/pl/tr 十二种）。**`Language_UIText_*` 也不例外**——它来自 excel_language 的 `UIText` 工作表（导出器只对文件名含 `excel_language` 的工作簿生成 `Language_{sheet}_{lang}.txt`）；`excel_ui_text[UI文本_FrameWork].xlsx` 只是 UIText 的 id 登记表（导出 `UIText.txt` + UITextBean，`content[language]` 列存的值=多语言 id），**新增 UI 文本两处都要加行**（excel_language 的 UIText 表加 8 语言内容 + excel_ui_text 加登记行），二者可能不同步（历史上 excel_ui_text 漏登记过 id）。注意 excel_language 的 UIText 工作表有历史遗留的**重复列**（`content_jp`~`content_ru` 出现两次），新增行两组列要填相同值。**新增/修改文本必须改对应 Excel 工作表**，再用 ExcelEditorWindow 导出；只改 `.txt` 会在下次导出时被**覆盖丢失**。下文示例若直接写 `.txt` 仅为说明字段结构，落地务必同步 Excel 工作表。

### 支持的语言

```csharp
LanguageEnum
├── cn = 0    - 简体中文
├── en = 1    - 英文
├── jp = 2    - 日语
├── kr = 3    - 韩语
├── tw = 4    - 繁体中文
├── de = 5    - 德语
├── fr = 6    - 法语
├── ru = 7    - 俄语
├── es = 8    - 西班牙语
├── br = 9    - 巴西葡萄牙语
├── pl = 10   - 波兰语
└── tr = 11   - 土耳其语
```

### 新增一种语言的完整流程（以 2026-08 新增 es/br/pl/tr 为例）

1. **枚举**：`LanguageEnum` 末尾追加新值（**只能追加，禁止改已有值**，展示名数组 `languageShowNames` 顺序与之绑定）。
2. **展示名**：`LanguageBeanPartial.cs` 的 `languageShowNames` 末尾追加 `简称/该语言自称`（如 `es/Español`、`br/Português (Brasil)`）。
3. **Steam 映射**：`LanguageCfg.GetInitialLanguage()` 补充 Steam 语言串→新枚举的映射（见上文）。
4. **Excel**：给 `excel_language` **全部工作表**加列——16 列结构的表加 `content_{lang}`，30 列结构的表加 `content_{lang}` 与 `content_1_{lang}`；元数据行（第 2 行类型 `string`、第 3 行中文描述）一并补齐。
5. **导出**：ExcelEditorWindow 导出后自动生成 `Language_{sheet}_{lang}.txt`（导出器 `ExcelToJsonItemForLanguage` 按 `EnumExtension.GetEnumNames<LanguageEnum>()` 驱动，加完枚举即自动识别新列，无需改导出代码）。
6. **字体**：主 UI 字体 `fusion-pixel-10px-monospaced-zh_hans SDF` 的回退链为 zh_hant→ko→ja→**latin**；`fusion-pixel-10px-monospaced-latin SDF` 已烘焙 Latin-1 + Latin Extended-A（西/葡/波/土特殊字符全覆盖），新增其他文字体系的语言时需先核对 SDF 字符表。

---

## 默认语言初始化（Steam 优先）

### 初始化策略

新用户（无 GameConfig 存档）首次启动时按下列优先级决定语言，已有存档则保留用户保存的偏好：

```
1. Steam 已连上（SteamManager.Initialized == true）
   └─ SteamApps.GetCurrentGameLanguage() 返回值
      ├─ schinese → cn   tchinese → tw   其他含 chinese → cn
      ├─ japanese → jp   koreana → kr
      ├─ german → de     french → fr     russian → ru
      ├─ spanish/latam → es   brazilian/portuguese → br
      ├─ polish → pl     turkish → tr
      └─ 其他（english / ...）→ en
2. 未连上 Steam 或异常 → en
```

### 关键代码

**[LanguageBeanPartial.cs](Assets/FrameWork/Scripts/Bean/MVC/LanguageBeanPartial.cs)** — 在 `LanguageCfg` 中提供 `GetInitialLanguage()` 和静态构造（映射全表：schinese→cn、tchinese→tw、含 chinese→cn、japanese→jp、koreana→kr、german→de、french→fr、russian→ru、spanish/latam→es、brazilian/portuguese→br、polish→pl、turkish→tr，其余→en）：

```csharp
public partial class LanguageCfg
{
    static LanguageCfg()
    {
        // 覆盖自动生成文件中的 currentLanguage = ""
        currentLanguage = GetInitialLanguage();
    }

    public static string GetInitialLanguage()
    {
        try
        {
            if (SteamManager.Initialized)
            {
                string steamLanguage = SteamApps.GetCurrentGameLanguage();
                if (!string.IsNullOrEmpty(steamLanguage))
                {
                    if (steamLanguage.Equals("schinese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.cn.GetEnumName();
                    if (steamLanguage.Equals("tchinese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.tw.GetEnumName();
                    if (steamLanguage.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0)
                        return LanguageEnum.cn.GetEnumName();
                    if (steamLanguage.Equals("japanese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.jp.GetEnumName();
                    if (steamLanguage.Equals("koreana", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.kr.GetEnumName();
                    if (steamLanguage.Equals("german", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.de.GetEnumName();
                    if (steamLanguage.Equals("french", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.fr.GetEnumName();
                    if (steamLanguage.Equals("russian", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.ru.GetEnumName();
                    if (steamLanguage.Equals("spanish", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.es.GetEnumName();
                    if (steamLanguage.Equals("latam", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.es.GetEnumName();
                    if (steamLanguage.Equals("brazilian", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.br.GetEnumName();
                    if (steamLanguage.Equals("portuguese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.br.GetEnumName();
                    if (steamLanguage.Equals("polish", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.pl.GetEnumName();
                    if (steamLanguage.Equals("turkish", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.tr.GetEnumName();
                    return LanguageEnum.en.GetEnumName();
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"读取 Steam 语言失败，回退到默认语言 en：{ex.Message}");
        }
        return LanguageEnum.en.GetEnumName();
    }
}
```

**[GameConfigBean.cs](Assets/FrameWork/Scripts/Bean/GameConfigBean.cs)** — `language` 默认空串，`GetLanguage()` 空串时回落到 Steam 检测：

```csharp
//语言（留空时由 LanguageCfg.GetInitialLanguage 判定）
public string language = "";

public LanguageEnum GetLanguage()
{
    string lang = string.IsNullOrEmpty(language) ? LanguageCfg.GetInitialLanguage() : language;
    return EnumExtension.GetEnum<LanguageEnum>(lang);
}
```

### 初始化时序

```
BaseLauncher.Start()
└─ TextHandler.Instance.InitData()
   └─ GameDataHandler.Instance.manager.GetGameConfig()
      ├─ Load() 成功 → 用户保存的 language（"cn" / "en"）
      └─ Load() == null → new GameConfigBean() → language = ""
   └─ gameConfig.GetLanguage()
      └─ language 为空时 → LanguageCfg.GetInitialLanguage()（Steam → cn/en, 否则 cn）
   └─ ChangeLanguageEnum(language)
      └─ LanguageCfg.ChangeLanguageData(language)
         └─ currentLanguage = language
```

### 注意事项

1. **`LanguageBean.cs` 是自动生成文件**：`public static string currentLanguage = "";` 不能直接改，必须通过 `LanguageBeanPartial.cs` 的静态构造覆盖。
2. **静态构造时机**：静态构造在首次访问 `LanguageCfg` 任意成员时触发，由 C# 运行时保证在字段初始化器之后执行——所以 `""` 会被 `GetInitialLanguage()` 的结果覆盖。
3. **Steam 未初始化的窗口**：若 `LanguageCfg` 比 `SteamHandler.Awake()` 更早被访问，`SteamManager.Initialized` 为 false，会回退到 cn，符合"未连上 Steam"的预期。
4. **`GameConfigBean.language` 不再硬编码 `"cn"`**：旧存档中字面值为空串的会触发 Steam 检测，已显式存了 `cn`/`en` 的不变。

---

## 使用现有文本

### 通过ID获取文本

```csharp
// 获取UI通用文本
string text = TextHandler.Instance.GetTextById(1001);

// 获取指定配置表的文本
string buffName = TextHandler.Instance.GetTextById("BuffInfo", 10001);
```

### 在配置Bean中使用多语言

配置表字段存储文本ID，通过属性获取本地化文本：

```csharp
public partial class BuffInfoBean : BaseBean
{
    public long name;           // 文本ID
    public long content;        // 描述文本ID
    
    [JsonIgnore]
    public string name_language { 
        get { return TextHandler.Instance.GetTextById(BuffInfoCfg.fileName, name); } 
    }
    
    [JsonIgnore]
    public string content_language { 
        get { return TextHandler.Instance.GetTextById(BuffInfoCfg.fileName, content); } 
    }
}
```

使用方式：
```csharp
BuffInfoBean buffInfo = BuffInfoCfg.GetItemData(10001);
string name = buffInfo.name_language;      // 获取本地化名称
string desc = buffInfo.content_language;   // 获取本地化描述
```

### UI中显示多语言

**方式1：使用UITextLanguageView组件（推荐静态文本）**

```csharp
// 在Prefab的Text组件上挂载UITextLanguageView
// 设置textId字段为对应的文本ID
```

**方式2：代码动态设置**

```csharp
// 普通Text
Text textUI = GetComponent<Text>();
textUI.text = TextHandler.Instance.GetTextById(1001);

// TextMeshProUGUI
TextMeshProUGUI tmpText = GetComponent<TextMeshProUGUI>();
tmpText.text = TextHandler.Instance.GetTextById(1001);
```

---

## ⚠️ 一个多语言ID承载多条文本（content / content_1 / content_2）

**这是创建带多语言的配置表时必须遵守的核心规则。**

### 规则说明

同一个多语言ID（多语言JSON里的**一行**）最多可以承载 **3 条文本**：

| contentIndex | JSON字段 | 用途约定 |
|--------------|----------|----------|
| 0（默认） | `content`   | 名称 / 主文本 |
| 1 | `content_1` | 详情 / 描述 |
| 2 | `content_2` | 额外文本（备注等） |

由 [TextManager.GetTextById](Assets/FrameWork/Scripts/Component/Manager/TextManager.cs) 的 `contentIndex` 参数选择读取哪一列：

```csharp
public string GetTextById(string cfgName, long id, int contentIndex = 0)
{
    // 约定: textId=0 表示无文本, 静默返回空串(不报错)
    // contentIndex: 0 → content, 1 → content_1, 2 → content_2
}
```

**因此「名称」和「详情」应当共用同一个多语言ID**，分别用 `content`（index 0）和 `content_1`（index 1）取值，**而不是分配两个独立ID**。

### 正确示例（深渊馈赠 AbyssalBlessingInfo —— 标准做法）

配置表 `AbyssalBlessingInfo.txt`：`name` 与 `details` 指向**同一个ID**：
```json
{"name":1000001001,"details":1000001001,"id":1000001001, ...}
```

多语言表 `Language_AbyssalBlessingInfo_cn.txt`：一行同时给出名称和详情：
```json
{"id":1000001001,"content":"增殖","content_1":"随机复制一个已有的魔物"}
```

Bean 中名称读 index 0、详情读 index 1（注意两个属性传入**同一个 id 字段**）：
```csharp
[JsonIgnore]
public string name_language {
    get { return TextHandler.Instance.GetTextById(XxxInfoCfg.fileName, name); }          // content
}
[JsonIgnore]
public string details_language {
    get { return TextHandler.Instance.GetTextById(XxxInfoCfg.fileName, name, 1); }        // content_1（同一个 id）
}
```

### 错误示例（应避免 —— 拆成两个ID）

```json
// ❌ 名称和详情各占一个独立ID，浪费ID、割裂同一条目的文本
{"id":4001001,"content":"生物猎手 I"}
{"id":4001002,"content":"累计击杀 1 只生物"}
```
```csharp
// ❌ 两个独立字段、两个独立ID
public long name;          // 4001001
public long description;   // 4001002
```

### 何时仍可拆分

仅当名称和详情**确实需要独立复用 / 独立维护**（例如多个条目共享同一个名称但详情不同）时，才使用独立ID。**默认一律共用一个ID + content_1。**

---

## 添加新多语言文本

### 1. 添加到现有配置表

如果要在现有配置表（如BuffInfo）中添加新文本：

**步骤1：修改配置Bean**

```csharp
public partial class BuffInfoBean : BaseBean
{
    // 添加新的文本ID字段
    public long new_field;
    
    [JsonIgnore]
    public string new_field_language { 
        get { return TextHandler.Instance.GetTextById(BuffInfoCfg.fileName, new_field); } 
    }
}
```

**步骤2：在Excel中添加文本数据**

```
// Excel中BuffInfo表
id  | name  | content | new_field  | ...
----|-------|---------|------------|----
... | 10001 | 20001   | 30001      | ...
```

**步骤3：添加多语言文本**

在`Assets/Resources/JsonText/Language_BuffInfo_cn.txt`中添加：
```json
{"content":"新的文本内容","id":30001}
```

在`Assets/Resources/JsonText/Language_BuffInfo_en.txt`中添加：
```json
{"content":"New Text Content","id":30001}
```

### 2. 创建全新的多语言配置表

**步骤1：创建配置Bean类**

```csharp
// Assets/Scripts/Bean/MVC/Game/MyFeatureInfoBean.cs
using System;
using Newtonsoft.Json;

[Serializable]
public partial class MyFeatureInfoBean : BaseBean
{
    public long name;           // 名称+详情共用的文本ID（推荐：一个ID承载 content/content_1）
    
    [JsonIgnore]
    public string name_language { 
        get { return TextHandler.Instance.GetTextById(MyFeatureInfoCfg.fileName, name); }        // content（index 0）
    }
    
    [JsonIgnore]
    public string description_language { 
        get { return TextHandler.Instance.GetTextById(MyFeatureInfoCfg.fileName, name, 1); }     // content_1（index 1，同一个 id）
    }
}

public partial class MyFeatureInfoCfg : BaseCfg<long, MyFeatureInfoBean>
{
    public static string fileName = "MyFeatureInfo";  // 必须与多语言文件名一致
    protected static Dictionary<long, MyFeatureInfoBean> dicData = null;
    
    public static MyFeatureInfoBean GetItemData(long key)
    {
        if (dicData == null)
        {
            MyFeatureInfoBean[] arrayData = GetInitData(fileName);
            InitData(arrayData);
        }
        return GetItemData(key, dicData);
    }
    
    public static void InitData(MyFeatureInfoBean[] arrayData)
    {
        dicData = new Dictionary<long, MyFeatureInfoBean>();
        for (int i = 0; i < arrayData.Length; i++)
        {
            MyFeatureInfoBean itemData = arrayData[i];
            dicData.Add(itemData.id, itemData);
        }
    }
}
```

**步骤2：创建多语言JSON文件**

`Assets/Resources/JsonText/Language_MyFeatureInfo_cn.txt`（名称+详情共用一个ID）：
```json
[
    {"id":1,"content":"功能名称","content_1":"功能描述内容"}
]
```

`Assets/Resources/JsonText/Language_MyFeatureInfo_en.txt`：
```json
[
    {"id":1,"content":"Feature Name","content_1":"Feature description content"}
]
```

**步骤3：使用**

```csharp
MyFeatureInfoBean data = MyFeatureInfoCfg.GetItemData(1);
string name = data.name_language;
string desc = data.description_language;
```

---

## 文本替换（动态参数）

当文本中包含变量时使用文本替换功能。

> ⭐ **强约定**：凡是「多语言文本里嵌入运行时数值」的场景，**一律优先使用本机制**（`TextReplaceEnum` 占位符 + `GetTextReplace`），**禁止** `string.Format("{0}")` 特判拼接、也**禁止**把数值写死进静态文本（调数值后 8 语言文本全部过期）。数值源须单一真实源（代码常量），UI 显示与实际逻辑引用同一常量。记忆：`feedback_text_replace_enum`。

### 定义带占位符的文本

```json
{"content":"击杀{KillNum}个敌人","id":1001}
{"content":"造成伤害{AttackDamage}","id":1002}
{"content":"生命值低于{HPRateLess}%","id":1003}
```

### 代码中使用

```csharp
// 创建替换字典
Dictionary<TextReplaceEnum, string> replaces = new Dictionary<TextReplaceEnum, string>
{
    { TextReplaceEnum.KillNum, "10" },
    { TextReplaceEnum.AttackDamage, "500" },
    { TextReplaceEnum.HPRateLess, "30" }
};

// 获取替换后的文本
string text = TextHandler.Instance.GetTextReplace(1001, replaces);
// 结果: "击杀10个敌人"
```

> **两个重载（重要区别）**：
> - `GetTextReplace(long id, dic)` —— **只从 UIText 表**(`UITextCfg`) 按 id 取模板再替换。仅适用于通用 UI 文本。
> - `GetTextReplace(string originText, dic)` —— 直接对**传入的字符串**替换。当模板来自**其他配置表**（如 `AchievementInfo`/`BuffInfo` 等自有 Language 表）时，必须**先**用 `GetTextById(cfgName, id, contentIndex)` 取到模板字符串，**再**调本重载。例：
>
> ```csharp
> // 成就描述: 模板存在 AchievementInfo 表的 details 文本 content_1, {Name} 替换为该级目标值
> // 优先用框架自动生成的 _language 属性取模板(带缓存), 不要手写 GetTextById(fileName, id, idx)
> string template = info.details_language; // = content_1
> var dic = new Dictionary<TextReplaceEnum, string> { { TextReplaceEnum.Name, "100" } };
> string desc = TextHandler.Instance.GetTextReplace(template, dic); // "累计击杀 100 只生物"
> ```
>
> 占位符语法是 `{枚举名}`（如 `{Name}`/`{KillNum}`/`{Time_H}`），与 `TextReplaceEnum` 值同名；字典里给哪个键就替换哪个占位符，模板里写死的文案原样保留。

### 可用的替换类型

```csharp
TextReplaceEnum
├── Name              - 名字
├── Percentage        - 百分比
├── Time_S            - 时间（秒）
├── Time_M            - 时间（分钟）
├── Time_H            - 时间（小时）
├── KillNum           - 击杀数
├── UnderAttackDamage - 承受伤害
├── AttackDamage      - 造成伤害
├── HPRateLess        - 生命值低于百分比
├── RegainHPReceived  - 累计被治疗HP
├── RegainHPCast      - 累计施放治疗HP
├── OnFieldTime       - 在场存活时间(秒)
└── Value             - 通用数值占位（无专属语义枚举时的默认选择，如研究节点距离/冷却秒数）
```

> **选键原则**：有语义占位的优先用语义占位（击杀→`KillNum`、秒→`Time_S`、百分比→`Percentage`），无合适语义时用通用 `Value`；都不合适再在 `TextReplaceEnum`（BaseGameEnum.cs）**追加**新枚举值（不改旧值）。同一数值可同时挂多个键（成就即把目标值同时挂 `{Name}` 与语义键，模板用哪个都能替换）。

### 更多范例

- **BUFF 描述**：`UIViewBuffShowItem` —— `content_language` 模板 + `{Percentage}`/`{Time_S}`/`{Value}` + 按前置条件追加 `{KillNum}` 等键。
- **成就逐级描述**：`AchievementInfoBeanPartial.GetLevelDescription` —— 一条 `{Name}` 模板按等级替换目标值，省去逐级建文本。
- **研究节点名称带待解锁数值**：`ResearchInfoBeanPartial.GetNameLanguageWithLevelDetail` —— 模板 `控制魔王时可进行突进（距离{Value}）`/`突进冷却（{Value}秒）`，按「待解锁等级=min(当前+1,满级)」算数值替换，每级只显示要解锁那一级；数值源 `UserUnlockBean.SPACE_DASH_*` 常量（控制层同引用，单一真实源）。

---

## 切换语言

### 主界面语言选择列表（玩家入口）

主界面 UIMainStart 底部有多语言选择列表，玩家点击 ItemLanguage 直接切换语言：

- **UIMainStart.InitLanguageList()**：OpenUI 时隐藏模板 ui_ItemLanguage → `CptUtil.RemoveChildsByActive` 清理旧实例 → 按 LanguageEnum 数量 `Instantiate` 实时生成（ListLanguage 有 VerticalLayoutGroup + ContentSizeFitter 自动排版）
- **UIViewLanguageItem**（Assets/Scripts/Component/UI/Game/MainStart/）：挂在 ItemLanguage 模板上（含 Button 组件），Awake 时 AutoLink 绑定 `ui_ItemText`（ItemText 子物体）与 `ui_ItemLanguage`（自身 Button）
- **文本格式**：`LanguageCfg.GetLanguageShowName(language)` → "英文简称/该语言自称"（如 `cn/中文`、`en/English`、`jp/日本語`），展示名数组在 LanguageBeanPartial.cs（顺序与 LanguageEnum 一致）
- **点击切换**：`SetLanguage` → `TextHandler.ChangeLanguageEnum` → `SaveGameConfig` 立即持久化 → `RefreshAllUI` + 当前 UI SetActive 重启刷新

> 设置界面 UIGameSettingForGame 里的多语言下拉选择代码已注释（迁移到主界面列表），恢复时取消注释即可。

### 运行时切换语言

```csharp
// 切换到英文
TextHandler.Instance.ChangeLanguageEnum(LanguageEnum.en);

// 切换到中文
TextHandler.Instance.ChangeLanguageEnum(LanguageEnum.cn);
```

### 获取当前语言

```csharp
string currentLang = LanguageCfg.currentLanguage;  // "en" 或 "cn"
LanguageEnum langEnum = GameDataHandler.Instance.manager.GetGameConfig().GetLanguage();
```

### 语言切换后刷新UI

切换语言后需要手动刷新UI显示：

```csharp
// 方案1：遍历所有UITextLanguageView组件
UITextLanguageView[] textViews = FindObjectsByType<UITextLanguageView>(FindObjectsSortMode.None);
foreach (var view in textViews)
{
    view.RefreshUI();
}

// 方案2：发送全局事件通知UI刷新
EventHandler.Instance.TriggerEvent(EventsInfo.Language_Change);
```

---

## 常用代码模板

### 快速添加多语言支持到UI

```csharp
public class MyUIComponent : BaseUIComponent
{
    public Text titleText;
    public Text descText;
    
    public void SetData(long titleId, long descId)
    {
        titleText.text = TextHandler.Instance.GetTextById(titleId);
        descText.text = TextHandler.Instance.GetTextById(descId);
    }
}
```

### 带参数的多语言文本

```csharp
public string GetLevelText(int level)
{
    Dictionary<TextReplaceEnum, string> replaces = new Dictionary<TextReplaceEnum, string>
    {
        { TextReplaceEnum.Name, level.ToString() }
    };
    return TextHandler.Instance.GetTextReplace(1001, replaces);
}
```

### 防止文本换行（空格替换为不间断空格）

```csharp
// 将普通空格替换为不间断空格，防止在空格处换行
string text = TextHandler.Instance.GetTextByIdNoBreakingSpace("BuffInfo", 10001);
```

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 多语言数据Bean（自动生成） | `Assets/FrameWork/Scripts/Bean/MVC/LanguageBean.cs` |
| 多语言Bean手写扩展（含 Steam 默认语言判定） | `Assets/FrameWork/Scripts/Bean/MVC/LanguageBeanPartial.cs` |
| UI文本Bean | `Assets/FrameWork/Scripts/Bean/MVC/UITextBean.cs` |
| 游戏配置（含 language 字段、`GetLanguage()` 空串回退） | `Assets/FrameWork/Scripts/Bean/GameConfigBean.cs` |
| 语言枚举 | `Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs` |
| 文本管理器 | `Assets/FrameWork/Scripts/Component/Manager/TextManager.cs` |
| 文本处理器 | `Assets/FrameWork/Scripts/Component/Handler/TextHandler.cs` |
| UI多语言组件 | `Assets/FrameWork/Scripts/Component/UI/UITextLanguageView.cs` |
| 多语言JSON文件 | `Assets/Resources/JsonText/Language_*.txt` |
| Bean代码模板 | `Assets/FrameWork/Editor/ScriptsTemplates/Excel_LanguageEntity.txt` |

---

## 注意事项

1. **文本ID唯一性**：同一配置表内的文本ID必须唯一，不同配置表可以重复
2. **textId=0 保留为「无文本」约定**：`GetTextById` 对 id=0 静默返回空串，不报「没有找到文本」错误。不需要名字的字段（如议会随机议员的 `name`）应配 0，而不是指向不存在的行
3. **一个ID承载多条文本**：名称与详情应共用同一个ID（`content` / `content_1` / `content_2`，最多3条），通过 `GetTextById(..., contentIndex)` 区分，禁止默认就拆成两个独立ID（详见上方 ⚠️ 专章）
4. **JSON格式**：多语言JSON文件必须使用UTF-8编码，确保中文正常显示
5. **字段命名**：配置表中的文本字段名建议与多语言属性名对应（如`name`对应`name_language`）
6. **延迟加载**：多语言文本是按需加载的，首次访问时会从JSON文件读取
7. **编辑器预览**：在Editor中可以直接使用`UITextLanguageView`预览多语言效果
