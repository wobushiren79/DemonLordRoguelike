---
name: localization-system
description: Demon Lord Roguelike 游戏的多语言(Localization)系统开发指南。使用此SKILL当需要添加新的多语言文本、创建带多语言的配置表、在UI中显示多语言文本、切换语言等。
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

### 支持的语言

```csharp
LanguageEnum
├── cn = 0    - 简体中文
└── en = 1    - 英文
```

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
    public long name;           // 名称文本ID
    public long description;    // 描述文本ID
    
    [JsonIgnore]
    public string name_language { 
        get { return TextHandler.Instance.GetTextById(MyFeatureInfoCfg.fileName, name); } 
    }
    
    [JsonIgnore]
    public string description_language { 
        get { return TextHandler.Instance.GetTextById(MyFeatureInfoCfg.fileName, description); } 
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

`Assets/Resources/JsonText/Language_MyFeatureInfo_cn.txt`：
```json
[
    {"content":"功能名称","id":1},
    {"content":"功能描述内容","id":2}
]
```

`Assets/Resources/JsonText/Language_MyFeatureInfo_en.txt`：
```json
[
    {"content":"Feature Name","id":1},
    {"content":"Feature description content","id":2}
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
| 多语言数据Bean | `Assets/FrameWork/Scripts/Bean/MVC/LanguageBean.cs` |
| UI文本Bean | `Assets/FrameWork/Scripts/Bean/MVC/UITextBean.cs` |
| 语言枚举 | `Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs` |
| 文本管理器 | `Assets/FrameWork/Scripts/Component/Manager/TextManager.cs` |
| 文本处理器 | `Assets/FrameWork/Scripts/Component/Handler/TextHandler.cs` |
| UI多语言组件 | `Assets/FrameWork/Scripts/Component/UI/UITextLanguageView.cs` |
| 多语言JSON文件 | `Assets/Resources/JsonText/Language_*.txt` |
| Bean代码模板 | `Assets/FrameWork/Editor/ScrpitsTemplates/Excel_LanguageEntity.txt` |

---

## 注意事项

1. **文本ID唯一性**：同一配置表内的文本ID必须唯一，不同配置表可以重复
2. **JSON格式**：多语言JSON文件必须使用UTF-8编码，确保中文正常显示
3. **字段命名**：配置表中的文本字段名建议与多语言属性名对应（如`name`对应`name_language`）
4. **延迟加载**：多语言文本是按需加载的，首次访问时会从JSON文件读取
5. **编辑器预览**：在Editor中可以直接使用`UITextLanguageView`预览多语言效果
