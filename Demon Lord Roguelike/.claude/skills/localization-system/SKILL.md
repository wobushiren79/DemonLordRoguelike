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
└── Language_{CfgName}_{lang}.txt       - 通用命名格式
```

> ⚠️ **真实源是 Excel，不是 `.txt`**：`Language_{CfgName}_{cn,en}.txt` 都是从 **`excel_language[多语言_FrameWork].xlsx` 里与 `{CfgName}` 同名的工作表**导出的产物（每个工作表列：`id / content_cn / content_en / content_1_cn / content_1_en / remark`；`Language_UIText_*` 则来自 `excel_ui_text`）。**新增/修改文本必须改对应 Excel 工作表**，再用 ExcelEditorWindow 导出；只改 `.txt` 会在下次导出时被**覆盖丢失**。下文示例若直接写 `.txt` 仅为说明字段结构，落地务必同步 Excel 工作表。

### 支持的语言

```csharp
LanguageEnum
├── cn = 0    - 简体中文
└── en = 1    - 英文
```

---

## 默认语言初始化（Steam 优先）

### 初始化策略

新用户（无 GameConfig 存档）首次启动时按下列优先级决定语言，已有存档则保留用户保存的偏好：

```
1. Steam 已连上（SteamManager.Initialized == true）
   └─ SteamApps.GetCurrentGameLanguage() 返回值
      ├─ 含 "chinese"（schinese / tchinese）→ cn
      └─ 其他（english / german / ...）   → en
2. 未连上 Steam 或异常 → cn
```

### 关键代码

**[LanguageBeanPartial.cs](Assets/FrameWork/Scripts/Bean/MVC/LanguageBeanPartial.cs)** — 在 `LanguageCfg` 中提供 `GetInitialLanguage()` 和静态构造：

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
                    if (steamLanguage.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0)
                        return LanguageEnum.cn.GetEnumName();
                    return LanguageEnum.en.GetEnumName();
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"读取 Steam 语言失败，回退到默认语言 cn：{ex.Message}");
        }
        return LanguageEnum.cn.GetEnumName();
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

当文本中包含变量时使用文本替换功能：

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
└── HPRateLess        - 生命值低于百分比
```

---

## 切换语言

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
UITextLanguageView[] textViews = FindObjectsOfType<UITextLanguageView>();
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
2. **一个ID承载多条文本**：名称与详情应共用同一个ID（`content` / `content_1` / `content_2`，最多3条），通过 `GetTextById(..., contentIndex)` 区分，禁止默认就拆成两个独立ID（详见上方 ⚠️ 专章）
3. **JSON格式**：多语言JSON文件必须使用UTF-8编码，确保中文正常显示
4. **字段命名**：配置表中的文本字段名建议与多语言属性名对应（如`name`对应`name_language`）
5. **延迟加载**：多语言文本是按需加载的，首次访问时会从JSON文件读取
6. **编辑器预览**：在Editor中可以直接使用`UITextLanguageView`预览多语言效果
