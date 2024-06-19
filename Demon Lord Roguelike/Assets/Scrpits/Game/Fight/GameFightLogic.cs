using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightLogic : BaseGameLogic
{
    //ս������
    public FightBean fightData;

    public GameObject selectCreature;    //ѡ�������
    public FightCreatureBean selectCreatureData;//ѡ�����￨Ƭ
    public Vector3Int selectCreaturePutPost;    //ѡ�������ķ���λ��

    /// <summary>
    /// ׼����Ϸ
    /// </summary>
    public override void PreGame()
    {
        base.PreGame();
        //����ս�������ӽ�
        CameraHandler.Instance.SetFightSceneCamera();
        //����ս������
        WorldHandler.Instance.LoadFightScene(1, (targetObj) =>
        {
            //����ս������
            GameControlHandler.Instance.SetFightControl();
            //�ر�LoadingUI
            var uiFightMain = UIHandler.Instance.OpenUIAndCloseOther<UIFightMain>();
            uiFightMain.InitData();
            StartGame();
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
                GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreaureSelectPreview();
                objSelectPreivew.gameObject.SetActive(true);
                Vector3Int targetPos = Vector3Int.RoundToInt(hit.point);
                selectCreature.transform.position = hit.point;
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
    public void SelectCard(FightCreatureBean fightCreature)
    {
        //���ԭ��û��ѡ��
        if (selectCreatureData == null)
        {

        }
        //���ԭ����ѡ�� ��Ҫȡ��ԭ����ѡ������
        else
        {
            //���ѡ�е������ǵ�ǰ������ ��������
            if (fightCreature == selectCreatureData)
                return;
            ClearSelectData();
        }
        selectCreatureData = fightCreature;
        CreatureHandler.Instance.CreateDefCreature(1, (targetObj) =>
        {
            selectCreature = targetObj;
        });
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_SelectCard, selectCreatureData);
    }

    /// <summary>
    /// ȡ��ѡ����һ�ſ�
    /// </summary>
    public void UnSelectCard()
    {
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_UnSelectCard, selectCreature, selectCreatureData);
        ClearSelectData();
    }

    /// <summary>
    /// ���ÿ�Ƭ
    /// </summary>
    public void PutCard()
    {
        if (selectCreature == null)
            return;
        bool checkPosHasMainCreature = fightData.CheckFightPositionHasCreature(new Vector2Int(selectCreaturePutPost.x, selectCreaturePutPost.z));
        if (checkPosHasMainCreature)
        {
            //�Ѿ���������
            return;
        }
        int createMagic = selectCreatureData.GetCreateMagic();
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
        GameFightCreatureEntity gameFightCreatureEntity = new GameFightCreatureEntity();
        gameFightCreatureEntity.aiEntity = AIHandler.Instance.CreateAIEntity<AIDefCreatureEntity>();
        gameFightCreatureEntity.fightCreatureData = selectCreatureData;
        gameFightCreatureEntity.creatureObj = selectCreature;

        fightData.SetFightPosition(new Vector2Int(selectCreaturePutPost.x, selectCreaturePutPost.z), gameFightCreatureEntity);
        selectCreature = null;

        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_PutCard, gameFightCreatureEntity);
        ClearSelectData();
    }

    /// <summary>
    /// ����ѡ�������
    /// </summary>
    public void ClearSelectData()
    {
        GameObject objSelectPreivew = CreatureHandler.Instance.manager.GetCreaureSelectPreview();
        objSelectPreivew.gameObject.SetActive(false);
        //����Ԥ��
        if (selectCreature != null)
        {
            CreatureHandler.Instance.RemoveCreatureObj(selectCreature, CreatureTypeEnum.FightDef);
        }
        selectCreature = null;
        selectCreatureData = null;
    }
}
