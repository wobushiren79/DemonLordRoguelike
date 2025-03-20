using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ExcelUtil;
[CustomEditor(typeof(LauncherTest))]
public class GameTestEditor : Editor
{
    public int testDataCurrentMagic = 1000;
    public int testDataCardNum = 20;
    public int fightSceneId = 1;
    public string fightCardId = "2001";
    public int fightSceneRoadNum = 6;    //道路数量
    public int fightSceneRoadLength = 10;    //道路长度

    public int creatureId = 0;
    public int creatureModelId = 0;
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
            fightCreature.creatureData.AddTestSkin();
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
        EditorGUILayout.LabelField("测试数据-当前魔力");
        testDataCurrentMagic = EditorGUILayout.IntField(testDataCurrentMagic);
        EditorGUILayout.EndHorizontal();

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
                CreatureBean creatureData = new CreatureBean(ids[0]);
                creatureData.AddTestSkin();
                launcher.StartForBaseTest(creatureData);
            }
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("生物ID");
        fightCardId = EditorGUILayout.TextField(fightCardId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    ///   测试数据
    /// </summary>
    /// <returns></returns>
    public FightBean GetTestData()
    {
        FightBean fightData = new FightBean();
        fightData.sceneRoadNum = fightSceneRoadNum;
        fightData.sceneRoadLength = fightSceneRoadLength;
        fightData.gameFightType = GameFightTypeEnum.Test;
        fightData.currentMagic = testDataCurrentMagic;

        //生成进攻数据
        fightData.fightAttackData = new FightAttackBean();
        for (int i = 0; i < 100; i++)
        {
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(3, 1001);
            fightData.fightAttackData.AddAttackQueue(fightAttackDetails);
        }

        //所有的卡皮数据
        fightData.dlDefenseCreatureData.Clear();
        var ids = fightCardId.SplitForArrayLong(',');
        for (int i = 0; i < testDataCardNum; i++)
        {
            int index = i % ids.Length;
            CreatureBean itemData = new CreatureBean(ids[index]);
            itemData.AddTestSkin();
            itemData.order = i;
            fightData.dlDefenseCreatureData.Add(itemData.creatureId, itemData);
        };

        FightCreatureBean fightDefCoreData = new FightCreatureBean(2001);
        fightDefCoreData.creatureData.AddSkin(2000001);
        fightDefCoreData.creatureData.AddSkin(2010011);
        fightDefCoreData.creatureData.AddSkin(2020030);
        fightData.fightDefenseCoreData = fightDefCoreData;

        fightData.InitData();
        fightData.fightSceneId = fightSceneId;

        return fightData;
    }
}

