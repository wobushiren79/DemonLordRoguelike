using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static ExcelUtil;
[CustomEditor(typeof(LauncherTest))]
public class GameTestEditor : Editor
{

    public int testDataCardNum = 20;
    public int fightSceneId = 1;
    public string fightCardId = "2002";
    public int fightSceneRoadNum = 1;    //道路数量
    public int fightSceneRoadLength = 10;    //道路长度
    public int fightSceneAttackNum = 2;//进攻者数量
    public float fightSceneAttackDelay = 1;//进攻者间隔

    public int creatureId = 1 ;
    public int creatureModelId = 0;

    public int attackModeAttackTestId = 0;//攻击模式测试ID
    public int attackModeDefenseTestId = 0;//攻击模式测试ID

    public string buffSelfAttackTestId = "";//buff测试ID
    public string buffSelfDefenseTestId = "";//buff测试ID

    public string buffTestId = "1000100001";//攻击模式测试ID
    public List<long> enemyIds = new List<long>() { 1010010001 };
    
    LauncherTest launcher;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        launcher = (LauncherTest)target;
        if (launcher.testSceneType == TestSceneTypeEnum.FightSceneTest)
        {
            UIForFightSceneTest();
        }
        else if (launcher.testSceneType == TestSceneTypeEnum.CardTest)
        {
            UIForCardTest();
        }
        else if (launcher.testSceneType == TestSceneTypeEnum.Base)
        {
            UIForFightBase();
        }
    }

    /// <summary>
    /// 卡片测试UI
    /// </summary>
    public void UIForCardTest()
    {
        if (GUILayout.Button("显示卡片") && Application.isPlaying)
        {
            FightCreatureBean fightCreature = new FightCreatureBean(creatureId);
            fightCreature.creatureData.AddSkinForBase();
            launcher.StartForCardTest(fightCreature);
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("生物ID");
        creatureId = EditorGUILayout.IntField(creatureId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("生物modeId");
        creatureModelId = EditorGUILayout.IntField(creatureModelId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 战斗场景测试UI
    /// </summary>
    public void UIForFightSceneTest()
    {
        if (GUILayout.Button("开始") && Application.isPlaying)
        {
            FightBean fightData = GetTestData();
            launcher.StartForFightSceneTest(fightData);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-卡片数量");
        testDataCardNum = EditorGUILayout.IntField(testDataCardNum);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-测试场景");
        fightSceneId = EditorGUILayout.IntField(fightSceneId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-卡片生物ID");
        fightCardId = EditorGUILayout.TextField(fightCardId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-测试场景道路数量");
        fightSceneRoadNum = EditorGUILayout.IntField(fightSceneRoadNum);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-测试场景道路长度");
        fightSceneRoadLength = EditorGUILayout.IntField(fightSceneRoadLength);
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.LabelField("测试数据-进攻生物Ids");
        for (int i = 0; i < enemyIds.Count; i++)
        {
            enemyIds[i] = EditorGUILayout.LongField(enemyIds[i]);
        }
        EditorGUILayout.BeginHorizontal();
        // 添加/删除按钮
        if (GUILayout.Button("Add"))
        {
            enemyIds.Add(0);
        }
        if (GUILayout.Button("Remove") && enemyIds.Count > 0)
        {
            enemyIds.RemoveAt(enemyIds.Count - 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-进攻生物数量");
        fightSceneAttackNum = EditorGUILayout.IntField(fightSceneAttackNum);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-进攻生物间隔");
        fightSceneAttackDelay = EditorGUILayout.FloatField(fightSceneAttackDelay);
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-进攻生物攻击模块");
        attackModeAttackTestId = EditorGUILayout.IntField(attackModeAttackTestId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-防守生物攻击模块");
        attackModeDefenseTestId = EditorGUILayout.IntField(attackModeDefenseTestId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-进攻生物携带BUFF");
        buffSelfAttackTestId = EditorGUILayout.TextField(buffSelfAttackTestId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-防守生物携带BUFF");
        buffSelfDefenseTestId = EditorGUILayout.TextField(buffSelfDefenseTestId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-攻击BUFF");
        buffTestId = EditorGUILayout.TextField(buffTestId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 基地测试UI
    /// </summary>
    public void UIForFightBase()
    {
        if (GUILayout.Button("开始") && Application.isPlaying)
        {
            var ids = fightCardId.SplitForArrayLong(',');
            if (ids.Length > 0)
            {
                CreatureBean creatureData = new CreatureBean(creatureId);
                creatureData.AddSkinForBase();
                launcher.StartForBaseTest(creatureData);
            }
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("魔王生物ID");
        creatureId = EditorGUILayout.IntField(creatureId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("手下生物ID");
        fightCardId = EditorGUILayout.TextField(fightCardId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    ///   测试数据
    /// </summary>
    /// <returns></returns>
    public FightBean GetTestData()
    {
        FightBeanForTest fightData = new FightBeanForTest();
        fightData.sceneRoadNum = fightSceneRoadNum;
        fightData.sceneRoadLength = fightSceneRoadLength;
        fightData.gameFightType = GameFightTypeEnum.Test;

        //生成进攻数据
        fightData.fightAttackData = new FightAttackBean();
        for (int i = 0; i < fightSceneAttackNum; i++)
        {
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(fightSceneAttackDelay, enemyIds);
            fightData.fightAttackData.AddAttackQueue(fightAttackDetails);
        }
        fightData.fightAttackDataRemark = ClassUtil.DeepCopy(fightData.fightAttackData);

        //所有的卡片数据
        fightData.dlDefenseCreatureData.Clear();
        var ids = fightCardId.SplitForArrayLong(',');
        for (int i = 0; i < testDataCardNum; i++)
        {
            int index = i % ids.Length;
            CreatureBean itemData = new CreatureBean(ids[index]);
            itemData.AddSkinForBase();
            //史莱姆加一个身体皮肤
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

            // BUFF测试
            if (!buffSelfDefenseTestId.IsNull())
            {
                itemData.creatureInfo.create_buff = buffSelfDefenseTestId;
                itemData.creatureInfo.GetCreatureBuff();
            }
        }

        FightCreatureBean fightDefCoreData = new FightCreatureBean(2001);
        fightDefCoreData.creatureData.AddSkinForBase();
        fightDefCoreData.creatureData.creatureInfo.creature_type = (int)CreatureTypeEnum.FightDefenseCore;
        fightData.fightDefenseCoreData = fightDefCoreData;

        fightData.InitData();
        fightData.fightSceneId = fightSceneId;

        //初始化BUFF
        if (!buffTestId.IsNull())
        {
            AttackModeInfoCfg.InitTestData(buffTestId);
        }
        return fightData;
    }
}

