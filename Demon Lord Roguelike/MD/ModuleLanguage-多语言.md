# 多语言模块 (Localization Module) 分析文档

## 一、模块概述

多语言模块负责游戏中所有文本的本地化支持，包括UI文本、配置表文本（Buff名称/描述、道具名称等）的动态加载、语言切换和文本替换（参数化）功能。支持简体中文(cn)和英文(en)两种语言。

---

## 二、核心数据结构

### 2.1 LanguageBean（多语言数据基础类）

**文件**: `Bean/MVC/LanguageBean.cs`

多语言数据的基础结构，用于JSON反序列化。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `long` | 文本唯一标识ID |
| `content` | `string` | 本地化文本内容 |

---

### 2.2 UITextBean（UI文本配置表）

**文件**: `Bean/MVC/UITextBean.cs`

UI通用文本的配置表数据。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `long` | 文本ID |
| `name` | `long` | （预留字段） |
| `content` | `long` | 实际存储文本ID，关联多语言文件 |

**配置管理器** `UITextCfg`:
- 继承 `BaseCfg<long, UITextBean>`，从 `"UIText"` JSON文件加载
- `GetItemData(long key)` — 按ID查询

**多语言属性** (`UITextBeanPartial.cs`):
```csharp
[JsonIgnore]
public string content_language { 
    get { return TextHandler.Instance.GetTextById(UITextCfg.fileName, content); } 
}
```

---

### 2.3 配置表的多语言支持

其他配置表（如BuffInfo、ItemsInfo）通过字段存储文本ID，通过Partial扩展提供多语言属性：

**示例 - BuffInfoBean**:
```csharp
public partial class BuffInfoBean : BaseBean
{
    public long name;           // 名称文本ID
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

**示例 - ItemsInfoBean**:
```csharp
public partial class ItemsInfoBean : BaseBean
{
    public long name;           // 道具名称文本ID
    
    [JsonIgnore]
    public string name_language { 
        get { return TextHandler.Instance.GetTextById(ItemsInfoCfg.fileName, name); } 
    }
}
```

---

## 三、枚举定义

**文件**: `Enums/BaseGameEnum.cs`

### LanguageEnum（语言类型）
```csharp
cn = 0,     // 简体中文
en = 1      // 英文
```

### TextReplaceEnum（文本替换类型）
```csharp
Name,               // 名字
Percentage,         // 百分比
Time_S,             // 时间（秒）
Time_M,             // 时间（分钟）
Time_H,             // 时间（小时）
KillNum,            // 击杀数
UnderAttackDamage,  // 承受伤害
AttackDamage,       // 造成伤害
HPRateLess          // 生命值低于百分比
```

---

## 四、文本管理系统

### 4.1 TextManager（文本管理器）

**文件**: `Component/Manager/TextManager.cs`

底层文本获取逻辑，负责多语言文件的加载和缓存。

| 方法 | 说明 |
|------|------|
| `GetTextData(string cfgName, long id, string language = null)` | 获取指定配置表、指定ID的文本 |
| `GetTextData(long id, string language = null)` | 获取UI通用文本（默认使用UIText配置表） |

**特性**:
- 延迟加载：首次访问某语言/配置表时从JSON文件读取
- 缓存机制：已加载的文本按 `语言/配置表名` 分类存储
- 自动回退：找不到当前语言文本时返回空字符串

**多语言文件存储路径**:
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

---

### 4.2 TextHandler（文本处理器）

**文件**: `Component/Handler/TextHandler.cs`

上层文本处理接口，提供游戏代码中使用的文本获取方法。

| 方法 | 说明 |
|------|------|
| `GetTextById(long id)` | 获取UI通用文本（默认配置表） |
| `GetTextById(string cfgName, long id)` | 获取指定配置表的文本 |
| `GetTextReplace(long id, Dictionary<TextReplaceEnum, string> replaceData)` | 获取带参数替换的UI文本 |
| `GetTextReplace(string cfgName, long id, Dictionary<TextReplaceEnum, string> replaceData)` | 获取带参数替换的指定配置表文本 |
| `GetTextByIdNoBreakingSpace(string cfgName, long id)` | 获取文本并将空格替换为不间断空格（防止换行） |
| `ChangeLanguageEnum(LanguageEnum language)` | 切换当前语言 |

**使用示例**:
```csharp
// 获取UI通用文本
string text = TextHandler.Instance.GetTextById(1001);

// 获取Buff名称
string buffName = TextHandler.Instance.GetTextById("BuffInfo", 10001);

// 获取带参数的文本（如"击杀10个敌人"）
Dictionary<TextReplaceEnum, string> replaces = new Dictionary<TextReplaceEnum, string>
{
    { TextReplaceEnum.KillNum, "10" }
};
string text = TextHandler.Instance.GetTextReplace(1001, replaces);
```

---

### 4.3 LanguageCfg（语言配置管理）

**文件**: `Component/Manager/LanguageCfg.cs`

语言配置的静态管理类。

| 字段/方法 | 说明 |
|-----------|------|
| `currentLanguage` | 当前语言代码（"cn"/"en"） |
| `GetLanguageName(LanguageEnum language)` | 获取语言名称 |

---

## 五、UI层结构

### 5.1 UITextLanguageView（UI多语言组件）

**文件**: `Component/UI/UITextLanguageView.cs`

自动化的UI文本多语言组件，挂载在Text或TextMeshProUGUI组件上。

| 字段 | 说明 |
|------|------|
| `textId` | 文本ID |
| `cfgName` | 配置表名（为空则使用默认UI文本） |
| `isReplaceSpace` | 是否将空格替换为不间断空格 |

**功能**:
- 自动根据当前语言刷新文本显示
- 支持Unity Text和TextMeshProUGUI组件
- 语言切换时自动更新

**使用方式**:
```csharp
// 在Prefab的Text组件上挂载UITextLanguageView
// 设置textId字段为对应的文本ID
// 设置cfgName字段（如使用BuffInfo表则填"BuffInfo"）
```

---

### 5.2 手动设置UI文本

**方式1：通过TextHandler获取**
```csharp
// 普通Text
Text textUI = GetComponent<Text>();
textUI.text = TextHandler.Instance.GetTextById(1001);

// TextMeshProUGUI
TextMeshProUGUI tmpText = GetComponent<TextMeshProUGUI>();
tmpText.text = TextHandler.Instance.GetTextById(1001);
```

**方式2：通过配置Bean的多语言属性**
```csharp
BuffInfoBean buffInfo = BuffInfoCfg.GetItemData(10001);
string name = buffInfo.name_language;      // 获取本地化名称
string desc = buffInfo.content_language;   // 获取本地化描述
```

---

## 六、语言切换

### 6.1 切换语言

```csharp
// 切换到英文
TextHandler.Instance.ChangeLanguageEnum(LanguageEnum.en);

// 切换到中文
TextHandler.Instance.ChangeLanguageEnum(LanguageEnum.cn);
```

### 6.2 获取当前语言

```csharp
// 获取语言代码字符串
string currentLang = LanguageCfg.currentLanguage;  // "en" 或 "cn"

// 获取语言枚举
LanguageEnum langEnum = GameDataHandler.Instance.manager.GetGameConfig().GetLanguage();
```

### 6.3 刷新UI显示

切换语言后需要手动刷新UI：

**方式1：遍历所有UITextLanguageView组件**
```csharp
UITextLanguageView[] textViews = FindObjectsOfType<UITextLanguageView>();
foreach (var view in textViews)
{
    view.RefreshUI();
}
```

**方式2：发送全局事件通知UI刷新**
```csharp
EventHandler.Instance.TriggerEvent(EventsInfo.Language_Change);
```

---

## 七、文本替换（动态参数）

### 7.1 定义带占位符的文本

在JSON多语言文件中：
```json
{"content":"击杀{KillNum}个敌人","id":1001}
{"content":"造成伤害{AttackDamage}","id":1002}
{"content":"生命值低于{HPRateLess}%","id":1003}
```

### 7.2 代码中使用

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

---

## 八、添加新多语言文本

### 8.1 添加到现有配置表

**步骤1**：在Excel配置表中添加文本ID字段（如BuffInfo表添加`new_field`列）

**步骤2**：在配置Bean中添加多语言属性
```csharp
public partial class BuffInfoBean : BaseBean
{
    public long new_field;
    
    [JsonIgnore]
    public string new_field_language { 
        get { return TextHandler.Instance.GetTextById(BuffInfoCfg.fileName, new_field); } 
    }
}
```

**步骤3**：在多语言JSON文件中添加文本

`Assets/Resources/JsonText/Language_BuffInfo_cn.txt`:
```json
{"content":"新的文本内容","id":30001}
```

`Assets/Resources/JsonText/Language_BuffInfo_en.txt`:
```json
{"content":"New Text Content","id":30001}
```

### 8.2 创建全新的多语言配置表

参考SKILL.md中的详细步骤，需创建：
1. 配置Bean类（继承BaseBean）
2. 配置管理器（继承BaseCfg）
3. 多语言JSON文件（按命名格式`Language_{CfgName}_{lang}.txt`）

---

## 九、关联系统

### 9.1 Buff系统关联

**文件**: `Bean/MVC/Game/BuffInfoBean.cs`

Buff名称和描述通过多语言系统实现本地化：
- `name` 字段存储文本ID → `name_language` 获取本地化名称
- `content` 字段存储文本ID → `content_language` 获取本地化描述

### 9.2 道具系统关联

**文件**: `Bean/MVC/Game/ItemsInfoBean.cs`

道具名称通过多语言系统实现本地化：
- `name` 字段存储文本ID → `name_language` 获取本地化名称

### 9.3 道具类型关联

**文件**: `Bean/MVC/Game/ItemsTypeBean.cs`

道具类型名称通过多语言系统实现本地化：
- `name` 字段存储文本ID → `name_language` 获取本地化类型名称

### 9.4 UI系统关联

所有UI文本通过`UITextLanguageView`组件或`TextHandler`获取本地化内容。

---

## 十、数据流总结

```
[Excel配置表]
    │
    ▼
[导表工具] → JSON配置文件
    │
    ├──► Language_UIText_cn.txt / Language_UIText_en.txt
    ├──► Language_BuffInfo_cn.txt / Language_BuffInfo_en.txt
    ├──► Language_ItemsInfo_cn.txt / Language_ItemsInfo_en.txt
    └──► Language_{CfgName}_{lang}.txt
             │
             ▼
    TextManager (延迟加载/缓存)
             │
             ├──► UITextCfg ◄─── UITextBean (UI文本配置)
             ├──► BuffInfoCfg ◄─── BuffInfoBean (Buff名称/描述)
             ├──► ItemsInfoCfg ◄─── ItemsInfoBean (道具名称)
             └──► ...
                      │
                      ▼
    TextHandler (上层接口)
                      │
                      ├──► GetTextById() → 获取文本
                      ├──► GetTextReplace() → 带参数文本
                      └──► ChangeLanguageEnum() → 切换语言
                                   │
                                   ▼
    UITextLanguageView (UI组件自动刷新)
    或手动设置UI文本
```

---

## 十一、文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 多语言数据Bean | `Assets/FrameWork/Scripts/Bean/MVC/LanguageBean.cs` |
| UI文本Bean | `Assets/FrameWork/Scripts/Bean/MVC/UITextBean.cs` |
| 语言枚举 | `Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs` |
| 文本管理器 | `Assets/FrameWork/Scripts/Component/Manager/TextManager.cs` |
| 文本处理器 | `Assets/FrameWork/Scripts/Component/Handler/TextHandler.cs` |
| UI多语言组件 | `Assets/FrameWork/Scripts/Component/UI/UITextLanguageView.cs` |
| 多语言JSON文件 | `Assets/Resources/JsonText/Language_*.txt` |

---

## 十二、注意事项

1. **文本ID唯一性**：同一配置表内的文本ID必须唯一，不同配置表可以重复
2. **JSON格式**：多语言JSON文件必须使用UTF-8编码，确保中文正常显示
3. **字段命名**：配置表中的文本字段名建议与多语言属性名对应（如`name`对应`name_language`）
4. **延迟加载**：多语言文本是按需加载的，首次访问时会从JSON文件读取
5. **编辑器预览**：在Editor中可以直接使用`UITextLanguageView`预览多语言效果
