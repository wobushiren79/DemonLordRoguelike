using NUnit.Framework.Interfaces;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("��������")]
    public TestSceneTypeEnum testSceneType = TestSceneTypeEnum.FightSceneTest;

    /// <summary>
    /// ��ʼս����������
    /// </summary>
    /// <param name="fightData"></param>
    public void StartForFightSceneTest(FightBean fightData)
    {
        //�򿪼���UI
        UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
        //����������ʼ��
        VolumeHandler.Instance.InitData();
        //��ͷ��ʼ��
        CameraHandler.Instance.InitData();
        //��������
        GameHandler.Instance.StartGameFight(fightData);
    }

    /// <summary>
    /// ��ʼ��Ƭ����
    /// </summary>
    /// <param name="fightCreature"></param>
    public void StartForCardTest(FightCreatureBean fightCreature)
    {
        var ui = UIHandler.Instance.OpenUIAndCloseOther<UITestCard>();
        ui.SetData(fightCreature);
    }
}
