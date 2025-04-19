using System;
using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class BaseAttackMode
{
    //当前obj
    public GameObject gameObject;
    public SpriteRenderer spriteRenderer;
    //信息
    public AttackModeInfoBean attackModeInfo;
    //攻击者的攻击力
    public int attackerDamage;
    //起始位置
    public Vector3 startPostion;
    //目标位置
    public Vector3 targetPos;
    //攻击方向
    public Vector3 attackDirection;
    //被攻击者的层级
    public int attackedLayer;

    //攻击者ID
    public string attackerId;
    //被攻击者ID
    public string attackedId;

    //发出攻击的生物ID
    public long creatureId;
    //发出攻击的武器
    public long weaponItemId;

    /// <summary>
    /// 初始化攻击样式
    /// </summary>
    public virtual void InitAttackModeShow()
    {
        //如果没有找到对应武器 则使用?图标
        if (weaponItemId == 0)
        {
            if (spriteRenderer != null)
            {
                IconHandler.Instance.GetUnKnowSprite((targetSprite) =>
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = targetSprite;
                    }
                });
            }
        }
        else
        {
            var weaponItemInfo = ItemsInfoCfg.GetItemData(weaponItemId);
            if (weaponItemInfo != null && !weaponItemInfo.attack_mode_data.IsNull())
            {
                weaponItemInfo.HandleItemsInfoAttackModeData(this);
            }
        }
    }

    /// <summary>
    /// 开始攻击
    /// </summary>
    /// <param name="attacker">攻击方</param>
    /// <param name="attacked">被攻击方</param>
    public virtual void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        attackerDamage = 0;
        if (attacker != null)
        {
            if (attacker.fightCreatureData != null)
            {
                if (attacker.fightCreatureData.creatureData != null)
                {
                    //设置攻击者ID
                    attackerId = attacker.fightCreatureData.creatureData.creatureId;
                    //设置伤害
                    attackerDamage = attacker.fightCreatureData.creatureData.GetATK();
                }
            }
        }
        if (attacked != null)
        {
            if (attacked.creatureObj != null)
            {
                targetPos = attacked.creatureObj.transform.position;
                attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - attacker.creatureObj.transform.position);
                attackedLayer = attacked.creatureObj.layer;

            }
            if (attacked.fightCreatureData != null)
            {
                if (attacked.fightCreatureData.creatureData != null)
                {
                    //设置被攻击者ID
                    attackedId = attacked.fightCreatureData.creatureData.creatureId;
                }
            }
            //LogUtil.Log($"attacker_{attacker.creatureObj.transform.position} attacked_{attacked.creatureObj.transform.position} attackDirection_{attackDirection}");
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    public virtual void Update()
    {

    }

    /// <summary>
    /// 清理自己
    /// </summary>
    public virtual void Destory()
    {
        attackerDamage = 0;
        attackerId = null;
        attackedId = null;
        creatureId = 0;
        weaponItemId = 0;
        FightHandler.Instance.RemoveAttackModePrefab(this);
    }

    #region  检测相关
    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual bool CheckHitTarget(out GameFightCreatureEntity gameFightCreatureEntity)
    {
        gameFightCreatureEntity = null;
        RayUtil.RayToCast(gameObject.transform.position, attackDirection, attackModeInfo.collider_size, 1 << attackedLayer, out RaycastHit hit);
        if (hit.collider != null)
        {
            string creatureId = hit.collider.gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureTypeEnum.None);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                gameFightCreatureEntity = targetCreature;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 检测范围内敌人
    /// </summary>
    public bool CheckHitTargetArea(Vector3 checkPosition, Action<GameFightCreatureEntity> actionForHitItem)
    {
        bool hasHitter = false;
        Collider[] targetColliders = RayUtil.OverlapToSphere(checkPosition, attackModeInfo.collider_area_size, 1 << attackedLayer);
        //绘制测试范围
        DrawTestArea(checkPosition, attackModeInfo.collider_area_size, 1);

        if (targetColliders != null)
        {
            for (int i = 0; i < targetColliders.Length; i++)
            {
                var itemHitterCollder = targetColliders[i];
                string creatureId = itemHitterCollder.gameObject.name;
                GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureTypeEnum.None);
                if (targetCreature != null && !targetCreature.IsDead())
                {
                    hasHitter = true;
                    actionForHitItem?.Invoke(targetCreature);
                }
            }
        }
        return hasHitter;
    }

    public void DrawTestArea(Vector3 startPostion, float areaSize, float duration)
    {
#if UNITY_EDITOR
        Debug.DrawRay(startPostion, new Vector3(areaSize, 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(-areaSize, 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, areaSize), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, -areaSize), Color.red, duration);
#endif
    }
    #endregion
}
