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
    public int fightCardId= 1;

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
            fightCreature.creatureData.AddAllSkin();
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
        fightSceneId =  EditorGUILayout.IntField(fightSceneId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("测试数据-卡片生物ID");
        fightCardId = EditorGUILayout.IntField(fightCardId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 基地测试UI
    /// </summary>
    public void UIForFightBase()
    {
        if (GUILayout.Button("开始") && Application.isPlaying)
        {
            CreatureBean creatureData = new CreatureBean(fightCardId);
            creatureData.AddAllSkin();
            launcher.StartForBaseTest(creatureData);
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("生物ID");
        fightCardId = EditorGUILayout.IntField(fightCardId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    ///   测试数据
    /// </summary>
    /// <returns></returns>
    public FightBean GetTestData()
    {
        FightBean fightData = new FightBean();
        fightData.currentMagic = testDataCurrentMagic;

        //生成进攻数据
        fightData.fightAttackData = new FightAttackBean();
        for (int i = 1; i < 4; i++)
        {
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(i * 5, 2);
            fightData.fightAttackData.AddAttackQueue(fightAttackDetails);
        }

        //所有的卡皮数据
        List<CreatureBean> listCreatureData = new List<CreatureBean>();
        for (int i = 0; i < testDataCardNum; i++)
        {
            CreatureBean itemData = new CreatureBean(fightCardId);
            itemData.AddAllSkin();
            //itemData.creatureData.AddSkin(1000001);
            //itemData.creatureData.AddSkin(1010010);
            listCreatureData.Add(itemData); ;
        }
        fightData.listDefCreatureData = listCreatureData;


        FightCreatureBean fightDefCoreData = new FightCreatureBean(1);
        fightDefCoreData.creatureData.AddSkin(1000001);
        fightDefCoreData.creatureData.AddSkin(1010011);
        fightDefCoreData.creatureData.AddSkin(1020030);
        fightData.fightDefCoreData = fightDefCoreData;

        fightData.InitData();
        fightData.fightSceneId = fightSceneId;

        return fightData;
    }
}

