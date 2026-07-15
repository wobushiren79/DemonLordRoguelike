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
    //实例ID（由 FightManager 分配，用于 DictionaryList 快速移除）
    public long instanceId;
    //当前obj（预制字段保留：DSP 迁移过渡期仍作渲染/兼容载体，位置真实源已改为 position）
    public GameObject gameObject;
    //弹道当前世界坐标（DSP 方案B 位置权威源，脱离 transform；gameObject 非空时同步写回其 transform，供 AttackModeInstanceRenderer 批量绘制读取）
    public Vector3 position;
    //sprite渲染（不一定有）
    public SpriteRenderer spriteRenderer;
    //攻击模块信息
    public AttackModeInfoBean attackModeInfo;
    //攻击模块数据
    public AttackModeBean attackModeData;
    //攻击搜索的生物战斗类型（由 attackedLayerTarget 推导，StartAttack 时缓存，避免每帧重算）
    protected CreatureFightTypeEnum searchCreatureType = CreatureFightTypeEnum.None;
    //本帧射线批处理命令索引（>=0 表示本帧已入队射线，检测时读批处理结果；-1 表示未入队，走 live 路径）
    protected int batchRayStart = -1;
    //攻速ASPD=100时弹道飞行速度的最大加成倍率（数值策划调整入口）
    public const float SpeedRateASPDMax = 3f;
    //弹道起点 Y 轴随机扰动幅度（±值，避免弹道起点完全重合，数值策划调整入口）
    public const float StartPosRandomRangeY = 0.05f;

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
        //位置真实源置为起点（即使无 gameObject 也生效），再同步到 transform
        position = attackModeData.startPos;
        if (gameObject != null)
        {
            gameObject.transform.position = position;
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
    /// <param name="actionForAttackEnd">攻击结束回调</param>
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
                    //设置弹道速度倍率（攻速ASPD 0~100 线性映射 1~SpeedRateASPDMax 倍，与攻击时间换算保持同一插值体系）
                    float attributeASPD = attacker.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ASPD);
                    attackModeData.attackerSpeedRate = MathUtil.InterpolationLerp(attributeASPD, 0, 100, 1f, SpeedRateASPDMax);
                    //设置起始位置（生物攻击起始位置 + 攻击模块自身偏移，再叠加 Y 轴随机扰动避免弹道起点完全重合）
                    Vector3 offsetPosition = creatureData.creatureInfo.GetAttackStartPosition() + attackModeInfo.GetStartPosOffset();
                    offsetPosition.y += UnityEngine.Random.Range(-StartPosRandomRangeY, StartPosRandomRangeY);
                    attackModeData.startPos = attacker.creatureObj.transform.position + offsetPosition;
                    //获取被攻击者的层级
                    attackModeData.attackedLayerTarget = attacker.fightCreatureData.GetCreatureLayer(true);
                    //缓存被攻击者战斗类型（用于范围检测时筛选 layer），避免每帧重算
                    if (attackModeData.attackedLayerTarget == LayerInfo.CreatureAtt)
                    {
                        searchCreatureType = CreatureFightTypeEnum.FightAttack;
                    }
                    else if (attackModeData.attackedLayerTarget == LayerInfo.CreatureDef)
                    {
                        searchCreatureType = CreatureFightTypeEnum.FightDefense;
                    }
                    else
                    {
                        searchCreatureType = CreatureFightTypeEnum.None;
                    }
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
        else
        {
            attackModeData.attackedId = "";
            if (attacker != null)
            {
                attackModeData.attackDirection = attacker.fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightAttack ? Vector3.left : Vector3.right;
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

    #region  位置(DSP 方案B 权威源)
    /// <summary>
    /// 设置弹道位置（位置真实源），并同步写回 gameObject.transform（若存在）
    /// </summary>
    public void SetPosition(Vector3 targetPosition)
    {
        position = targetPosition;
        if (gameObject != null)
        {
            gameObject.transform.position = position;
        }
    }

    /// <summary>
    /// 按世界向量平移弹道位置（等价于弹体无旋转下的 transform.Translate），并同步写回 transform
    /// </summary>
    public void TranslatePosition(Vector3 delta)
    {
        position += delta;
        if (gameObject != null)
        {
            gameObject.transform.position = position;
        }
    }
    #endregion

    /// <summary>
    /// 获取弹道实际飞行速度（配置speed_move × 攻击者攻速加成倍率）
    /// </summary>
    public float GetMoveSpeed()
    {
        return attackModeInfo.speed_move * attackModeData.attackerSpeedRate;
    }

    /// <summary>
    /// 清理自己
    /// </summary>
    public virtual void Destroy(bool isPermanently = false)
    {
        this.isValid = false;
        //重置攻击搜索类型，避免对象池复用时残留上次的目标层级
        this.searchCreatureType = CreatureFightTypeEnum.None;
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
    public void PlayEffectForHit(Vector3 startPosition, int effectIndex = 0)
    {
        long effectId = attackModeInfo.GetEffectHitId(effectIndex);
        if (effectId != 0)
        {
            float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
            Direction2DEnum effectDirection = attackModeData.attackDirection.x > 0 ? Direction2DEnum.Right : Direction2DEnum.Left;
            EffectHandler.Instance.ShowEffect(effectId, startPosition, direction: effectDirection, size: colliderAreaSize[0]);
        }
    }
    #endregion

    #region  射线批处理
    /// <summary>
    /// 收集本帧射线检测请求（在批处理调度前调用）
    /// <para>默认仅重置状态、不入队（非射线弹道）；走射线检测的子类重写此方法把射线加入批处理。</para>
    /// </summary>
    public virtual void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
    }

    /// <summary>
    /// 按当前配置把一条单射线入队到批处理（供 Ray/RaySelf 类型的单体弹道复用）
    /// <para>与 FightCreatureSearchUtil.FindCreatureEntityByRay/BySelf 的起点/方向/距离/层级保持一致。</para>
    /// </summary>
    protected void EnqueueSingleRay(FightRaycastBatch batch)
    {
        if (gameObject == null)
            return;
        int layerMask = GetSearchLayerMask();
        if (layerMask == 0)
            return;
        CreatureSearchType searchType = attackModeInfo.GetCreatureSerachType();
        Vector3 pos = position;
        Vector3 dir = attackModeData.attackDirection;
        float dist = attackModeInfo.collider_size;
        if (searchType == CreatureSearchType.RaySelf)
        {
            //远处射向自己：起点前移一个射程、方向取反
            pos += dir.x > 0 ? new Vector3(dist, 0, 0) : new Vector3(-dist, 0, 0);
            dir = -dir;
        }
        else if (searchType != CreatureSearchType.Ray)
        {
            return;
        }
        if (dir == Vector3.zero)
            return;
        batchRayStart = batch.Enqueue(pos, dir.normalized, dist, layerMask);
    }

    /// <summary>
    /// 获取射线检测的层级掩码（由 StartAttack 缓存的 searchCreatureType 推导）
    /// </summary>
    protected int GetSearchLayerMask()
    {
        if (searchCreatureType == CreatureFightTypeEnum.FightDefense)
            return 1 << LayerInfo.CreatureDef;
        if (searchCreatureType == CreatureFightTypeEnum.FightAttack)
            return 1 << LayerInfo.CreatureAtt;
        return 0;
    }

    /// <summary>
    /// 从批处理结果中解析某条命令的首个存活目标（命中窗口内跳过已死目标，取第一个存活）
    /// </summary>
    protected FightCreatureEntity ResolveFirstAliveFromBatch(int cmdIndex)
    {
        if (cmdIndex < 0)
            return null;
        FightRaycastBatch batch = FightHandler.Instance.manager.raycastBatch;
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        for (int h = 0; h < FightRaycastBatch.MaxHitsPerRay; h++)
        {
            var collider = batch.GetHit(cmdIndex, h).collider;
            //命中窗口内遇到空 collider 表示后续无更多命中
            if (collider == null)
                break;
            var targetCreature = gameFightLogic.fightData.GetCreatureById(collider.gameObject.name, searchCreatureType);
            if (targetCreature != null && !targetCreature.IsDead())
                return targetCreature;
        }
        return null;
    }

    /// <summary>
    /// 从批处理结果中解析某条命令的全部存活目标（穿透用），结果写入传入 buffer 以复用内存
    /// </summary>
    protected void ResolveAllAliveFromBatch(int cmdIndex, List<FightCreatureEntity> buffer)
    {
        if (cmdIndex < 0)
            return;
        FightRaycastBatch batch = FightHandler.Instance.manager.raycastBatch;
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        for (int h = 0; h < FightRaycastBatch.MaxHitsPerRay; h++)
        {
            var collider = batch.GetHit(cmdIndex, h).collider;
            if (collider == null)
                break;
            var targetCreature = gameFightLogic.fightData.GetCreatureById(collider.gameObject.name, searchCreatureType);
            if (targetCreature != null && !targetCreature.IsDead())
                buffer.Add(targetCreature);
        }
    }
    #endregion

    #region  检测相关
    /// <summary>
    /// 检测弹道当前位置(position)是否到达边界（DSP 方案B 首选，脱离 gameObject）
    /// </summary>
    public virtual bool CheckIsMoveBound()
    {
        return CheckIsMoveBoundByPosition(position);
    }

    /// <summary>
    /// 检测是否到达边界（兼容重载：读传入 gameObject 的 transform 位置）
    /// </summary>
    public virtual bool CheckIsMoveBound(GameObject targetObj)
    {
        return CheckIsMoveBoundByPosition(targetObj.transform.position);
    }

    /// <summary>
    /// 按世界坐标判定是否越出地图范围（x∈[-5,15]、y∈[-5,15] 之外即越界）
    /// </summary>
    private bool CheckIsMoveBoundByPosition(Vector3 checkPosition)
    {
        if (checkPosition.x > 15 || checkPosition.x < -5 ||
               checkPosition.y < -5 || checkPosition.y > 15)
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
        return CheckHitTargetForSingle(position);
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual List<FightCreatureEntity> CheckHitTarget()
    {
        return CheckHitTarget(position);
    }

    /// <summary>
    /// 检测是否击中生物
    /// </summary>
    public virtual FightCreatureEntity CheckHitTargetForSingle(Vector3 checkPosition)
    {
        //本帧已入队射线：直接读批处理结果，避免 live Physics 查询
        if (batchRayStart >= 0)
        {
            return ResolveFirstAliveFromBatch(batchRayStart);
        }
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
        //本帧已入队射线：从批处理结果解析全部存活目标(穿透用)
        if (batchRayStart >= 0)
        {
            List<FightCreatureEntity> listBatch = new List<FightCreatureEntity>(FightRaycastBatch.MaxHitsPerRay);
            ResolveAllAliveFromBatch(batchRayStart, listBatch);
            return listBatch.Count > 0 ? listBatch : null;
        }
        CreatureSearchType searchType = attackModeInfo.GetCreatureSerachType();
        //使用 StartAttack 时缓存的 searchCreatureType，避免每帧重算
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
            //循环外缓存 GameFightLogic，避免每个 collider 命中都做 GetGameLogic 反射查询
            GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
            for (int i = 0; i < targetColliders.Length; i++)
            {
                var itemHitterCollder = targetColliders[i];
                string creatureId = itemHitterCollder.gameObject.name;
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
