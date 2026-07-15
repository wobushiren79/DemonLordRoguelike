using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        DrawHeader();

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
            case TestSceneTypeEnum.NpcCreate:
                DrawNpcCreateTest();
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

    private void DrawHeader()
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
            EditorGUILayout.BeginHorizontal();
            fightCardId = EditorGUILayout.TextField(new GUIContent("卡片生物 ID", "防守方卡片的生物 ID，多个用逗号分隔"), fightCardId);
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
            EditorGUILayout.LabelField("进攻生物 IDs （NPCID）", EditorStyles.boldLabel);
            for (int i = 0; i < enemyIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"敌人 {i + 1}", GUILayout.Width(60));
                enemyIds[i] = EditorGUILayout.LongField(enemyIds[i]);
                if (GUILayout.Button("🗑️", GUILayout.Width(30)))
                {
                    enemyIds.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ 添加敌人"))
            {
                enemyIds.Add(0);
            }
            if (enemyIds.Count > 0 && GUILayout.Button("🗑️ 移除最后一个"))
            {
                enemyIds.RemoveAt(enemyIds.Count - 1);
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
            abyssalBlessingIds = EditorGUILayout.TextField(new GUIContent("深渊馈赠 IDs", "深渊馈赠 ID 列表，多个用逗号分隔"), abyssalBlessingIds);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
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
            var ids = fightCardId.SplitForArrayLong(',');
            if (ids.Length > 0)
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
        fightCardId = EditorGUILayout.TextField(new GUIContent("手下生物 ID", "手下角色的生物 ID，多个用逗号分隔"), fightCardId);
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

    private void DrawNpcCreateTest()
    {
        showNpcCreateTest = EditorGUILayout.Foldout(showNpcCreateTest, "🧙 NPC 创建测试", true);
        if (!showNpcCreateTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始 NPC 创建", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartNpcCreate();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(3);

        GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
        if (GUILayout.Button("▶️ 开始 NPC 创建（GUI）", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartNpcCreateGUI();
        }
        GUI.backgroundColor = Color.white;

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
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

        // 所有的卡片数据
        fightData.dlDefenseCreatureData.Clear();
        var ids = fightCardId.SplitForArrayLong(',');
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

        FightCreatureBean fightDefCoreData = CreatureHandler.Instance.GetFightCreatureData(2001, CreatureFightTypeEnum.FightDefenseCore);
        fightDefCoreData.creatureData.AddSkinForBase();
        fightData.fightDefenseCoreData = fightDefCoreData;
        fightData.InitData();
        fightData.fightSceneId = fightSceneId;

        // 初始化 BUFF
        if (!buffTestId.IsNull())
        {
            AttackModeInfoCfg.InitTestData(buffTestId);
        }
        // 设置深渊馈赠
        if (!abyssalBlessingIds.IsNull())
        {
            long[] arrayAbyssalBlessingIds = abyssalBlessingIds.SplitForArrayLong(',');
            for (int i = 0; i < arrayAbyssalBlessingIds.Length; i++)
            {
                var itemID = arrayAbyssalBlessingIds[i];
                AbyssalBlessingInfoBean abyssalBlessingInfo = AbyssalBlessingInfoCfg.GetItemData(itemID);
                AbyssalBlessingEntityBean abyssalBlessingEntityData = new AbyssalBlessingEntityBean(abyssalBlessingInfo);
                BuffHandler.Instance.AddAbyssalBlessing(abyssalBlessingEntityData);
            }
        }
        return fightData;
    }
}
