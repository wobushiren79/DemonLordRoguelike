using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldHandler : BaseHandler<WorldHandler, WorldManager>
{
    //��ǰս������
    public GameObject currentFightScene;
    //���س���
    public GameObject currentBaseScene;

    public void EnterGameForBaseScene(UserDataBean userData,bool isInitScene)
    {
        Action actionForStart = () =>
        {
            //��ͷ��ʼ��
            CameraHandler.Instance.InitData();
            //����������ʼ��
            VolumeHandler.Instance.InitData(GameSceneTypeEnum.Base);
            //���û��س����ӽ�
            CameraHandler.Instance.InitBaseSceneControlCamera(() =>
            {
                //�ر�LoadingUI
                var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
            }, userData.selfCreature);
        };
        if (isInitScene)
        {
            //������������
            ClearWorldData(() =>
            {                   
                //���ػ��س���
                LoadBaseScene((targetObj) =>
                {
                    actionForStart?.Invoke();
                });
            });
        }
        else
        {
            actionForStart?.Invoke();
        }
    }

    public void EnterMainForBaseScene()
    {
        ClearWorldData(() =>
        {
            //�򿪼���UI
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
            //��ͷ��ʼ��
            CameraHandler.Instance.InitData();
            //����������ʼ��
            VolumeHandler.Instance.InitData(GameSceneTypeEnum.Base);
            //���ػ��س���
            LoadBaseScene((targetObj) =>
            {
                //�ر�LoadingUI �򿪿�ʼUI
                UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
            });
        });
    }

    /// <summary>
    /// ���ػ��س���
    /// </summary>
    /// <param name="actionForComplete"></param>
    public void LoadBaseScene(Action<GameObject> actionForComplete)
    {
        UnLoadBaseScene();
        manager.GetBaseScene((targetScene) =>
        {
            currentBaseScene = Instantiate(targetScene);
            currentBaseScene.SetActive(true);
            currentBaseScene.transform.position = Vector3.zero;
            currentBaseScene.transform.eulerAngles = Vector3.zero;
            actionForComplete?.Invoke(currentBaseScene);
        });
    }

    /// <summary>
    /// ����ս������
    /// </summary>
    public void LoadFightScene(int fightSceneId, Action<GameObject> actionForComplete)
    {
        UnLoadFightScene();
        manager.GetFightScene(fightSceneId, (targetScene) =>
        {
            currentFightScene = Instantiate(targetScene);
            currentFightScene.SetActive(true);
            currentFightScene.transform.position = Vector3.zero;
            currentFightScene.transform.eulerAngles = Vector3.zero;
            actionForComplete?.Invoke(currentFightScene);
        });
    }

    /// <summary>
    /// ж��ս������
    /// </summary>
    public void UnLoadFightScene()
    {
        if (currentFightScene != null)
        {
            DestroyImmediate(currentFightScene);
        }
    }

    /// <summary>
    /// ж�ػ��س���
    /// </summary>
    public void UnLoadBaseScene()
    {
        if (currentBaseScene != null)
        {
            DestroyImmediate(currentBaseScene);
        }
    }

    /// <summary>
    /// ����������������
    /// </summary>
    public async void ClearWorldData(Action actionForComplete,bool isShowLoading = true)
    {      
        //�򿪼���UI
        if(isShowLoading)
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
        //�ر����п���
        GameControlHandler.Instance.manager.EnableAllControl(false);

        await new WaitNextFrame();
        UnLoadBaseScene();
        await new WaitNextFrame();
        //logic����
        BaseGameLogic gameLogic =  GameHandler.Instance.manager.GetGameLogic<BaseGameLogic>();
        if (gameLogic != null)
        {
            gameLogic.ClearGame();
        }
        await new WaitNextFrame();
        //��������
        EffectHandler.Instance.manager.Clear();
        await new WaitNextFrame();
        //������
        System.GC.Collect();
        await new WaitNextFrame();
        actionForComplete?.Invoke();
    }
}
