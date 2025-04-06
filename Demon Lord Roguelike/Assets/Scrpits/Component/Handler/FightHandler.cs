using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Spine.Unity;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;

public class FightHandler : BaseHandler<FightHandler, FightManager>
{
    public void Update()
    {
        UpdateHandleForAttackModePrefab();
        UpdateHandleForFightPrefab();
        UpdateHandleTimeCountDown();
    }

    #region  Update更新
    /// <summary>
    /// 更新攻击模组
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
    /// 更新战斗预制
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
    /// 更新倒计时
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
    #endregion

    #region  战斗相关计时
    /// <summary>
    /// 创建一个倒计时
    /// </summary>
    public void CreateTimeCountDown(float countDownTime, Action<GameTimeCountDownBean> actionForEnd)
    {
        var targetCountDown = manager.GetNewTimeCountDown();
        targetCountDown.timeUpdateMax = countDownTime;
        targetCountDown.actionForEnd = actionForEnd;
    }

    /// <summary>
    /// 移除一个倒计时
    /// </summary>

    public void RemoveTimeCountDown(GameTimeCountDownBean targetData)
    {
        manager.RemoveTimeCountDown(targetData);
    }
    #endregion

    #region  攻击模块相关
    /// <summary>
    /// 开始创建攻击模块
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="attacked">被攻击者</param>
    /// <param name="actionForCreateEnd">创建结束</param>
    public void StartCreateAttackMode(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForCreateEnd)
    {
        //只保存基础生物ID和武器ID 用于初始化攻击的样式
        long weaponItemId = 0;
        long creatureId = attacker.fightCreatureData.creatureData.creatureInfo.id;
        int attackModeId = attacker.fightCreatureData.creatureData.creatureInfo.attack_mode;
        var weaponItemData = attacker.fightCreatureData.creatureData.GetEquip(ItemTypeEnum.Weapon);
        if (weaponItemData != null)
        {
            weaponItemId = weaponItemData.itemId;
        }
        else
        {
            weaponItemId = attacker.fightCreatureData.creatureData.creatureInfo.GetEquipBaseWeaponId();
        }
        manager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            //只保存基础生物ID和武器ID 用于初始化攻击的样式
            attackMode.creatureId = creatureId;
            attackMode.weaponItemId =  weaponItemId;
            if(attacker == null)
            {
                attackMode.startPostion = Vector3.zero;
            }
            else
            {
                attackMode.startPostion = attacker.creatureObj.transform.position;
            }
            attackMode.gameObject.transform.position = attackMode.startPostion;
            attackMode.gameObject.SetActive(true);
            attackMode.InitAttackModeShow();

            if (attackMode.spriteRenderer != null)
            {
                attackMode.spriteRenderer.transform.eulerAngles = CameraHandler.Instance.manager.mainCamera.transform.eulerAngles;
            }

            attackMode.StartAttack(attacker,attacked,actionForCreateEnd);
        });
    }

    /// <summary>
    /// 移除一个攻击预制
    /// </summary>
    public void RemoveAttackModePrefab(BaseAttackMode targetMode)
    {
        targetMode.gameObject.SetActive(false);
        manager.RemoveAttackModePrefab(targetMode);
    }
    #endregion

    #region  战斗场景物品
    /// <summary>
    /// 创建掉落金币
    /// </summary>
    /// <param name="dropPos"></param>
    public void CreateDropCrystal(Vector3 dropPos)
    {
        manager.GetDropCrystalPrefab((targetPrefab) =>
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
                //设置于摄像头角度持平
                var mainCamera = CameraHandler.Instance.manager.mainCamera;
                targetPrefab.spriteRenderer.transform.eulerAngles = mainCamera.transform.eulerAngles;
            }
            if (targetPrefab.collider != null)
            {
                targetPrefab.collider.enabled = false;
            }

            //播放一个掉落动画
            float randomX = UnityEngine.Random.Range(-0.5f, 0.5f);
            float randomZ = UnityEngine.Random.Range(-0.5f, 0.5f);
            Vector3 endPos = new Vector3(dropPos.x + randomX, dropPos.y + 0.1f, dropPos.z + randomZ);
            Vector3 startPos = dropPos + new Vector3(0, 0.5f, 0);
            // 使用DOPath创建抛物线移动动画
            targetPrefab.gameObject.transform.position = startPos;
            targetPrefab.gameObject.transform
                .DOJump(endPos, 0.3f, 2, 0.8f)
                .SetEase(Ease.Linear)// 设置动画的缓动效果
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
    /// 创建掉落魔力
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
                //设置于摄像头角度持平
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
    /// 移除战斗预制
    /// </summary>
    public void RemoveFightPrefab(GameFightPrefabEntity targetEntity)
    {
        targetEntity.gameObject.SetActive(false);
        manager.RemoveFightPrefabCommon(targetEntity);
    }
    #endregion
}
