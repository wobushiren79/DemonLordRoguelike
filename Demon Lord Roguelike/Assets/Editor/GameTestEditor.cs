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
        else
        {

        }
    }

    /// <summary>
    /// 卡片测试UI
    /// </summary>
    public void UIForCardTest()
    {
        if (GUILayout.Button("显示卡片"))
        {
            FightCreatureBean fightCreature = new FightCreatureBean(creatureId);
            fightCreature.AddAllSkin();
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
        if (GUILayout.Button("开始"))
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
    }

    /// <summary>
    ///   测试数据
    /// </summary>
    /// <returns></returns>
    public FightBean GetTestData()
    {
        FightBean fightData = new FightBean();
        fightData.currentMagic = testDataCurrentMagic;
        fightData.fightAttCreateData = new FightAttCreateBean();

        //进攻数据1
        FightAttCreateDetailsBean attCreateData1 = new FightAttCreateDetailsBean();
        attCreateData1.stage = 1;
        attCreateData1.timeDuration = 180;
        attCreateData1.createNum = 1;
        attCreateData1.createDelay = 0.5f;
        attCreateData1.timePointForCreatures = new List<FightAttCreateDetailsTimePointBean>()
        {
            new FightAttCreateDetailsTimePointBean(0f, 0.5f, new List<int>() { 2 }),
            new FightAttCreateDetailsTimePointBean(0.5f, 1f, new List<int>() { 2 })
        };
        attCreateData1.creatureEndIds = new Dictionary<int, int>()
        {
            {1, 10},
        };
        FightAttCreateDetailsBean attCreateData2 = new FightAttCreateDetailsBean();
        attCreateData2.stage = 2;
        attCreateData2.timeDuration = 60;
        attCreateData2.createNum = 2;
        attCreateData2.createDelay = 2;
        attCreateData2.timePointForCreatures = new List<FightAttCreateDetailsTimePointBean>()
        {
            new FightAttCreateDetailsTimePointBean(0, 0.5f, new List<int>() { 2 }),
            new FightAttCreateDetailsTimePointBean(0.5f, 1f, new List<int>() { 2 })
        };
        attCreateData2.creatureEndIds = new Dictionary<int, int>()
        {
            {1, 10},
        };
        fightData.fightAttCreateData.dicDetailsData = new Dictionary<int, FightAttCreateDetailsBean>()
        {
            {1,attCreateData1},
            {2,attCreateData2},
        };

        //所有的卡皮数据
        List<FightCreatureBean> listCreatureData = new List<FightCreatureBean>();
        for (int i = 0; i < testDataCardNum; i++)
        {
            FightCreatureBean itemData = new FightCreatureBean(999998);
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

        fightData.InitDataForAttCreateStage(1);
        fightData.fightSceneId = fightSceneId;

        return fightData;
    }
}

