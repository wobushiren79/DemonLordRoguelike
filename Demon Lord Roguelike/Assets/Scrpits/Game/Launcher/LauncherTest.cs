using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("��������-��ǰħ��")]
    public int testDataCurrentMagic = 1000;
    [Header("��������-��Ƭ����")]
    public int testDataCardNum = 20;
    public override void Launch()
    {
        base.Launch();
        //�򿪼���UI
        UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
        //����������ʼ��
        VolumeHandler.Instance.InitData();
        //��ͷ��ʼ��
        CameraHandler.Instance.InitData();
        //��ʼս��
        FightBean fightData = GetTestData();
        //��������
        GameHandler.Instance.StartGameFight(fightData);
    }

    /// <summary>
    ///   ��������
    /// </summary>
    /// <returns></returns>
    public FightBean GetTestData()
    {
        FightBean fightData = new FightBean();
        fightData.currentMagic = testDataCurrentMagic;
        fightData.fightAttCreateData = new FightAttCreateBean();

        //��������1
        FightAttCreateDetailsBean attCreateData1 = new FightAttCreateDetailsBean();
        attCreateData1.stage = 1;
        attCreateData1.timeDuration = 180;
        attCreateData1.createNum = 1;
        attCreateData1.createDelay = 0.1f;
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

        //���еĿ�Ƥ����
        List<FightCreatureBean> listCreatureData = new List<FightCreatureBean>();
        for (int i = 0; i < testDataCardNum; i++)
        {
            FightCreatureBean itemData = new FightCreatureBean(1);
            listCreatureData.Add(itemData); ;
        }
        fightData.listDefCreatureData = listCreatureData;
        fightData.InitDataForAttCreateStage(1);
        return fightData;
    }
}
