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
        WorldHandler.Instance.ClearWorldData(() =>
        {
            //�򿪼���UI
            UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
            //��ͷ��ʼ��
            CameraHandler.Instance.InitData();
            //����������ʼ��
            VolumeHandler.Instance.InitData();
            //��������
            GameHandler.Instance.StartGameFight(fightData);
        });
    }

    /// <summary>
    /// ��ʼ��Ƭ����
    /// </summary>
    /// <param name="fightCreature"></param>
    public void StartForCardTest(FightCreatureBean fightCreature)
    {
        WorldHandler.Instance.ClearWorldData(() =>
        {
            var ui = UIHandler.Instance.OpenUIAndCloseOther<UITestCard>();
            ui.SetData(fightCreature);
        });
    }

    /// <summary>
    /// ���ز���
    /// </summary>
    public void StartForBaseTest(FightCreatureBean fightCreature)
    {
        WorldHandler.Instance.ClearWorldData(() =>
        {
            //�򿪼���UI
            UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
            //��ͷ��ʼ��
            CameraHandler.Instance.InitData();
            //����������ʼ��
            VolumeHandler.Instance.InitData();
            //���û��س����ӽ�
            CameraHandler.Instance.SetBaseSceneCamera(() =>
            {
                //���ػ��س���
                WorldHandler.Instance.LoadBaseScene((targetObj) =>
                {
                    //��������
                    GameControlHandler.Instance.SetBaseControl();
                    //�ر�LoadingUI
                    var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
                });
            }, fightCreature);
        });
    }
}
