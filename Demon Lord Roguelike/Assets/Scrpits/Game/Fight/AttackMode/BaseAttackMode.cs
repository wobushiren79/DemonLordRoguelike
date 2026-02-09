using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class BaseAttackMode
{
    public bool isValid = true;
    //当前obj
    public GameObject gameObject;
    //sprite渲染（不一定有）
    public SpriteRenderer spriteRenderer;
    //攻击模块信息
    public AttackModeInfoBean attackModeInfo;
    //攻击模块数据
    public AttackModeBean attackModeData;

    /// <summary>
    /// 初始化攻击样式
    /// </summary>
    public virtual void InitAttackModeShow()
    {
        //如果没有找到对应武器 则使用?图标
        if (attackModeData.attackerWeaponItemId == 0)
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
            var weaponItemInfo = ItemsInfoCfg.GetItemData(attackModeData.attackerWeaponItemId);
            if (weaponItemInfo != null && !weaponItemInfo.attack_mode_data.IsNull())
            {
                weaponItemInfo.HandleItemsInfoAttackModeData(this);
            }
        }
    }

    /// <summary>
    /// 开始攻击初始化
    /// </summary>
    public virtual void StartAttackInit(AttackModeBean attackModeData)
    {
        this.isValid = true;
        this.attackModeData = attackModeData;
        //初始化攻击模块外形
        InitAttackModeShow();
        //设置渲染朝向
        if (spriteRenderer != null)
        {
            CameraHandler.Instance.ChangeAngleForCamera(spriteRenderer.transform);
        }
    }

    /// <summary>
    /// 开始攻击 基础-每一个StartAttack都会调用
    /// </summary>
    public virtual void StartAttackBase()
    {
        if (gameObject != null)
        {
            gameObject.transform.position = attackModeData.startPos;
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 开始攻击-默认
    /// </summary>
    public virtual void StartAttack()
    {
        StartAttackBase();
    }

    /// <summary>
    /// 开始攻击-生物
    /// </summary>
    /// <param name="attacker">攻击方</param>
    /// <param name="attacked">被攻击方</param>
    public virtual void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        attackModeData.attackerDamage = 0;
        if (attacker != null)
        {
            if (attacker.fightCreatureData != null)
            {
                var creatureData = attacker.fightCreatureData.creatureData;
                if (creatureData != null)
                {
                    //设置攻击者ID
                    attackModeData.attackerId = creatureData.creatureUUId;
                    //设置伤害
                    attackModeData.attackerDamage = (int)attacker.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
                    //提示设置暴击概率 
                    attackModeData.attackerCRT = attacker.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.CRT);
                    //设置起始位置
                    Vector3 offsetPosition = creatureData.creatureInfo.GetAttackStartPosition();
                    attackModeData.startPos = attacker.creatureObj.transform.position + offsetPosition;
                    //获取被攻击者的层级
                    attackModeData.attackedLayerTarget = attacker.fightCreatureData.GetCreatrueLayer(true);
                }
            }
        }
        else
        {
            attackModeData.startPos = Vector3.zero;
        }    
        if (attacked != null)
        {
            if (attacked.creatureObj != null)
            {
                //设置被攻击者位置
                attackModeData.targetPos = attacked.creatureObj.transform.position;
                //设置攻击方朝向
                attackModeData.attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - attacker.creatureObj.transform.position);
            }
            if (attacked.fightCreatureData != null)
            {
                if (attacked.fightCreatureData.creatureData != null)
                {
                    //设置被攻击者ID
                    attackModeData.attackedId = attacked.fightCreatureData.creatureData.creatureUUId;
                }
            }
        }
        StartAttackBase();
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
    public virtual void Destroy(bool isPermanently = false)
    {
        this.isValid = false;
        if (isPermanently)
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }
        else
        {
            FightHandler.Instance.RemoveAttackMode(this);
        }
    }

    #region  特效
    /// <summary>
    /// 播放攻击特效
    /// </summary>
    /// <param name="startPosition"></param>
    public void PlayEffectForHit(Vector3 startPosition)
    {
        if (attackModeInfo.effect_hit != 0)
        {
            float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
            Direction2DEnum effectDirection = attackModeData.attackDirection.x > 0 ? Direction2DEnum.Right : Direction2DEnum.Left;
            EffectHandler.Instance.ShowEffect(attackModeInfo.effect_hit, startPosition, direction: effectDirection, size: colliderAreaSize[0]);
        }
    }
    #endregion

    #region  检测相关
    /// <summary>
    /// 检测是否到达边界
    /// </summary>
    /// <returns></returns>
    public virtual bool CheckIsMoveBound(GameObject targetObj)
    {
        if (targetObj.transform.position.x > 15 || targetObj.transform.position.x < -5 ||
               targetObj.transform.position.y < -5 || targetObj.transform.position.y > 15)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual FightCreatureEntity CheckHitTargetForSingle()
    {
        return CheckHitTargetForSingle(gameObject.transform.position);
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual List<FightCreatureEntity> CheckHitTarget()
    {
        return CheckHitTarget(gameObject.transform.position);
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual FightCreatureEntity CheckHitTargetForSingle(Vector3 checkPosition)
    {
        List<FightCreatureEntity> listData = CheckHitTarget(checkPosition);
        if (listData.IsNull())
        {
            return null;
        }
        return listData[0];
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual List<FightCreatureEntity> CheckHitTarget(Vector3 checkPosition)
    {
        CreatureSearchType searchType = attackModeInfo.GetCreatureSerachType();
        CreatureFightTypeEnum searchCreatureType = CreatureFightTypeEnum.None;
        if (attackModeData.attackedLayerTarget == LayerInfo.CreatureAtt)
        {
            searchCreatureType = CreatureFightTypeEnum.FightAttack;
        }
        else if (attackModeData.attackedLayerTarget == LayerInfo.CreatureDef)
        {
            searchCreatureType = CreatureFightTypeEnum.FightDefense;
        }
        return FightCreatureSearchUtil.FindCreatureEntity(searchType, searchCreatureType, checkPosition, attackModeData.attackDirection, Vector3.zero, attackModeInfo.collider_size);
    }

    /// <summary>
    /// 检测范围内敌人
    /// </summary>
    public bool CheckHitTargetArea(Vector3 checkPosition, Action<FightCreatureEntity> actionForHitItem)
    {
        bool hasHitter = false;
        Collider[] targetColliders = GetHitTargetAreaCollider(checkPosition);
        if (targetColliders != null)
        {
            for (int i = 0; i < targetColliders.Length; i++)
            {
                var itemHitterCollder = targetColliders[i];
                string creatureId = itemHitterCollder.gameObject.name;
                GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureFightTypeEnum.None);
                if (targetCreature != null && !targetCreature.IsDead())
                {
                    hasHitter = true;
                    actionForHitItem?.Invoke(targetCreature);
                }
            }
        }
        return hasHitter;
    }

    /// <summary>
    /// 获取打击区域内的Collider
    /// </summary>
    /// <returns></returns>
    public Collider[] GetHitTargetAreaCollider(Vector3 checkPosition)
    {        
        CreatureSearchType searchType = attackModeInfo.GetColliderAreaSerachType();
        float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
        Collider[] colliders = null;
        switch (searchType)
        {
            case CreatureSearchType.AreaSphere:
                //圆形半径
                colliders = RayUtil.OverlapToSphere(checkPosition, colliderAreaSize[0], 1 << attackModeData.attackedLayerTarget);
                //绘制测试范围
                DrawTestAreaForSphere(checkPosition, colliderAreaSize[0], 1);
                break;
            case CreatureSearchType.AreaSphereFront:
                break;
            case CreatureSearchType.AreaBox:
                break;
            case CreatureSearchType.AreaBoxFront:
                Vector3 offsetPosition;
                if (attackModeData.attackDirection.x > 0)
                {
                    offsetPosition = new Vector3(colliderAreaSize[0], 0, 0);
                }
                else
                {
                    offsetPosition = new Vector3(-colliderAreaSize[0], 0, 0);
                }
                Vector3 halfEx = new Vector3(colliderAreaSize[0], colliderAreaSize[1], colliderAreaSize[2]);
                colliders = RayUtil.OverlapToBox(checkPosition + offsetPosition, halfEx, 1 << attackModeData.attackedLayerTarget);
                DrawTestAreaForBox(checkPosition + offsetPosition, halfEx, 1);
                break;
            default:
                break;
        }
        return colliders;
    }

    public static void DrawTestAreaForSphere(Vector3 startPostion, float areaSize, float duration)
    {
#if UNITY_EDITOR
        Debug.DrawRay(startPostion, new Vector3(areaSize, 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(-areaSize, 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, areaSize), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, -areaSize), Color.red, duration);
#endif
    }

    public static void DrawTestAreaForBox(Vector3 startPostion, Vector3 halfEx, float duration)
    {
#if UNITY_EDITOR
        Debug.DrawRay(startPostion, new Vector3(halfEx[0], 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(-halfEx[0], 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, halfEx[1], 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, -halfEx[1], 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, halfEx[2]), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, -halfEx[2]), Color.red, duration);
#endif
    }
    #endregion
}
