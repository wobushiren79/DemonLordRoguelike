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
                if (itemAttackMode.isValid)
                {
                    itemAttackMode.Update();
                }
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

    #region  战斗相关
    /// <summary>
    /// 获取被攻击数据
    /// </summary>
    public FightUnderAttackBean GetFightUnderAttackData(BaseAttackMode baseAttackMode, string attackedId)
    {
        var targetData = manager.GetFightUnderAttackData();
        targetData.SetData(baseAttackMode, attackedId);
        return targetData;
    }
    
    /// <summary>
    /// 获取被攻击数据
    /// </summary>
    public FightUnderAttackBean GetFightUnderAttackData(BuffEntityBean buffEntityData, int attackerDamage)
    {
        var targetData = manager.GetFightUnderAttackData();
        targetData.SetData(buffEntityData, attackerDamage);
        return targetData;
    }

    /// <summary>
    /// 移除被攻击数据
    /// </summary>
    public void RemoveFightUnderAttackData(FightUnderAttackBean fightUnderAttackData)
    {
        manager.RemoveFightUnderAttackData(fightUnderAttackData);
    }
    #endregion

    #region  攻击模块相关
    /// <summary>
    /// 开始创建攻击模块
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="attacked">被攻击者</param>
    /// <param name="actionForCreateEnd">创建结束</param>
    public void StartCreateAttackMode(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForCreateEnd)
    {
        //只保存基础生物ID和武器ID 用于初始化攻击的样式
        long weaponItemId = 0;
        CreatureBean attackerCreatureData = attacker.fightCreatureData.creatureData;
        CreatureInfoBean attackerCreatureInfo = attackerCreatureData.creatureInfo;
        
        long creatureId = attackerCreatureInfo.id;
        int attackModeId = attackerCreatureInfo.attack_mode;
        var weaponItemData = attackerCreatureData.GetEquip(ItemTypeEnum.Weapon);
        if (weaponItemData != null)
        {
            weaponItemId = weaponItemData.itemId;
        }
        else
        {
            weaponItemId = attackerCreatureInfo.GetEquipBaseWeaponId();
        }
        AttackModeBean attackModeData = manager.GetAttackModeData(attackModeId);
        //保存基础生物ID和武器ID 用于初始化攻击的样式
        attackModeData.attackerCreatureId = creatureId;
        attackModeData.attackerWeaponItemId = weaponItemId;

        manager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack(attacker, attacked, actionForCreateEnd);
        });
    }

    /// <summary>
    /// 开始攻击
    /// </summary>
    public void StartCreateAttackMode(AttackModeBean attackModeData)
    {
        manager.GetAttackModePrefab(attackModeData.attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
        });
    }

    /// <summary>
    /// 移除一个攻击预制
    /// </summary>
    public async void RemoveAttackMode(BaseAttackMode targetMode)
    {
        targetMode.isValid = false;
        //延迟一帧执行
        await new WaitNextFrame();
        //移除预制
        if (targetMode.gameObject != null)
        {
            targetMode.gameObject.SetActive(false);
        }
        manager.RemoveAttackModePrefab(targetMode);
        //移除数据
        if (targetMode.attackModeData!=null)
        {
            manager.RemoveAttackModeData(targetMode.attackModeData);
            targetMode.attackModeData = null;
        }
    }
    #endregion

    #region  战斗场景物品
    /// <summary>
    /// 创建掉落金币
    /// </summary>
    public void CreateDropCrystal(FightDropCrystalBean fightDropCrystal)
    {
        manager.GetDropCrystalPrefab((targetPrefab) =>
        {
            targetPrefab.valueInt = fightDropCrystal.crystalNum;
            targetPrefab.lifeTime = fightDropCrystal.lifeTime;
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
            Vector3 endPos = new Vector3(fightDropCrystal.dropPos.x + randomX, fightDropCrystal.dropPos.y + 0.1f, fightDropCrystal.dropPos.z + randomZ);
            Vector3 startPos = fightDropCrystal.dropPos + new Vector3(0, 0.5f, 0);
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
            //移除掉落数据到缓存
            manager.RemoveFightDropCrystalBean(fightDropCrystal);
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
    public void RemoveFightPrefab(FightPrefabEntity targetEntity)
    {
        targetEntity.gameObject.SetActive(false);
        manager.RemoveFightPrefabCommon(targetEntity);
    }
    #endregion
}
