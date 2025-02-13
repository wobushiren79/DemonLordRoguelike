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
    /// ��Ƭ����UI
    /// </summary>
    public void UIForCardTest()
    {
        if (GUILayout.Button("��ʾ��Ƭ") && Application.isPlaying)
        {
            FightCreatureBean fightCreature = new FightCreatureBean(creatureId);
            fightCreature.creatureData.AddAllSkin();
            launcher.StartForCardTest(fightCreature);
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("����ID");
        creatureId = EditorGUILayout.IntField(creatureId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("����modeId");
        creatureModelId = EditorGUILayout.IntField(creatureModelId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// ս����������UI
    /// </summary>
    public void UIForFightSceneTest()
    {
        if (GUILayout.Button("��ʼ") && Application.isPlaying)
        {
            FightBean fightData = GetTestData();
            launcher.StartForFightSceneTest(fightData);
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("��������-��ǰħ��");
        testDataCurrentMagic = EditorGUILayout.IntField(testDataCurrentMagic);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("��������-��Ƭ����");
        testDataCardNum = EditorGUILayout.IntField(testDataCardNum);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("��������-���Գ���");
        fightSceneId =  EditorGUILayout.IntField(fightSceneId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("��������-��Ƭ����ID");
        fightCardId = EditorGUILayout.IntField(fightCardId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// ���ز���UI
    /// </summary>
    public void UIForFightBase()
    {
        if (GUILayout.Button("��ʼ") && Application.isPlaying)
        {
            CreatureBean creatureData = new CreatureBean(fightCardId);
            creatureData.AddAllSkin();
            launcher.StartForBaseTest(creatureData);
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("����ID");
        fightCardId = EditorGUILayout.IntField(fightCardId);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    ///   ��������
    /// </summary>
    /// <returns></returns>
    public FightBean GetTestData()
    {
        FightBean fightData = new FightBean();
        fightData.currentMagic = testDataCurrentMagic;

        //���ɽ�������
        fightData.fightAttackData = new FightAttackBean();
        for (int i = 1; i < 4; i++)
        {
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(i * 5, 2);
            fightData.fightAttackData.AddAttackQueue(fightAttackDetails);
        }

        //���еĿ�Ƥ����
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

