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

    /// <summary>
    /// ������Ϸ����������ѡ��
    /// </summary>
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
    /// ������Ϸ�� ���س���
    /// </summary>
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



    /// <summary>
    /// ����ս������
    /// </summary>
    public void EnterGameForFightScene(FightBean fightData)
    {
        ClearWorldData(() =>
        {
            //�򿪼���UI
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
            //��ͷ��ʼ��
            CameraHandler.Instance.InitData();
            //����������ʼ��
            VolumeHandler.Instance.InitData(GameSceneTypeEnum.Fight);
            //��ʼս��
            GameHandler.Instance.StartGameFight(fightData);
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

        FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightSceneId);
        if (fightSceneData == null)
        {
            LogUtil.LogError($"��ѯFightSceneս������ʧ��  û���ҵ�idΪ{fightSceneId}��ս������");
            return;
        }

        //��ȡ��պ�
        manager.GetSkybox(fightSceneData.skybox_mat, (skyboxMat) =>
        {
            //������պ�
            RenderSettings.skybox = skyboxMat;
            //��ȡ����
            string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
            manager.GetFightScene(dataPath, (targetScene) =>
            {
                currentFightScene = Instantiate(targetScene);
                currentFightScene.SetActive(true);
                currentFightScene.transform.position = Vector3.zero;
                currentFightScene.transform.eulerAngles = Vector3.zero;
                actionForComplete?.Invoke(currentFightScene);
            });
        });
    }

    /// <summary>
    /// ж��ս������
    /// </summary>
    public void UnLoadFightScene()
    {
        //ɾ�����е�ս������
        if (currentFightScene != null)
        {
            DestroyImmediate(currentFightScene);
        }
        //�Ƴ���պ�
        manager.RemoveSkybox();
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
        //�Ƴ���պ�
        manager.RemoveSkybox();
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
