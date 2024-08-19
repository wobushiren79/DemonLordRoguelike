using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightLogic : BaseGameLogic
{
    //ս������
    public FightBean fightData;

    public GameObject selectCreature;    //ѡ�������
    public UIViewCreatureCardItem selectCreatureCard;//ѡ�����￨Ƭ
    public Vector3Int selectCreaturePutPost;    //ѡ�������ķ���λ��

    /// <summary>
    /// ׼����Ϸ
    /// </summary>
    public override void PreGame()
    {
        base.PreGame();
        //����ս�������ӽ�
        CameraHandler.Instance.SetFightSceneCamera(() =>
        {
            //����ս������
            WorldHandler.Instance.LoadFightScene(fightData.fightSceneId,async (targetObj) =>
            {
                //�ӳ�0.1�� ��ֹһЩ��ͷ��1��2֡���
                await new WaitForSeconds(0.1f);
                //���غ��ģ�ħ����ʵ��
                CreatureHandler.Instance.CreateDefCoreCreature((defCoreCreatureEntity) =>
                {
                    //����ħ������
                    fightData.fightDefCoreCreature = defCoreCreatureEntity;
                    //����ս������
                    GameControlHandler.Instance.SetFightControl();
                    //�ر�LoadingUI
                    var uiFightMain = UIHandler.Instance.OpenUIAndCloseOther<UIFightMain>();
                    uiFightMain.InitData();
                    //��ʼ��Ϸ
                    StartGame();
                });
            });
        });
    }

    /// <summary>
    /// ע���¼�
    /// </summary>
    public override void PreGameForRegisterEvent()
    {

    }

    /// <summary>
    /// ����
    /// </summary>
    public override void UpdateGame()
    {
        base.UpdateGame();
        fightData.gameTime = fightData.gameTime + Time.deltaTime * fightData.gameSpeed;
        UpdateGameForSelectCreature();
        UpdateGameForAttCreate();
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public override void ClearGame()
    {
        base.ClearGame();
        ClearSelectData(true);
        //����ս������
        fightData.Clear();
        //��������
        CreatureHandler.Instance.manager.Clear();
        //ս������
        FightHandler.Instance.manager.Clear();
        //AI����
        AIHandler.Instance.manager.Clear();
        //����ս������
        WorldHandler.Instance.UnLoadFightScene();
    }

    /// <summary>
    /// ����-ѡ������
    /// </summary>
    public void UpdateGameForSelectCreature()
    {
        //�����ѡ�е�����
        if (selectCreature != null)
        {
            RayUtil.RayToScreenPointForMousePosition(10, 1 << LayerInfo.Ground, out bool isCollider, out RaycastHit hit, CameraHandler.Instance.manager.mainCamera);
            if (isCollider && hit.collider != null)
            {
                GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreaureSelectPreview(selectCreatureCard.cardData.fightCreatureData);
                objSelectPreivew.gameObject.SetActive(true);
                Vector3 hitPoint = hit.point;

                if (hitPoint.x < 1) hitPoint.x = 1;
                if (hitPoint.x > 10) hitPoint.x = 10;
                if (hitPoint.z > 6) hitPoint.z = 6;
                if (hitPoint.z < 1) hitPoint.z = 1;

                Vector3Int targetPos = Vector3Int.RoundToInt(hitPoint);
                selectCreature.transform.position = hitPoint;
                objSelectPreivew.transform.position = targetPos;
                selectCreaturePutPost = targetPos;
            }
        }
    }

    /// <summary>
    /// ����-����������
    /// </summary>
    public void UpdateGameForAttCreate()
    {
        fightData.timeUpdateForAttCreate += (Time.deltaTime * fightData.gameSpeed);
        if (fightData.timeUpdateForAttCreate > fightData.timeUpdateTargetForAttCreate)
        {
            fightData.timeUpdateForAttCreate = 0;
            //����һ������
            CreatureHandler.Instance.CreateAttCreature(fightData.gameProgress, fightData.currentFightAttCreateDetails);
        }
    }

    /// <summary>
    /// ѡ����һ�ŷ�����
    /// </summary>
    public void SelectCard(UIViewCreatureCardItem targetView)
    {
        //���ԭ��û��ѡ��
        if (selectCreatureCard == null)
        {

        }
        //���ԭ����ѡ�� ��Ҫȡ��ԭ����ѡ������
        else
        {
            //���ѡ�е������ǵ�ǰ������ ��������
            if (targetView == selectCreatureCard)
                return;
            ClearSelectData();
        }
        selectCreatureCard = targetView;
        CreatureHandler.Instance.CreateDefCreature(targetView.cardData.fightCreatureData.creatureData, (targetObj) =>
        {
            selectCreature = targetObj;
        });
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_SelectCard, selectCreatureCard);
    }

    /// <summary>
    /// ȡ��ѡ����һ�ſ�
    /// </summary>
    public void UnSelectCard()
    {
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_UnSelectCard, selectCreatureCard);
        ClearSelectData();
    }

    /// <summary>
    /// ���ÿ�Ƭ
    /// </summary>
    public void PutCard()
    {
        if (selectCreature == null)
            return;
        bool checkPosHasMainCreature = fightData.CheckFightPositionHasCreature(selectCreaturePutPost);
        if (checkPosHasMainCreature)
        {
            //�Ѿ���������
            return;
        }
        int createMagic = selectCreatureCard.cardData.creatureData.GetCreateMagic();
        if (fightData.currentMagic < createMagic)
        {
            //ħ������
            EventHandler.Instance.TriggerEvent(EventsInfo.Toast_NoEnoughCreateMagic);
            return;
        }
        //�۳�ħ��
        fightData.ChangeMagic(-createMagic);
        //��������λ��
        selectCreature.transform.position = selectCreaturePutPost;
        //����ս������
        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity(selectCreature, selectCreatureCard.cardData.fightCreatureData);
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefCreatureEntity>(actionBeforeStart: (targetEntity) =>
        {
            targetEntity.InitData(gameFightCreatureEntity);
        });

        fightData.SetFightPosition(selectCreaturePutPost, gameFightCreatureEntity);
        selectCreature = null;

        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_PutCard, selectCreatureCard);
        ClearSelectData();
    }

    /// <summary>
    /// ����ѡ�������
    /// </summary>
    public void ClearSelectData(bool isDestroyImm = false)
    {
        GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreaureSelectPreview();
        objSelectPreivew.gameObject.SetActive(false);
        //����Ԥ��
        if (selectCreature != null)
        {
            if (isDestroyImm)
            {
                GameObject.DestroyImmediate(selectCreature);
            }
            else
            {
                CreatureHandler.Instance.RemoveCreatureObj(selectCreature, CreatureTypeEnum.FightDef);
            }
        }
        selectCreature = null;
        selectCreatureCard = null;
    }


}
