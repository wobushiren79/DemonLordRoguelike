using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Spine.Unity;

public class FightHandler : BaseHandler<FightHandler, FightManager>
{
    public void Update()
    {
        UpdateHandleForAttackModePrefab();
        UpdateHandleForFightPrefab();
        UpdateHandleTimeCountDown();
    }

    /// <summary>
    /// ���¹���ģ��
    /// </summary>
    public void UpdateHandleForAttackModePrefab()
    {
        if (manager.listAttackModePrefab.Count > 0)
        {
            for (int i = 0; i < manager.listAttackModePrefab.Count; i++)
            {
                var itemAttackMode = manager.listAttackModePrefab[i];
                itemAttackMode.Update();
            }
        }
    }

    /// <summary>
    /// ����ս��Ԥ��
    /// </summary>
    public void UpdateHandleForFightPrefab()
    {
        if (manager.listFightPrefab.Count > 0)
        {
            for (int i = 0; i < manager.listFightPrefab.Count; i++)
            {
                var itemAttackMode = manager.listFightPrefab[i];
                itemAttackMode.Update();
            }
        }
    }

    /// <summary>
    /// ���µ���ʱ
    /// </summary>
    public void UpdateHandleTimeCountDown()
    {
        if (manager.listTimeCountDown.Count > 0)
        {
            for (int i = 0; i < manager.listTimeCountDown.Count; i++)
            {
                var itemCountDown = manager.listTimeCountDown[i];
                itemCountDown.Update(Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// ����һ������ʱ
    /// </summary>
    public void CreateTimeCountDown(float countDownTime,Action<GameTimeCountDownBean> actionForEnd)
    {
        var targetCountDown = manager.GetNewTimeCountDown();
        targetCountDown.timeUpdateMax = countDownTime;
        targetCountDown.actionForEnd = actionForEnd;
    }

    /// <summary>
    /// �Ƴ�һ������ʱ
    /// </summary>

    public void RemoveTimeCountDown(GameTimeCountDownBean targetData)
    {
        manager.RemoveTimeCountDown(targetData);
    }

    /// <summary>
    /// ����һ������Ԥ��
    /// </summary>
    public void CreateAttackModePrefab(int attackModeId, Action<BaseAttackMode> actionForComplete)
    {
        manager.GetAttackModePrefab(attackModeId, (targetPrefab) =>
        {
            targetPrefab.gameObject.SetActive(true);
            if (targetPrefab.spriteRenderer != null)
            {
                targetPrefab.spriteRenderer.transform.eulerAngles = CameraHandler.Instance.manager.mainCamera.transform.eulerAngles;
            }
  
            actionForComplete?.Invoke(targetPrefab);
        });
    }

    /// <summary>
    /// �Ƴ�һ������Ԥ��
    /// </summary>
    public void RemoveAttackModePrefab(BaseAttackMode targetMode)
    {
        targetMode.gameObject.SetActive(false);
        manager.RemoveAttackModePrefab(targetMode);
    }

    /// <summary>
    /// ����������
    /// </summary>
    /// <param name="dropPos"></param>
    public void CreateDropCoin(Vector3 dropPos)
    {
        manager.GetDropCoinPrefab((targetPrefab) =>
        {           
            if (targetPrefab.spriteRenderer == null)
            {
                Transform rendererTF = targetPrefab.gameObject.transform.Find("Renderer");
                if (rendererTF != null)
                {
                    targetPrefab.spriteRenderer = rendererTF.GetComponent<SpriteRenderer>();
                }
            }
            if (targetPrefab.collider == null)
            {
                targetPrefab.collider = targetPrefab.gameObject.GetComponent<BoxCollider>();
            }

            if (targetPrefab.spriteRenderer != null)
            {           
                //����������ͷ�Ƕȳ�ƽ
                var mainCamera = CameraHandler.Instance.manager.mainCamera;
                targetPrefab.spriteRenderer.transform.eulerAngles = mainCamera.transform.eulerAngles;
            }
            if (targetPrefab.collider != null)
            {
                targetPrefab.collider.enabled = false;
            }
 
            //����һ�����䶯��
            float randomX = UnityEngine.Random.Range(-0.5f, 0.5f);
            float randomZ = UnityEngine.Random.Range(-0.5f, 0.5f);
            Vector3 endPos = new Vector3(dropPos.x + randomX, dropPos.y + 0.1f, dropPos.z + randomZ);
            Vector3 startPos = dropPos + new Vector3(0, 0.5f, 0);
            // ʹ��DOPath�����������ƶ�����
            targetPrefab.gameObject.transform.position = startPos;
            targetPrefab.gameObject.transform
                .DOJump(endPos, 0.3f, 2, 0.8f)
                .SetEase(Ease.Linear)// ���ö����Ļ���Ч��
                .OnComplete(() =>
                {
                    targetPrefab.SetState(GameFightPrefabStateEnum.DropCheck);
                    if (targetPrefab.collider != null)
                    {
                        targetPrefab.collider.enabled = true;
                    }
                });
        });
    }

    /// <summary>
    /// ��������ħ��
    /// </summary>
    /// <param name="dropPos"></param>
    public void CreateDropMagic(Vector3 dropPos)
    {
        manager.GetDropMagicPrefab((targetPrefab) =>
        {
            if (targetPrefab.spriteRenderer == null)
            {
                Transform rendererTF = targetPrefab.gameObject.transform.Find("Renderer");
                if (rendererTF != null)
                {
                    targetPrefab.spriteRenderer = rendererTF.GetComponent<SpriteRenderer>();
                }
            }
            if (targetPrefab.collider == null)
            {
                targetPrefab.collider = targetPrefab.gameObject.GetComponent<BoxCollider>();
            }

            if (targetPrefab.spriteRenderer != null)
            {
                //����������ͷ�Ƕȳ�ƽ
                var mainCamera = CameraHandler.Instance.manager.mainCamera;
                targetPrefab.spriteRenderer.transform.eulerAngles = mainCamera.transform.eulerAngles;
            }
            if (targetPrefab.collider != null)
            {
                targetPrefab.collider.enabled = false;
            }

            targetPrefab.gameObject.transform.position = dropPos;
        });
    }

    /// <summary>
    /// �Ƴ�ս��Ԥ��
    /// </summary>
    public void RemoveFightPrefab(GameFightPrefabEntity targetEntity)
    {
        targetEntity.gameObject.SetActive(false);
        manager.RemoveFightPrefabCommon(targetEntity);
    }
}
