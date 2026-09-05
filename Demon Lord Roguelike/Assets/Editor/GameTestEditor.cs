using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static ExcelUtil;

[CustomEditor(typeof(LauncherTest))]
public partial class GameTestEditor : Editor
{
    LauncherTest launcher;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        launcher = (LauncherTest)target;

        EditorGUI.BeginChangeCheck();

        DrawTitleHeader();

        switch (launcher.testSceneType)
        {
            case TestSceneTypeEnum.FightSceneTest:
                DrawFightSceneTest();
                break;
            case TestSceneTypeEnum.CardTest:
                DrawCardTest();
                break;
            case TestSceneTypeEnum.Base:
                DrawBaseTest();
                break;
            case TestSceneTypeEnum.RewardSelect:
                DrawRewardSelectTest();
                break;
            case TestSceneTypeEnum.DoomCouncil:
                DrawDoomCouncilTest();
                break;
            case TestSceneTypeEnum.ResearchUI:
                DrawResearchTest();
                break;
            case TestSceneTypeEnum.AbyssalBlessing:
                DrawAbyssalBlessingTest();
                break;
            case TestSceneTypeEnum.CreatureSacrifice:
                DrawCreatureSacrificeTest();
                break;
            case TestSceneTypeEnum.CreatureVat:
                DrawCreatureVatTest();
                break;
            case TestSceneTypeEnum.CreatureJuicer:
                DrawCreatureJuicerTest();
                break;
            case TestSceneTypeEnum.EffectTest:
                DrawEffectTest();
                break;
            case TestSceneTypeEnum.ConversationTest:
                DrawConversationTest();
                break;
            case TestSceneTypeEnum.StoryTest:
                DrawStoryTest();
                break;
            case TestSceneTypeEnum.NormalGame:
                DrawNormalGameTest();
                break;
        }

        DrawGlobalTest();

        if (EditorGUI.EndChangeCheck())
        {
            SaveAllPreferences();
        }
    }
    
    private void DrawGlobalTest()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🔧 全局通用测试", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f);
        if (GUILayout.Button("▶️ 执行通用测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            // TODO: 后续添加具体执行内容
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);
    }

    private void DrawTitleHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎮 游戏测试工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
    }

    private void DrawFightSceneTest()
    {
        showFightSceneTest = EditorGUILayout.Foldout(showFightSceneTest, "⚔️ 战斗场景测试", true);
        if (!showFightSceneTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        // 战斗测试模式选择
        EditorGUILayout.BeginVertical("box");
        fightTestMode = (FightTestModeEnum)EditorGUILayout.EnumPopup(new GUIContent("战斗测试模式", "普通模式=自定义场景/敌人/BUFF的战斗；征服模式BOSS关=指定世界与难度直接进入征服BOSS关；单体测试模式=道路长度10/道路数量1/进攻生物数量1/进攻间隔1固定不显示，其余同普通模式"), fightTestMode);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // 征服模式BOSS关：单独的简化配置，直接进入征服BOSS关
        if (fightTestMode == FightTestModeEnum.ConquerBoss)
        {
            DrawFightSceneTestConquerBoss();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
            return;
        }

        // 运行按钮(单体测试模式与普通模式共用同一进入逻辑，仅按钮文案区分)
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        string startFightBtnText = fightTestMode == FightTestModeEnum.SingleUnit ? "▶️ 开始单体测试" : "▶️ 开始战斗测试";
        if (GUILayout.Button(startFightBtnText, GUILayout.Height(30)) && Application.isPlaying)
        {
            FightBean fightData = GetTestData();
            launcher.StartForFightSceneTest(fightData);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        // 基础设置
        showFightBasicSettings = EditorGUILayout.Foldout(showFightBasicSettings, "📋 基础设置", true);
        if (showFightBasicSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            testDataCardNum = EditorGUILayout.IntField(new GUIContent("卡片数量", "初始生成的卡片数量"), testDataCardNum);
            EditorGUILayout.BeginHorizontal();
            fightSceneId = EditorGUILayout.IntField(new GUIContent("测试场景 ID", "战斗场景的 ID"), fightSceneId);
            if (GUILayout.Button("📂 场景表", GUILayout.Width(80)))
            {
                string scenePath = Path.Combine(Application.dataPath, "Data/Excel/excel_fight_scene[战斗场景].xlsx");
                if (File.Exists(scenePath))
                {
                    Application.OpenURL("file:///" + scenePath.Replace("\\", "/"));
                }
                else
                {
                    EditorUtility.DisplayDialog("文件未找到", $"找不到战斗场景配置表:\n{scenePath}", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();
            // 卡片生物ID列表(手动输入 + 下拉选择已有生物)
            DrawFightCardIdList("卡片生物 IDs");
            fightDefenseCoreId = EditorGUILayout.IntField(new GUIContent("魔王生物 ID", "防守核心(魔王)的生物 ID，默认 2001 骷髅战士"), fightDefenseCoreId);
            fightDemonLordMP = EditorGUILayout.FloatField(new GUIContent("魔王蓝量", "战斗开始时魔王的当前魔力值(同时会把魔力上限提升到不低于该值)，默认 9999"), fightDemonLordMP);
            // 单体测试模式下道路数量/道路长度为固定值，不显示
            if (fightTestMode != FightTestModeEnum.SingleUnit)
            {
                fightSceneRoadNum = EditorGUILayout.IntField(new GUIContent("道路数量", "战斗场景的道路数量"), fightSceneRoadNum);
                fightSceneRoadLength = EditorGUILayout.IntField(new GUIContent("道路长度", "每条道路的长度"), fightSceneRoadLength);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // 敌人设置
        showFightEnemySettings = EditorGUILayout.Foldout(showFightEnemySettings, "👹 敌人设置", true);
        if (showFightEnemySettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            // 单体测试模式下进攻生物数量/进攻间隔为固定值，不显示
            if (fightTestMode != FightTestModeEnum.SingleUnit)
            {
                fightSceneAttackNum = EditorGUILayout.IntField(new GUIContent("进攻生物数量", "每波进攻的生物数量"), fightSceneAttackNum);
                fightSceneAttackDelay = EditorGUILayout.FloatField(new GUIContent("进攻间隔", "进攻波次之间的延迟时间"), fightSceneAttackDelay);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("进攻生物 IDs （NPCID）", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 刷新列表", GUILayout.Width(90)))
            {
                //配置重导后清空选项缓存，下次绘制时重建
                fightEnemyNpcOptions = null;
                //Cfg 的 static 缓存只加载一次(不随 JSON 重导失效)，需一并清掉才能读到新行
                ClearCfgBaseStaticCache(typeof(NpcInfoCfg));
            }
            EditorGUILayout.EndHorizontal();

            //进攻生物NPC下拉选项懒加载(id + 中文名，首项"(手动输入)"占位)
            EnsureFightEnemyNpcOptions();
            if (fightEnemyNpcOptions == null || fightEnemyNpcOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("未读取到 NPC 配置，请检查配置表导出或点「刷新列表」。", MessageType.Warning);
            }
            else
            {
                DrawIdListWithDropdown(enemyIds, "敌人", fightEnemyNpcOptions, fightEnemyNpcIds);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📂 NPC配置表", GUILayout.Width(100)))
            {
                string path = Path.Combine(Application.dataPath, "Data/Excel/excel_npc_info[NPC信息].xlsx");
                if (File.Exists(path))
                {
                    Application.OpenURL("file:///" + path.Replace("\\", "/"));
                }
                else
                {
                    EditorUtility.DisplayDialog("文件未找到", $"找不到 NPC 配置表:\n{path}", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // BUFF 设置
        showFightBuffSettings = EditorGUILayout.Foldout(showFightBuffSettings, "✨ BUFF 设置", true);
        if (showFightBuffSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            attackModeAttackTestId = EditorGUILayout.IntField(new GUIContent("进攻方攻击模块 ID", "进攻方使用的攻击模块测试 ID"), attackModeAttackTestId);
            attackModeDefenseTestId = EditorGUILayout.IntField(new GUIContent("防守方攻击模块 ID", "防守方使用的攻击模块测试 ID"), attackModeDefenseTestId);

            EditorGUILayout.Space(5);
            buffSelfAttackTestId = EditorGUILayout.TextField(new GUIContent("进攻方 BUFF", "进攻方携带的 BUFF 测试 ID"), buffSelfAttackTestId);
            buffSelfDefenseTestId = EditorGUILayout.TextField(new GUIContent("防守方 BUFF", "防守方携带的 BUFF 测试 ID"), buffSelfDefenseTestId);
            buffTestId = EditorGUILayout.TextField(new GUIContent("全局攻击 BUFF", "攻击时触发的 BUFF 测试 ID"), buffTestId);
            DrawFightAbyssalBlessingSettings();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 绘制卡片生物ID列表(标题行含「刷新列表/生物表」按钮；每行: 手动ID输入 + 生物下拉[选中覆盖该行ID] + 删除)。
    /// 战斗测试的「卡片生物 IDs」与基地测试的「手下生物 IDs」共用同一份 fightCardIds 数据。
    /// </summary>
    /// <param name="title">列表标题</param>
    private void DrawFightCardIdList(string title)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(title, "可手动输入生物 ID，或从下拉选择已有生物(选中后覆盖该行 ID)；卡片数量超过列表数量时循环使用列表"), EditorStyles.boldLabel);
        if (GUILayout.Button("🔄 刷新列表", GUILayout.Width(90)))
        {
            //配置重导后清空选项缓存，下次绘制时重建
            fightCardCreatureOptions = null;
            //Cfg 的 static 缓存只加载一次(不随 JSON 重导失效)，需一并清掉才能读到新行
            ClearCfgBaseStaticCache(typeof(CreatureInfoCfg));
        }
        if (GUILayout.Button("📂 生物表", GUILayout.Width(80)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_creature_info[生物信息].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到生物配置表:\n{path}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        //生物下拉选项懒加载([id] 中文名，首项"(手动输入)"占位)
        EnsureFightCardCreatureOptions();
        if (fightCardCreatureOptions == null || fightCardCreatureOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("未读取到生物配置，请检查配置表导出或点「刷新列表」。", MessageType.Warning);
            return;
        }
        DrawIdListWithDropdown(fightCardIds, "生物", fightCardCreatureOptions, fightCardCreatureIds);
    }

    /// <summary>
    /// 绘制「手动ID输入 + 下拉选择」的 ID 列表(每行: 序号 + 手动ID输入框 + 下拉[选中覆盖该行ID] + 删除；底部: 添加/移除最后一个)。
    /// options/optionIds 首项约定为"(手动输入)"占位(当前ID不在选项中时显示，选中它不改动ID)。
    /// </summary>
    /// <param name="idList">要编辑的 ID 列表</param>
    /// <param name="itemLabelPrefix">每行序号标签前缀(如"生物"/"敌人")</param>
    /// <param name="options">下拉选项(首项为手动占位)</param>
    /// <param name="optionIds">下拉选项对应的 ID(首项为占位哨兵)</param>
    private void DrawIdListWithDropdown(List<long> idList, string itemLabelPrefix, GUIContent[] options, long[] optionIds)
    {
        for (int i = 0; i < idList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{itemLabelPrefix} {i + 1}", GUILayout.Width(60));
            idList[i] = EditorGUILayout.LongField(idList[i], GUILayout.Width(100));
            //下拉选择：当前ID不在选项中时回落到首项"(手动输入)"占位(不覆盖手动值)，选中有效项后覆盖该行ID
            int selectIndex = Array.IndexOf(optionIds, idList[i]);
            if (selectIndex < 0) selectIndex = 0;
            int newIndex = EditorGUILayout.Popup(selectIndex, options);
            if (newIndex > 0) idList[i] = optionIds[newIndex];
            if (GUILayout.Button("🗑️", GUILayout.Width(30)))
            {
                idList.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        //新增行默认复制最后一行的值，便于批量添加同类生物；空列表时补 0(显示为手动占位)
        if (GUILayout.Button($"➕ 添加{itemLabelPrefix}"))
        {
            idList.Add(idList.Count > 0 ? idList[idList.Count - 1] : 0);
        }
        if (idList.Count > 0 && GUILayout.Button("🗑️ 移除最后一个"))
        {
            idList.RemoveAt(idList.Count - 1);
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 懒加载战斗测试-卡片生物下拉选项(首项"(手动输入)"占位，其余为 [id] 中文名，按id排序)。
    /// 中文名直读 Language_CreatureInfo_cn.txt，不切 LanguageCfg 语言——避免 Inspector 绘制时篡改正在运行游戏的语言。
    /// </summary>
    private void EnsureFightCardCreatureOptions()
    {
        if (fightCardCreatureOptions != null) return;
        var allData = CreatureInfoCfg.GetAllArrayData();
        //加载中文多语言(CreatureInfo.name → Language_CreatureInfo_cn.txt)
        Dictionary<long, LanguageBean> dicLanguage = LoadLanguageForCn("Language_CreatureInfo_cn.txt");
        var listEntries = new List<KeyValuePair<long, GUIContent>>();
        for (int i = 0; i < allData.Length; i++)
        {
            var creatureInfo = allData[i];
            if (creatureInfo == null) continue;
            //先检查多语言行是否存在，避免对缺失行刷 LogError
            string creatureName;
            if (creatureInfo.name == 0)
            {
                creatureName = "(无名字)";
            }
            else if (dicLanguage.TryGetValue(creatureInfo.name, out LanguageBean languageBean) && !languageBean.content.IsNull())
            {
                creatureName = languageBean.content;
            }
            else
            {
                creatureName = "(未配置名字)";
            }
            listEntries.Add(new KeyValuePair<long, GUIContent>(creatureInfo.id, new GUIContent($"[{creatureInfo.id}] {creatureName}")));
        }
        //按 id 排序保证下拉顺序稳定
        listEntries.Sort((a, b) => a.Key.CompareTo(b.Key));
        //首项插入"(手动输入)"占位(当前ID不在选项中时显示，选中不改动ID)
        fightCardCreatureOptions = new GUIContent[listEntries.Count + 1];
        fightCardCreatureIds = new long[listEntries.Count + 1];
        fightCardCreatureOptions[0] = new GUIContent("(手动输入)");
        fightCardCreatureIds[0] = 0;
        for (int i = 0; i < listEntries.Count; i++)
        {
            fightCardCreatureIds[i + 1] = listEntries[i].Key;
            fightCardCreatureOptions[i + 1] = listEntries[i].Value;
        }
    }

    /// <summary>
    /// 懒加载战斗测试-进攻生物NPC下拉选项(首项"(手动输入)"占位，其余为 id + 中文名，按id排序)
    /// </summary>
    private void EnsureFightEnemyNpcOptions()
    {
        if (fightEnemyNpcOptions != null) return;
        BuildNpcOptions(true, out fightEnemyNpcOptions, out fightEnemyNpcIds);
    }

    /// <summary>
    /// 绘制战斗测试-深渊馈赠设置(下拉选择馈赠族[显示中文名与效果] + 目标等级)
    /// </summary>
    private void DrawFightAbyssalBlessingSettings()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("深渊馈赠", EditorStyles.boldLabel);
        if (GUILayout.Button("🔄 刷新列表", GUILayout.Width(90)))
        {
            //配置重导后清空选项缓存，下次绘制时重建
            abyssalBlessingFamilyOptions = null;
            //Cfg 的 static 缓存只加载一次（不随 JSON 重导失效），需一并清掉才能读到新行
            ClearAbyssalBlessingInfoCfgCache();
        }
        if (GUILayout.Button("📂 配置表", GUILayout.Width(80)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到深渊馈赠配置表:\n{path}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        //下拉选项懒加载(族根=parent_id==0 的行)
        EnsureAbyssalBlessingOptions();
        if (abyssalBlessingFamilyOptions == null || abyssalBlessingFamilyOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("未读取到深渊馈赠配置，请检查配置表导出。", MessageType.Warning);
            return;
        }

        for (int i = 0; i < abyssalBlessingFightTestList.Count; i++)
        {
            var item = abyssalBlessingFightTestList[i];
            EditorGUILayout.BeginHorizontal();
            //馈赠族下拉(选项含中文名字与效果)
            int selectIndex = System.Array.IndexOf(abyssalBlessingFamilyRootIds, item.familyRootId);
            if (selectIndex < 0) selectIndex = 0;
            selectIndex = EditorGUILayout.Popup(selectIndex, abyssalBlessingFamilyOptions);
            item.familyRootId = abyssalBlessingFamilyRootIds[selectIndex];
            //目标等级(仅升级链族显示；level=0 的可重复馈赠无等级概念)
            int maxLevel = AbyssalBlessingInfoCfg.GetFamilyMaxLevel(item.familyRootId);
            if (maxLevel > 0)
            {
                EditorGUILayout.LabelField("Lv", GUILayout.Width(20));
                item.level = EditorGUILayout.IntField(item.level, GUILayout.Width(40));
                item.level = Mathf.Clamp(item.level, 1, maxLevel);
            }
            else
            {
                item.level = 0;
                EditorGUILayout.LabelField("(可重复)", GUILayout.Width(60));
            }
            if (GUILayout.Button("🗑️", GUILayout.Width(30)))
            {
                abyssalBlessingFightTestList.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("➕ 添加馈赠"))
        {
            abyssalBlessingFightTestList.Add(new AbyssalBlessingFightTestItem { familyRootId = abyssalBlessingFamilyRootIds[0], level = 1 });
        }
        if (abyssalBlessingFightTestList.Count > 0 && GUILayout.Button("🗑️ 移除最后一个"))
        {
            abyssalBlessingFightTestList.RemoveAt(abyssalBlessingFightTestList.Count - 1);
        }
        EditorGUILayout.EndHorizontal();
        if (abyssalBlessingFightTestList.Count > 0)
        {
            EditorGUILayout.HelpBox("同一馈赠族添加多行时，后添加的会替换先添加的（同族升级替换机制）。", MessageType.None);
        }
    }

    /// <summary>
    /// 清空 Cfg 基类的 static 数据缓存（反射访问 NonPublic static 的 dicData/arrayData）：
    /// Cfg 的 static 缓存只加载一次（不随 JSON 重导失效），JSON 重导后若不清理且不触发 domain reload，编辑器读取的仍是旧数据。
    /// </summary>
    private void ClearCfgBaseStaticCache(System.Type cfgType)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.FlattenHierarchy;
        cfgType.GetField("dicData", flags)?.SetValue(null, null);
        cfgType.GetField("arrayData", flags)?.SetValue(null, null);
    }

    /// <summary>
    /// 清空 AbyssalBlessingInfoCfg 的全部 static 缓存（基类数据本体 dicData/arrayData + 族根/最大等级 dicFamilyRoot/dicFamilyMaxLevel）
    /// </summary>
    private void ClearAbyssalBlessingInfoCfgCache()
    {
        var cfgType = typeof(AbyssalBlessingInfoCfg);
        //基类数据本体缓存
        ClearCfgBaseStaticCache(cfgType);
        //族根/最大等级缓存
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.FlattenHierarchy;
        cfgType.GetField("dicFamilyRoot", flags)?.SetValue(null, null);
        cfgType.GetField("dicFamilyMaxLevel", flags)?.SetValue(null, null);
    }

    /// <summary>
    /// 懒加载深渊馈赠下拉选项(族根列表，选项文本含中文名字与效果描述)。
    /// 中文文本直读 Language_AbyssalBlessingInfo_cn.txt，不切 LanguageCfg 语言——避免 Inspector 绘制时篡改正在运行游戏的语言。
    /// </summary>
    private void EnsureAbyssalBlessingOptions()
    {
        if (abyssalBlessingFamilyOptions != null) return;
        var allData = AbyssalBlessingInfoCfg.GetAllData();
        //加载中文多语言(名字=content，效果=content_1)
        Dictionary<long, LanguageBean> dicLanguage = LoadAbyssalBlessingLanguageForCn();
        //按 id 排序保证下拉顺序稳定；族根 = parent_id==0 的行(含 level=0 可重复馈赠 与 level=1 升级链族根)
        var listInfo = new List<AbyssalBlessingInfoBean>(allData.Values);
        listInfo.Sort((a, b) => a.id.CompareTo(b.id));
        var listOptions = new List<GUIContent>();
        var listRootIds = new List<long>();
        for (int i = 0; i < listInfo.Count; i++)
        {
            var info = listInfo[i];
            if (info == null || info.parent_id != 0) continue;
            dicLanguage.TryGetValue(info.name, out LanguageBean nameBean);
            dicLanguage.TryGetValue(info.details, out LanguageBean detailsBean);
            //多级族标注可选等级范围，可重复馈赠(level=0)标注[可重复]
            int maxLevel = AbyssalBlessingInfoCfg.GetFamilyMaxLevel(info.id);
            string levelHint = maxLevel > 1 ? $"[1~{maxLevel}级] " : (maxLevel == 0 ? "[可重复] " : "");
            listOptions.Add(new GUIContent($"[{info.id}] {levelHint}{nameBean?.content} - {detailsBean?.content_1}"));
            listRootIds.Add(info.id);
        }
        abyssalBlessingFamilyOptions = listOptions.ToArray();
        abyssalBlessingFamilyRootIds = listRootIds.ToArray();
    }

    /// <summary>
    /// 直读指定中文语言表(Resources/JsonText/<languageFileName>)，返回 id→语言行 字典；读取失败返回空字典(调用方需做兜底显示)
    /// </summary>
    private Dictionary<long, LanguageBean> LoadLanguageForCn(string languageFileName)
    {
        var dicLanguage = new Dictionary<long, LanguageBean>();
        string path = Path.Combine(Application.dataPath, "Resources/JsonText/" + languageFileName);
        if (!File.Exists(path)) return dicLanguage;
        var arrayData = JsonConvert.DeserializeObject<LanguageBean[]>(File.ReadAllText(path));
        if (arrayData == null) return dicLanguage;
        for (int i = 0; i < arrayData.Length; i++)
        {
            if (arrayData[i] != null)
                dicLanguage[arrayData[i].id] = arrayData[i];
        }
        return dicLanguage;
    }

    /// <summary>
    /// 直读深渊馈赠中文语言表(Language_AbyssalBlessingInfo_cn.txt)，返回 id→语言行 字典；读取失败返回空字典(选项文本退化为 null 占位)
    /// </summary>
    private Dictionary<long, LanguageBean> LoadAbyssalBlessingLanguageForCn()
    {
        return LoadLanguageForCn("Language_AbyssalBlessingInfo_cn.txt");
    }

    /// <summary>
    /// 绘制征服模式BOSS关测试配置(指定世界与难度，直接进入征服BOSS关)
    /// </summary>
    private void DrawFightSceneTestConquerBoss()
    {
        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 进入征服BOSS关", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForConquerBossTest(conquerTestWorldId, conquerTestDifficultyLevel);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        // 参数配置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        conquerTestWorldId = EditorGUILayout.LongField(new GUIContent("世界 ID", "征服模式的世界 ID (对应 world_id)"), conquerTestWorldId);
        if (GUILayout.Button("📂 征服配置表", GUILayout.Width(110)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到征服模式配置表:\n{path}", "确定");
            }
        }
        //世界 ID 右侧：打开世界配置表(excel_game_world_info)
        if (GUILayout.Button("📂 世界配置表", GUILayout.Width(110)))
        {
            string worldPath = Path.Combine(Application.dataPath, "Data/Excel/excel_game_world_info[游戏世界信息].xlsx");
            if (File.Exists(worldPath))
            {
                Application.OpenURL("file:///" + worldPath.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到世界配置表:\n{worldPath}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();
        conquerTestDifficultyLevel = EditorGUILayout.IntField(new GUIContent("难度等级", "征服模式的难度等级 (对应 level)"), conquerTestDifficultyLevel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.HelpBox("将关卡总数设为 1，使首关即为 BOSS 关，启动后直接进入指定世界/难度的征服模式 BOSS 关。", MessageType.Info);
    }

    private void DrawCardTest()
    {
        showCardTest = EditorGUILayout.Foldout(showCardTest, "🃏 卡片测试", true);
        if (!showCardTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 显示卡片", GUILayout.Height(30)) && Application.isPlaying)
        {
            FightCreatureBean fightCreature;
            if (creatureId == 0)
            {
                var npcInfo = NpcInfoCfg.GetItemData(npcInfoId);
                fightCreature = CreatureHandler.Instance.GetFightCreatureData(npcInfo, CreatureFightTypeEnum.FightDefense);
            }
            else
            {
                fightCreature = CreatureHandler.Instance.GetFightCreatureData(creatureId, CreatureFightTypeEnum.FightDefense);
            }
            fightCreature.creatureData.AddSkinForBase();
            launcher.StartForCardTest(fightCreature);
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = new Color(0.4f, 0.7f, 0.9f);
        if (GUILayout.Button("🎛️ 卡片编辑器(自由设置稀有度/等级/颜色)", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForCreatureCardEditor(creatureId, npcInfoId);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox("「显示卡片」= 图标尺寸校准(UITestCard预制)；「卡片编辑器」= 纯代码GUI面板实时预览小卡+大卡详情，可自由设置稀有度/等级/生物或NPC，并自定义稀有度板色/等级颜色(可写回配置表)。", MessageType.Info);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        creatureId = EditorGUILayout.IntField(new GUIContent("生物 ID", "要显示的卡片生物 ID，0 表示使用 NPC ID"), creatureId);
        npcInfoId = EditorGUILayout.IntField(new GUIContent("NPC ID", "NPC 信息 ID，当生物 ID 为 0 时使用"), npcInfoId);
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    private void DrawBaseTest()
    {
        showBaseSceneTest = EditorGUILayout.Foldout(showBaseSceneTest, "🏰 基地测试", true);
        if (!showBaseSceneTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始基地测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            if (fightCardIds.Count > 0)
            {
                CreatureBean creatureData = new CreatureBean(creatureId);
                creatureData.AddSkinForBase();
                launcher.StartForBaseTest(creatureData);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        creatureId = EditorGUILayout.IntField(new GUIContent("魔王生物 ID", "魔王角色的生物 ID"), creatureId);
        // 手下生物ID列表(与战斗测试共用同一份 fightCardIds，手动输入 + 下拉选择已有生物)
        DrawFightCardIdList("手下生物 IDs");
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    private void DrawRewardSelectTest()
    {
        showRewardSelectTest = EditorGUILayout.Foldout(showRewardSelectTest, "🎁 奖励选择测试", true);
        if (!showRewardSelectTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始奖励选择", GUILayout.Height(30)) && Application.isPlaying)
        {
            RewardSelectTestData testData = new RewardSelectTestData(
                rewardSelectRarity, 
                rewardSelectAddAttribute, 
                rewardSelectCrystalNum,
                rewardSelectCreateEquipNum,
                rewardSelectCreateItemNum,
                rewardSelectNumMax,
                rewardSelectEquipDemonLordRate);
            launcher.StartForRewardSelect(testData);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        rewardSelectRarity = (RarityEnum)EditorGUILayout.EnumPopup(new GUIContent("装备品质", "生成装备的品质等级 (N/R/SR/SSR)"), rewardSelectRarity);
        rewardSelectAddAttribute = EditorGUILayout.IntField(new GUIContent("属性加成", "装备的额外属性加成值"), rewardSelectAddAttribute);
        rewardSelectCrystalNum = EditorGUILayout.IntField(new GUIContent("魔晶数量", "魔晶道具的基础数量"), rewardSelectCrystalNum);
        rewardSelectCreateEquipNum = EditorGUILayout.IntField(new GUIContent("装备生成数量", "生成的装备道具数量"), rewardSelectCreateEquipNum);
        rewardSelectCreateItemNum = EditorGUILayout.IntField(new GUIContent("道具生成数量", "生成的道具总数（包含装备）"), rewardSelectCreateItemNum);
        rewardSelectNumMax = EditorGUILayout.IntField(new GUIContent("最大选择次数", "玩家可以选择奖励的最大次数"), rewardSelectNumMax);
        rewardSelectEquipDemonLordRate = EditorGUILayout.Slider(new GUIContent("魔王专属概率", "装备是魔王专属的概率 (0-1)"), rewardSelectEquipDemonLordRate, 0f, 1f);
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 绘制终焉议会测试配置(议案 ID + 加载中文名字 + 打开配置表)
    /// </summary>
    private void DrawDoomCouncilTest()
    {
        showDoomCouncilTest = EditorGUILayout.Foldout(showDoomCouncilTest, "📜 终焉议会测试", true);
        if (!showDoomCouncilTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始终焉议会", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForDoomCouncil(doomCouncilBillId);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(3);

        // 直接进入议会并加载所有固定议员(用于测试全部固定议员的显示/参数)
        GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
        if (GUILayout.Button("▶️ 查看所有固定议员", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForDoomCouncilAllFixed(doomCouncilBillId);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("「查看所有固定议员」会跳过随机议员生成，直接把配置表中所有议会固定议员(npc_type=2)各生成 1 名放入议会，用于测试其显示与参数。议案 ID 仍需有效(用于生成投票态度)。", MessageType.Info);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");

        // 议案 ID 输入 + 右侧两个加载按钮(加载中文名字 / 打开配置表)
        EditorGUILayout.BeginHorizontal();
        doomCouncilBillId = EditorGUILayout.LongField(new GUIContent("议会议案 ID", "终焉议会的议案 ID"), doomCouncilBillId);
        // 加载名字：根据议案 ID 读取配置表得到中文名字并显示
        if (GUILayout.Button("🏷️ 加载名字", GUILayout.Width(100)))
        {
            doomCouncilBillNameLoaded = LoadDoomCouncilBillName(doomCouncilBillId);
        }
        // 打开对应的配置表(excel_doom_council_info)
        if (GUILayout.Button("📂 配置表", GUILayout.Width(90)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_doom_council_info[终焉议会信息].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到终焉议会配置表:\n{path}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        // 显示已加载的中文名字
        if (!string.IsNullOrEmpty(doomCouncilBillNameLoaded))
        {
            EditorGUILayout.LabelField("议案名字", doomCouncilBillNameLoaded);
        }

        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 根据议案 ID 加载终焉议会议案的中文名字(编辑器模式下直接读配置表+多语言表,不依赖运行时单例)
    /// </summary>
    /// <param name="billId">议会议案 ID</param>
    /// <returns>中文名字；未找到时返回提示文本</returns>
    private string LoadDoomCouncilBillName(long billId)
    {
        DoomCouncilInfoBean billInfo = DoomCouncilInfoCfg.GetItemData(billId);
        if (billInfo == null)
        {
            return $"[未找到] 议案 ID {billId} 不存在于配置表";
        }
        // 切到中文语言后通过 LanguageCfg 直接取文本(编辑器模式不依赖 TextHandler 单例)
        LanguageCfg.ChangeLanguageData(LanguageEnum.cn);
        LanguageBean languageBean = LanguageCfg.GetItemData(DoomCouncilInfoCfg.fileName, billInfo.name);
        if (languageBean == null || string.IsNullOrEmpty(languageBean.content))
        {
            return $"[无中文名] 议案 ID {billId} 的多语言文本(textId {billInfo.name})为空";
        }
        return languageBean.content;
    }

    private void DrawResearchTest()
    {
        showResearchTest = EditorGUILayout.Foldout(showResearchTest, "🔬 研究 UI 测试", true);
        if (!showResearchTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 打开研究 UI", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForResearchUI();
        }
        GUI.backgroundColor = Color.white;

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    private void DrawAbyssalBlessingTest()
    {
        showAbyssalBlessingTest = EditorGUILayout.Foldout(showAbyssalBlessingTest, "🌀 深渊馈赠 UI 测试", true);
        if (!showAbyssalBlessingTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 打开深渊馈赠 UI", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForAbyssalBlessingUI(abyssalBlessingTestIds);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("深渊馈赠 IDs （最多展示前 3 个）", EditorStyles.boldLabel);

        for (int i = 0; i < abyssalBlessingTestIds.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"馈赠 {i + 1}", GUILayout.Width(60));
            abyssalBlessingTestIds[i] = EditorGUILayout.LongField(abyssalBlessingTestIds[i]);
            if (GUILayout.Button("🗑️", GUILayout.Width(30)))
            {
                abyssalBlessingTestIds.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("➕ 添加馈赠"))
        {
            abyssalBlessingTestIds.Add(0);
        }
        if (abyssalBlessingTestIds.Count > 0 && GUILayout.Button("🗑️ 移除最后一个"))
        {
            abyssalBlessingTestIds.RemoveAt(abyssalBlessingTestIds.Count - 1);
        }
        if (GUILayout.Button("📂 配置表", GUILayout.Width(100)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到深渊馈赠配置表:\n{path}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 绘制正常游戏启动测试(走真实开始流程，免去切换 GameScene)
    /// </summary>
    private void DrawNormalGameTest()
    {
        showNormalGameTest = EditorGUILayout.Foldout(showNormalGameTest, "🎬 正常游戏启动", true);
        if (!showNormalGameTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 正常启动游戏", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForNormalGame();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("走与正式游戏一致的开始流程：清理运行时数据 → 加载基地场景 → 打开主菜单(UIMainStart)。无需切换到 GameScene。", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 绘制粒子特效测试(打开纯代码特效测试面板，下拉选特效id后在10x10平面随机位置播放)
    /// </summary>
    private void DrawEffectTest()
    {
        showEffectTest = EditorGUILayout.Foldout(showEffectTest, "✨ 粒子特效测试", true);
        if (!showEffectTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始粒子特效测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForEffectTest();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("打开纯代码特效测试面板(不依赖预制)：下拉选择特效id，点播放后在10x10平面(顶面高度0)上方1格随机位置播放。注意：持久型特效为全局单例，同一特效重复播放会复用/移动原实例。", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 绘制对话系统测试(说话NPC下拉/手动输入 + 自由文本 + 开始展示对话)
    /// </summary>
    private void DrawConversationTest()
    {
        showConversationTest = EditorGUILayout.Foldout(showConversationTest, "💬 对话系统测试", true);
        if (!showConversationTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        // 说话NPC选择(下拉 + 手动输入)
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("说话 NPC", EditorStyles.boldLabel);
        if (GUILayout.Button("🔄 刷新列表", GUILayout.Width(90)))
        {
            //配置重导后清空选项缓存，下次绘制时重建
            conversationTestNpcOptions = null;
            //Cfg 的 static 缓存只加载一次(不随 JSON 重导失效)，需一并清掉才能读到新行
            ClearCfgBaseStaticCache(typeof(NpcInfoCfg));
        }
        if (GUILayout.Button("📂 NPC配置表", GUILayout.Width(100)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_npc_info[NPC信息].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到 NPC 配置表:\n{path}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        //NPC下拉选项懒加载(id + 中文名)
        EnsureConversationTestNpcOptions();
        if (conversationTestNpcOptions == null || conversationTestNpcOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("未读取到 NPC 配置，请检查配置表导出或点「刷新列表」。", MessageType.Warning);
        }
        else
        {
            conversationTestNpcSelectIndex = EditorGUILayout.Popup(
                new GUIContent("NPC 下拉选择", "从 NpcInfo 配置表选择说话NPC(id + 中文名)；「手动 NPC ID」非 0 时优先于下拉"),
                Mathf.Clamp(conversationTestNpcSelectIndex, 0, conversationTestNpcOptions.Length - 1),
                conversationTestNpcOptions);
        }
        conversationTestNpcId = EditorGUILayout.LongField(new GUIContent("手动 NPC ID", "非 0 时优先于下拉选择；NPC ID 为 long(议会随机议员等大 id 超 int 上限)"), conversationTestNpcId);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // 对话文本(自由输入)
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(new GUIContent("对话文本", "要展示的对话内容(自由输入，不走多语言)，点击开始后由对话系统逐字展示"));
        conversationTestContent = EditorGUILayout.TextArea(conversationTestContent, GUILayout.MinHeight(60));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始对话展示", GUILayout.Height(30)) && Application.isPlaying)
        {
            //解析目标NPC：手动ID非0优先，否则取下拉选择
            long targetNpcId = conversationTestNpcId;
            if (targetNpcId == 0)
            {
                if (conversationTestNpcIds == null || conversationTestNpcIds.Length == 0)
                {
                    EditorUtility.DisplayDialog("提示", "NPC 列表为空，请点「刷新列表」或改用手动 NPC ID。", "确定");
                    return;
                }
                int index = Mathf.Clamp(conversationTestNpcSelectIndex, 0, conversationTestNpcIds.Length - 1);
                targetNpcId = conversationTestNpcIds[index];
            }
            if (NpcInfoCfg.GetItemData(targetNpcId) == null)
            {
                EditorUtility.DisplayDialog("提示", $"找不到 NPC 配置: {targetNpcId}", "确定");
                return;
            }
            if (conversationTestContent.IsNull())
            {
                EditorUtility.DisplayDialog("提示", "对话文本为空，请输入要展示的文本。", "确定");
                return;
            }
            launcher.StartForConversationTest(targetNpcId, conversationTestContent);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("在测试场景直接打开对话界面(UIGameConversation)：显示所选NPC的名字/头像，逐字展示输入文本(带说话音效)，文本动画播完后再点背景关闭。贿赂入口为真实逻辑，已开启测试模拟不会写回真实存档。", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 懒加载对话测试NPC下拉选项(id + 中文名，按id排序)
    /// </summary>
    private void EnsureConversationTestNpcOptions()
    {
        if (conversationTestNpcOptions != null) return;
        BuildNpcOptions(false, out conversationTestNpcOptions, out conversationTestNpcIds);
    }

    /// <summary>
    /// 绘制故事演出测试(故事下拉/手动输入 + 播放演出)
    /// </summary>
    private void DrawStoryTest()
    {
        showStoryTest = EditorGUILayout.Foldout(showStoryTest, "📖 故事演出测试", true);
        if (!showStoryTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        // 测试故事选择(下拉 + 手动输入)
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试故事", EditorStyles.boldLabel);
        if (GUILayout.Button("🔄 刷新列表", GUILayout.Width(90)))
        {
            //配置重导后清空选项缓存，下次绘制时重建
            storyTestOptions = null;
            //Cfg 的 static 缓存只加载一次(不随 JSON 重导失效)，需一并清掉才能读到新行
            ClearCfgBaseStaticCache(typeof(StoryInfoCfg));
        }
        if (GUILayout.Button("📂 故事配置表", GUILayout.Width(100)))
        {
            string path = Path.Combine(Application.dataPath, "Data/Excel/excel_story_info[故事信息].xlsx");
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path.Replace("\\", "/"));
            }
            else
            {
                EditorUtility.DisplayDialog("文件未找到", $"找不到故事配置表:\n{path}", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        //故事下拉选项懒加载(id + 中文名 + 触发类型/场景/条件)
        EnsureStoryTestOptions();
        if (storyTestOptions == null || storyTestOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("未读取到故事配置，请检查配置表导出或点「刷新列表」。", MessageType.Warning);
        }
        else
        {
            storyTestSelectIndex = EditorGUILayout.Popup(
                new GUIContent("故事下拉选择", "从 StoryInfo 配置表选择要测试的故事；「手动故事ID」非 0 时优先于下拉"),
                Mathf.Clamp(storyTestSelectIndex, 0, storyTestOptions.Length - 1),
                storyTestOptions);
        }
        storyTestId = EditorGUILayout.LongField(new GUIContent("手动故事ID", "非 0 时优先于下拉选择；StoryInfo.id"), storyTestId);
        //存档槽位选择(0=当前测试数据,1~3=读取对应存档作为运行时数据;全程测试模拟不写回真实存档)
        storyTestSaveSlot = EditorGUILayout.IntPopup(
            new GUIContent("存档槽位", "0=使用当前测试数据(InitTestData 伪造数据)；1~3=读取对应存档槽位(UserData_1/2/3)作为运行时数据进行故事测试(测试模拟,不写回真实存档)"),
            storyTestSaveSlot,
            new[] { new GUIContent("当前测试数据"), new GUIContent("存档 1"), new GUIContent("存档 2"), new GUIContent("存档 3") },
            new[] { 0, 1, 2, 3 });
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 播放故事演出", GUILayout.Height(30)) && Application.isPlaying)
        {
            //解析目标故事：手动ID非0优先，否则取下拉选择
            long targetStoryId = storyTestId;
            if (targetStoryId == 0)
            {
                if (storyTestIds == null || storyTestIds.Length == 0)
                {
                    EditorUtility.DisplayDialog("提示", "故事列表为空，请点「刷新列表」或改用手动故事ID。", "确定");
                    return;
                }
                int index = Mathf.Clamp(storyTestSelectIndex, 0, storyTestIds.Length - 1);
                targetStoryId = storyTestIds[index];
            }
            if (StoryInfoCfg.GetItemData(targetStoryId) == null)
            {
                EditorUtility.DisplayDialog("提示", $"找不到故事配置: {targetStoryId}", "确定");
                return;
            }
            launcher.StartForStoryTest(targetStoryId, storyTestSaveSlot);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("按故事配置的演出场景自动进入对应场景(基地/战斗/终焉议会)后强制播放演出；战斗场景用内置默认测试战斗数据。存档槽位选 1~3 时先读取该存档作为运行时数据(基地场景故事可看到真实基地状态),全程测试模拟不写回真实存档；测试场景不注册自动触发,这里直接调 StoryHandler.PlayStory。", MessageType.Info);

        EditorGUILayout.Space(10);

        // 清除存档故事演出数据(删除指定槽位的 UserStory 拆分存档,让故事触发条件重新生效,便于反复测试真实演出)
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("清除存档故事演出数据", EditorStyles.boldLabel);
        //槽位切换后旧状态文本/已播故事下拉失效,清空待重新查询
        EditorGUI.BeginChangeCheck();
        storyTestClearSlot = EditorGUILayout.IntPopup(
            new GUIContent("目标存档槽位", "要清除故事演出已播记录的存档槽位(UserStory_{槽位} 拆分存档);仅 1~3 真实存档"),
            storyTestClearSlot,
            new[] { new GUIContent("存档 1"), new GUIContent("存档 2"), new GUIContent("存档 3") },
            new[] { 1, 2, 3 });
        if (EditorGUI.EndChangeCheck())
        {
            storyTestClearStatus = null;
            storyTestRemoveOptions = null;
            storyTestRemoveIds = null;
            storyTestRemoveSelectIndex = 0;
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 查询状态", GUILayout.Width(90)))
        {
            RefreshStoryClearStatus();
        }
        EditorGUILayout.LabelField(storyTestClearStatus ?? "未查询", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.4f);
        if (GUILayout.Button("🗑️ 清除该存档的故事演出数据", GUILayout.Height(24)))
        {
            ClearStoryDataForSlot(storyTestClearSlot);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox("删除指定槽位的故事演出已播记录(UserStory 拆分存档),故事触发条件将重新生效,便于进游戏反复测试同一存档的真实演出;不影响主档与其它数据。若游戏正在运行且当前加载的正是该槽位,会同步清空运行时内存记录。", MessageType.None);

        EditorGUILayout.Space(5);
        // 删除指定故事(仅移除选中故事的一条已播记录,其余保留;选项来自「查询状态」时构建的已播记录)
        EditorGUILayout.LabelField("删除指定故事(仅移除一条已播记录)", EditorStyles.boldLabel);
        if (storyTestRemoveOptions == null || storyTestRemoveOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("暂无可删除的已播故事,请先点「🔍 查询状态」或该槽位无已播记录。", MessageType.Info);
        }
        else
        {
            storyTestRemoveSelectIndex = EditorGUILayout.Popup(
                new GUIContent("已播故事", "从该槽位已播记录中选择要单独删除的故事(先「🔍 查询状态」刷新列表)"),
                Mathf.Clamp(storyTestRemoveSelectIndex, 0, storyTestRemoveOptions.Length - 1),
                storyTestRemoveOptions);
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.4f);
            if (GUILayout.Button("🗑️ 删除指定的故事数据", GUILayout.Height(24)))
            {
                long targetStoryId = storyTestRemoveIds[Mathf.Clamp(storyTestRemoveSelectIndex, 0, storyTestRemoveIds.Length - 1)];
                RemoveStoryDataForSlot(storyTestClearSlot, targetStoryId);
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.HelpBox("仅删除下拉选中故事的已播记录(其它已播故事保留),该故事触发条件重新生效;不影响主档与其它数据。若游戏正在运行且当前加载的正是该槽位,会同步清空运行时内存中的该条记录。", MessageType.None);
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 刷新清除区的存档故事数据状态显示(已播故事数量)并重建已播故事下拉(删除指定故事用)
    /// </summary>
    private void RefreshStoryClearStatus()
    {
        var service = new UserDataService(storyTestClearSlot);
        var storyData = service.LoadStoryData();
        BuildStoryRemoveOptions(storyData);
        if (storyData == null)
        {
            storyTestClearStatus = "该槽位暂无故事存档(无已播记录)";
            return;
        }
        int count = storyData.GetDicPlayedStory().Count;
        storyTestClearStatus = count > 0 ? $"已播放 {count} 个故事" : "故事存档存在,无已播记录";
    }

    /// <summary>
    /// 构建已播故事下拉选项(id + 中文名,按id排序;空/无已播记录时清成 null)
    /// 中文名直读 Language_StoryInfo_cn.txt,不切 LanguageCfg 语言——避免 Inspector 绘制时篡改正在运行游戏的语言
    /// </summary>
    private void BuildStoryRemoveOptions(UserStoryBean storyData)
    {
        storyTestRemoveOptions = null;
        storyTestRemoveIds = null;
        storyTestRemoveSelectIndex = 0;
        if (storyData == null || storyData.GetDicPlayedStory().Count == 0)
            return;
        Dictionary<long, LanguageBean> dicLanguage = LoadLanguageForCn("Language_StoryInfo_cn.txt");
        var listEntries = new List<KeyValuePair<long, GUIContent>>();
        foreach (var kv in storyData.GetDicPlayedStory())
        {
            var storyInfo = StoryInfoCfg.GetItemData(kv.Key);
            string storyName = "(未配置名字)";
            if (storyInfo != null && storyInfo.name != 0 && dicLanguage.TryGetValue(storyInfo.name, out LanguageBean languageBean) && !languageBean.content.IsNull())
                storyName = languageBean.content;
            listEntries.Add(new KeyValuePair<long, GUIContent>(kv.Key, new GUIContent($"{kv.Key}  {storyName}")));
        }
        //按 id 排序保证下拉顺序稳定(与故事测试下拉一致)
        listEntries.Sort((a, b) => a.Key.CompareTo(b.Key));
        storyTestRemoveOptions = new GUIContent[listEntries.Count];
        storyTestRemoveIds = new long[listEntries.Count];
        for (int i = 0; i < listEntries.Count; i++)
        {
            storyTestRemoveIds[i] = listEntries[i].Key;
            storyTestRemoveOptions[i] = listEntries[i].Value;
        }
    }

    /// <summary>
    /// 清除指定槽位的故事演出数据(二次确认后删除 UserStory 拆分存档;
    /// 运行时若当前加载的正是该槽位,同步清空内存已播记录与触发条件耗尽缓存,防止后续写盘写回旧记录/触发被短路)
    /// </summary>
    /// <param name="slot">存档槽位(1~3)</param>
    private void ClearStoryDataForSlot(int slot)
    {
        if (!EditorUtility.DisplayDialog("清除故事演出数据",
            $"确定清除存档 {slot} 的故事演出已播记录吗?\n清除后该存档的故事触发条件将重新生效(不影响主档与其它数据)。",
            "清除", "取消"))
        {
            return;
        }
        var service = new UserDataService(slot);
        service.DeleteStoryData();
        //游戏运行中且当前加载的正是该槽位:同步清内存,保持与磁盘一致
        if (Application.isPlaying)
        {
            var userData = GameDataHandler.Instance.manager.GetUserData();
            if (userData != null && userData.saveIndex == slot)
            {
                userData.GetUserStoryData().GetDicPlayedStory().Clear();
                var storyManager = StoryHandler.Instance.manager;
                storyManager.setExhaustedCondition.Clear();
                storyManager.exhaustedForStoryData = null;
            }
        }
        RefreshStoryClearStatus();
        EditorUtility.DisplayDialog("完成", $"已清除存档 {slot} 的故事演出数据。", "确定");
    }

    /// <summary>
    /// 删除指定槽位的单个故事已播记录(二次确认后仅移除该故事,其余保留;
    /// 运行时若当前加载的正是该槽位,同步清空内存中的该条记录与触发条件耗尽缓存,防止后续写盘写回旧记录/触发被短路)
    /// </summary>
    /// <param name="slot">存档槽位(1~3)</param>
    /// <param name="storyId">故事ID(StoryInfo.id)</param>
    private void RemoveStoryDataForSlot(int slot, long storyId)
    {
        if (!EditorUtility.DisplayDialog("删除故事演出数据",
            $"确定删除存档 {slot} 的该条故事已播记录吗?\n故事ID:{storyId}\n仅移除这条记录,其余已播故事保留;该故事触发条件重新生效(不影响主档与其它数据)。",
            "删除", "取消"))
        {
            return;
        }
        var service = new UserDataService(slot);
        service.RemoveStoryData(storyId);
        //游戏运行中且当前加载的正是该槽位:同步清内存,保持与磁盘一致
        if (Application.isPlaying)
        {
            var userData = GameDataHandler.Instance.manager.GetUserData();
            if (userData != null && userData.saveIndex == slot)
            {
                userData.GetUserStoryData().RemoveStoryPlayed(storyId);
                var storyManager = StoryHandler.Instance.manager;
                storyManager.setExhaustedCondition.Clear();
                storyManager.exhaustedForStoryData = null;
            }
        }
        RefreshStoryClearStatus();
        EditorUtility.DisplayDialog("完成", $"已删除存档 {slot} 的故事数据(故事ID:{storyId})。", "确定");
    }

    /// <summary>
    /// 懒加载故事测试下拉选项(id + 中文名 + [触发类型/场景/条件]，按id排序)
    /// 中文名直读 Language_StoryInfo_cn.txt，不切 LanguageCfg 语言——避免 Inspector 绘制时篡改正在运行游戏的语言。
    /// </summary>
    private void EnsureStoryTestOptions()
    {
        if (storyTestOptions != null) return;
        var allData = StoryInfoCfg.GetAllArrayData();
        Dictionary<long, LanguageBean> dicLanguage = LoadLanguageForCn("Language_StoryInfo_cn.txt");
        var listEntries = new List<KeyValuePair<long, GUIContent>>();
        for (int i = 0; i < allData.Length; i++)
        {
            var storyInfo = allData[i];
            if (storyInfo == null) continue;
            //先检查多语言行是否存在，避免 name_language 对缺失行刷 LogError
            string storyName;
            if (storyInfo.name != 0 && dicLanguage.TryGetValue(storyInfo.name, out LanguageBean languageBean) && !languageBean.content.IsNull())
            {
                storyName = languageBean.content;
            }
            else
            {
                storyName = "(未配置名字)";
            }
            listEntries.Add(new KeyValuePair<long, GUIContent>(storyInfo.id, new GUIContent($"{storyInfo.id}  {storyName}  [{storyInfo.GetTriggerType()}/{storyInfo.GetSceneType()}/{storyInfo.GetTriggerCondition()}]")));
        }
        //按 id 排序保证下拉顺序稳定
        listEntries.Sort((a, b) => a.Key.CompareTo(b.Key));
        storyTestOptions = new GUIContent[listEntries.Count];
        storyTestIds = new long[listEntries.Count];
        for (int i = 0; i < listEntries.Count; i++)
        {
            storyTestIds[i] = listEntries[i].Key;
            storyTestOptions[i] = listEntries[i].Value;
        }
    }

    /// <summary>
    /// 构建 NPC 下拉选项(id + 中文名，按id排序；withManualPlaceholder 时首项插入"(手动输入)"占位，选中不改动ID)。
    /// 中文名直读 Language_NpcInfo_cn.txt，不切 LanguageCfg 语言——避免 Inspector 绘制时篡改正在运行游戏的语言。
    /// </summary>
    /// <param name="withManualPlaceholder">首项是否插入"(手动输入)"占位(战斗测试的逐行下拉需要，对话测试不需要)</param>
    /// <param name="options">输出下拉选项</param>
    /// <param name="ids">输出选项对应的 NPC ID</param>
    private void BuildNpcOptions(bool withManualPlaceholder, out GUIContent[] options, out long[] ids)
    {
        var allData = NpcInfoCfg.GetAllArrayData();
        //加载中文多语言(NpcInfo.name → Language_NpcInfo_cn.txt)
        Dictionary<long, LanguageBean> dicLanguage = LoadLanguageForCn("Language_NpcInfo_cn.txt");
        var listEntries = new List<KeyValuePair<long, GUIContent>>();
        for (int i = 0; i < allData.Length; i++)
        {
            var npcInfo = allData[i];
            if (npcInfo == null) continue;
            //先检查多语言行是否存在，避免 name_language 对缺失行刷 LogError
            string npcName;
            if (npcInfo.name == 0)
            {
                //随机议员没有配置名字，用通用称谓展示
                npcName = npcInfo.GetNpcType() == NpcTypeEnum.CouncilorRandom ? "(随机议员)" : "(无名字)";
            }
            else if (dicLanguage.TryGetValue(npcInfo.name, out LanguageBean languageBean) && !languageBean.content.IsNull())
            {
                npcName = languageBean.content;
            }
            else
            {
                npcName = "(未配置名字)";
            }
            listEntries.Add(new KeyValuePair<long, GUIContent>(npcInfo.id, new GUIContent($"{npcInfo.id}  {npcName}")));
        }
        //按 id 排序保证下拉顺序稳定
        listEntries.Sort((a, b) => a.Key.CompareTo(b.Key));
        int offset = withManualPlaceholder ? 1 : 0;
        options = new GUIContent[listEntries.Count + offset];
        ids = new long[listEntries.Count + offset];
        if (withManualPlaceholder)
        {
            options[0] = new GUIContent("(手动输入)");
            ids[0] = 0;
        }
        for (int i = 0; i < listEntries.Count; i++)
        {
            ids[i + offset] = listEntries[i].Key;
            options[i + offset] = listEntries[i].Value;
        }
    }

    /// <summary>
    /// 绘制生物献祭升级测试配置(选存档→选目标生物→手动/真实成功率→进入献祭)
    /// </summary>
    private void DrawCreatureSacrificeTest()
    {
        showCreatureSacrificeTest = EditorGUILayout.Foldout(showCreatureSacrificeTest, "🔮 献祭升级测试", true);
        if (!showCreatureSacrificeTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        // 存档槽位选择 + 加载存档生物
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        sacrificeTestSaveSlot = EditorGUILayout.IntPopup(
            new GUIContent("存档槽位", "要读取数据的存档槽位(1~3，与游戏一致：UserData_1/2/3)"),
            sacrificeTestSaveSlot,
            new[] { new GUIContent("存档 1"), new GUIContent("存档 2"), new GUIContent("存档 3") },
            new[] { 1, 2, 3 });
        if (GUILayout.Button("📂 加载存档生物", GUILayout.Width(120)))
        {
            LoadSacrificeTestCreatures();
        }
        EditorGUILayout.EndHorizontal();

        // 目标生物选择
        if (sacrificeTestCreatureNames != null && sacrificeTestCreatureNames.Length > 0)
        {
            sacrificeTestSelectIndex = EditorGUILayout.Popup(
                new GUIContent("目标生物", "从该存档背包中选取要升级的目标生物"),
                sacrificeTestSelectIndex,
                sacrificeTestCreatureNames);
        }
        else
        {
            EditorGUILayout.HelpBox("请先点击「加载存档生物」读取该存档背包中的生物。", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // 成功率设置
        EditorGUILayout.BeginVertical("box");
        sacrificeTestUseManualRate = EditorGUILayout.Toggle(
            new GUIContent("手动成功率", "勾选则用手动指定成功率掷骰；不勾选则使用该存档真实数据按公式计算"),
            sacrificeTestUseManualRate);
        if (sacrificeTestUseManualRate)
        {
            sacrificeTestManualRate = EditorGUILayout.Slider(
                new GUIContent("成功率", "手动指定的献祭成功率(0~1)"),
                sacrificeTestManualRate, 0f, 1f);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始献祭升级测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            if (sacrificeTestCreatureUUIds == null || sacrificeTestCreatureUUIds.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先加载存档生物并选择目标生物。", "确定");
            }
            else
            {
                int index = Mathf.Clamp(sacrificeTestSelectIndex, 0, sacrificeTestCreatureUUIds.Count - 1);
                launcher.StartForCreatureSacrificeTest(
                    sacrificeTestSaveSlot,
                    sacrificeTestCreatureUUIds[index],
                    sacrificeTestUseManualRate,
                    sacrificeTestManualRate);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("读取所选存档的真实数据作为运行时数据，进入基地后直接对目标生物发起献祭。结算不会写回真实存档。", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 加载所选存档槽位的背包生物，填充目标生物下拉选项(仅 Editor，不修改运行时数据)
    /// </summary>
    private void LoadSacrificeTestCreatures()
    {
        UserDataService dataService = new UserDataService();
        dataService.ChangeSlot(sacrificeTestSaveSlot);
        UserDataBean userData = dataService.Load(false);
        sacrificeTestCreatureUUIds = new List<string>();
        var listBackpackCreature = userData?.GetUserBackpackCreatureData().listBackpackCreature;
        if (userData == null || listBackpackCreature == null || listBackpackCreature.Count == 0)
        {
            sacrificeTestCreatureNames = new GUIContent[0];
            sacrificeTestSelectIndex = 0;
            EditorUtility.DisplayDialog("提示", $"存档 {sacrificeTestSaveSlot} 不存在或没有背包生物数据。", "确定");
            return;
        }
        List<GUIContent> listNames = new List<GUIContent>();
        for (int i = 0; i < listBackpackCreature.Count; i++)
        {
            var creatureData = listBackpackCreature[i];
            sacrificeTestCreatureUUIds.Add(creatureData.creatureUUId);
            listNames.Add(new GUIContent($"[{i}] {creatureData.creatureName} (id:{creatureData.creatureId} Lv.{creatureData.level} 稀有度:{creatureData.rarity})"));
        }
        sacrificeTestCreatureNames = listNames.ToArray();
        sacrificeTestSelectIndex = Mathf.Clamp(sacrificeTestSelectIndex, 0, sacrificeTestCreatureNames.Length - 1);
    }

    /// <summary>
    /// 绘制魔物进阶(生物升阶容器)测试配置(选存档→选解锁VAT数量/加速等级→进入基地直接打开进阶UI)
    /// </summary>
    private void DrawCreatureVatTest()
    {
        showCreatureVatTest = EditorGUILayout.Foldout(showCreatureVatTest, "🧪 魔物进阶测试", true);
        if (!showCreatureVatTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        // 存档槽位选择
        EditorGUILayout.BeginVertical("box");
        creatureVatTestSaveSlot = EditorGUILayout.IntPopup(
            new GUIContent("存档槽位", "要读取数据的存档槽位(1~3，与游戏一致：UserData_1/2/3)"),
            creatureVatTestSaveSlot,
            new[] { new GUIContent("存档 1"), new GUIContent("存档 2"), new GUIContent("存档 3") },
            new[] { 1, 2, 3 });
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // 解锁项:VAT数量 / 加速等级 —— 均为自由选择具体解锁几级(滑条拉满即全解锁,默认拉满)
        EditorGUILayout.BeginVertical("box");
        creatureVatTestVatNum = EditorGUILayout.IntSlider(
            new GUIContent("解锁VAT数量", $"本次测试解锁的升阶容器数量({CREATURE_VAT_TEST_VAT_NUM_MIN}~{CREATURE_VAT_TEST_VAT_NUM_MAX})；拉满={CREATURE_VAT_TEST_VAT_NUM_MAX}即全解锁"),
            creatureVatTestVatNum, CREATURE_VAT_TEST_VAT_NUM_MIN, CREATURE_VAT_TEST_VAT_NUM_MAX);
        creatureVatTestProgressLevel = EditorGUILayout.IntSlider(
            new GUIContent("解锁加速等级", $"魔晶加速研究等级(0~{CREATURE_VAT_TEST_PROGRESS_LEVEL_MAX})；0=加速锁定(隐藏加速按钮)，等级=每次加速推进秒数，拉满={CREATURE_VAT_TEST_PROGRESS_LEVEL_MAX}即全解锁"),
            creatureVatTestProgressLevel, 0, CREATURE_VAT_TEST_PROGRESS_LEVEL_MAX);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始魔物进阶测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForCreatureVatTest(creatureVatTestSaveSlot, creatureVatTestVatNum, creatureVatTestProgressLevel);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("读取所选存档的真实数据作为运行时数据，进入基地后直接打开魔物进阶UI。全程只是模拟(测试模拟标记，不会写回真实存档)。", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// 绘制魔汁机(魔物回收)测试配置(选存档→选投入数量上限→进入基地直接打开魔汁机UI)
    /// </summary>
    private void DrawCreatureJuicerTest()
    {
        showCreatureJuicerTest = EditorGUILayout.Foldout(showCreatureJuicerTest, "🧃 魔汁机测试", true);
        if (!showCreatureJuicerTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        // 存档槽位选择
        EditorGUILayout.BeginVertical("box");
        juicerTestSaveSlot = EditorGUILayout.IntPopup(
            new GUIContent("存档槽位", "要读取数据的存档槽位(1~3，与游戏一致：UserData_1/2/3)"),
            juicerTestSaveSlot,
            new[] { new GUIContent("存档 1"), new GUIContent("存档 2"), new GUIContent("存档 3") },
            new[] { 1, 2, 3 });
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // 解锁项:投入魔物可选上限 —— 自由选择具体上限(基础5+JuicerNum研究等级,拉满即全解锁,默认拉满)
        EditorGUILayout.BeginVertical("box");
        juicerTestCreatureMax = EditorGUILayout.IntSlider(
            new GUIContent("投入魔物上限", $"本次测试的投入魔物可选上限({JUICER_TEST_CREATURE_NUM_MIN}~{JUICER_TEST_CREATURE_NUM_MAX})；基础{JUICER_TEST_CREATURE_NUM_MIN}+JuicerNum研究等级，拉满={JUICER_TEST_CREATURE_NUM_MAX}即全解锁"),
            juicerTestCreatureMax, JUICER_TEST_CREATURE_NUM_MIN, JUICER_TEST_CREATURE_NUM_MAX);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始魔汁机测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForCreatureJuicerTest(juicerTestSaveSlot, juicerTestCreatureMax);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("读取所选存档的真实数据作为运行时数据，进入基地后直接打开魔汁机UI。全程只是模拟(测试模拟标记，不会写回真实存档)。", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }

    public FightBean GetTestData()
    {
        // 单体测试模式：道路长度/道路数量/进攻生物数量/进攻间隔强制使用固定值
        bool isSingleUnit = fightTestMode == FightTestModeEnum.SingleUnit;
        int roadNum = isSingleUnit ? SINGLE_UNIT_ROAD_NUM : fightSceneRoadNum;
        int roadLength = isSingleUnit ? SINGLE_UNIT_ROAD_LENGTH : fightSceneRoadLength;
        int attackNum = isSingleUnit ? SINGLE_UNIT_ATTACK_NUM : fightSceneAttackNum;
        float attackDelay = isSingleUnit ? SINGLE_UNIT_ATTACK_DELAY : fightSceneAttackDelay;

        FightBeanForTest fightData = new FightBeanForTest();
        fightData.sceneRoadNum = roadNum;
        fightData.sceneRoadLength = roadLength;
        fightData.gameFightType = GameFightTypeEnum.Test;

        // 生成进攻数据
        fightData.fightAttackData = new FightAttackBean();
        for (int i = 0; i < attackNum; i++)
        {
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(attackDelay, enemyIds);
            fightData.fightAttackData.AddAttackQueue(fightAttackDetails);
        }
        fightData.fightAttackDataRemark = ClassUtil.DeepCopy(fightData.fightAttackData);

        // 所有的卡片数据(卡片生物ID列表为空时兜底2002，避免取模除零)
        fightData.dlDefenseCreatureData.Clear();
        long[] ids = fightCardIds.Count > 0 ? fightCardIds.ToArray() : new long[] { 2002 };
        for (int i = 0; i < testDataCardNum; i++)
        {
            int index = i % ids.Length;
            CreatureBean itemData = new CreatureBean(ids[index]);
            itemData.AddSkinForBase();
            // 史莱姆加一个身体皮肤
            if (itemData.creatureId > 3000 && itemData.creatureId < 4000)
            {
                itemData.AddSkin(3040001);
            }
            itemData.order = i;
            fightData.dlDefenseCreatureData.Add(itemData.creatureUUId, itemData);

            // 攻击模式测试
            if (attackModeDefenseTestId != 0)
            {
                itemData.creatureInfo.attack_mode = attackModeDefenseTestId;
            }

            // BUFF 测试
            if (!buffSelfDefenseTestId.IsNull())
            {
                itemData.creatureInfo.creature_buff = buffSelfDefenseTestId;
                itemData.creatureInfo.GetCreatureBuffs();
            }
        }

        FightCreatureBean fightDefCoreData = CreatureHandler.Instance.GetFightCreatureData(fightDefenseCoreId, CreatureFightTypeEnum.FightDefenseCore);
        fightDefCoreData.creatureData.AddSkinForBase();
        fightData.fightDefenseCoreData = fightDefCoreData;
        //传递测试魔王蓝量(由 GameFightLogicTest 在防守核心创建后统一应用)
        fightData.testDemonLordMP = fightDemonLordMP;
        fightData.InitData();
        fightData.fightSceneId = fightSceneId;

        // 初始化 BUFF
        if (!buffTestId.IsNull())
        {
            AttackModeInfoCfg.InitTestData(buffTestId);
        }
        // 设置深渊馈赠：解析"族根+等级"→具体馈赠行id存入战斗数据，由 GameFightLogicTest 在防守核心创建后统一添加
        // （不可在此直接调 BuffHandler.AddAbyssalBlessing——战斗场景尚未启动，防守核心未创建，必空引用）
        fightData.testAbyssalBlessingIds.Clear();
        for (int i = 0; i < abyssalBlessingFightTestList.Count; i++)
        {
            var item = abyssalBlessingFightTestList[i];
            if (item == null) continue;
            long targetId = item.familyRootId;
            //升级链族按目标等级取对应行；可重复馈赠(level=0)直接用族根行
            if (item.level > 0)
            {
                var levelInfo = AbyssalBlessingInfoCfg.GetItemDataByFamilyLevel(item.familyRootId, item.level);
                if (levelInfo == null)
                {
                    LogUtil.LogWarning($"深渊馈赠测试：族根 {item.familyRootId} 不存在等级 {item.level} 的配置，已跳过");
                    continue;
                }
                targetId = levelInfo.id;
            }
            fightData.testAbyssalBlessingIds.Add(targetId);
        }
        return fightData;
    }
}
