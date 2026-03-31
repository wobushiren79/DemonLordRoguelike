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

        // 运行按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始战斗测试", GUILayout.Height(30)) && Application.isPlaying)
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
            fightSceneId = EditorGUILayout.IntField(new GUIContent("测试场景 ID", "战斗场景的 ID"), fightSceneId);
            fightCardId = EditorGUILayout.TextField(new GUIContent("卡片生物 ID", "防守方卡片的生物 ID，多个用逗号分隔"), fightCardId);
            fightSceneRoadNum = EditorGUILayout.IntField(new GUIContent("道路数量", "战斗场景的道路数量"), fightSceneRoadNum);
            fightSceneRoadLength = EditorGUILayout.IntField(new GUIContent("道路长度", "每条道路的长度"), fightSceneRoadLength);
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
            fightSceneAttackNum = EditorGUILayout.IntField(new GUIContent("进攻生物数量", "每波进攻的生物数量"), fightSceneAttackNum);
            fightSceneAttackDelay = EditorGUILayout.FloatField(new GUIContent("进攻间隔", "进攻波次之间的延迟时间"), fightSceneAttackDelay);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("进攻生物 IDs", EditorStyles.boldLabel);
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

    private void DrawCardTest()
    {
        showCardTest = EditorGUILayout.Foldout(showCardTest, "🃏 卡片测试", true);
        if (!showCardTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 显示卡片", GUILayout.Height(30)) && Application.isPlaying)
        {
            FightCreatureBean fightCreature = CreatureHandler.Instance.GetFightCreatureData(creatureId, CreatureFightTypeEnum.FightDefense);
            fightCreature.creatureData.AddSkinForBase();
            launcher.StartForCardTest(fightCreature);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        creatureId = EditorGUILayout.IntField(new GUIContent("生物 ID", "要显示的卡片生物 ID"), creatureId);
        creatureModelId = EditorGUILayout.IntField(new GUIContent("模型 ID", "生物模型 ID，0 表示默认"), creatureModelId);
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
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        doomCouncilBillId = EditorGUILayout.LongField(new GUIContent("议会议案 ID", "终焉议会的议案 ID"), doomCouncilBillId);
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
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

    public FightBean GetTestData()
    {
        FightBeanForTest fightData = new FightBeanForTest();
        fightData.sceneRoadNum = fightSceneRoadNum;
        fightData.sceneRoadLength = fightSceneRoadLength;
        fightData.gameFightType = GameFightTypeEnum.Test;

        // 生成进攻数据
        fightData.fightAttackData = new FightAttackBean();
        for (int i = 0; i < fightSceneAttackNum; i++)
        {
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(fightSceneAttackDelay, enemyIds);
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
