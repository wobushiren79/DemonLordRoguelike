using System;
using System.Collections.Generic;
using UnityEngine;

public class FightManager : BaseManager
{
    //攻击预制obj
    public Dictionary<string, GameObject> dicAttackModeObj = new Dictionary<string, GameObject>();
    //攻击预制的缓存池
    public Dictionary<long, Queue<BaseAttackMode>> dicPoolAttackModeObj = new Dictionary<long, Queue<BaseAttackMode>>();
    //攻击预制列表（用 DictionaryList 便于按 instanceId 快速 Remove 同时保留可遍历 List）
    public DictionaryList<long, BaseAttackMode> dlAttackModePrefab = new DictionaryList<long, BaseAttackMode>();
    //攻击模块实例ID自增计数器
    private long attackModeInstanceCounter = 0;

    //战斗逻辑缓存（避免热路径每次 GetGameLogic 反射查找）
    private GameFightLogic cachedGameFightLogic;
    //攻击模块数据缓存池
    public Queue<AttackModeBean> poolAttackModeData = new Queue<AttackModeBean>();
    //攻击数据缓存
    public Queue<FightUnderAttackBean> poolFightUnderAttackData = new Queue<FightUnderAttackBean>();

    public static string pathDropMagicPrefab = "Assets/LoadResources/Common/FightDropMagic.prefab";
    public static string pathDropCrystalPrefab = "Assets/LoadResources/Common/FightDropCrystal.prefab";
    //一些战斗杂项预制
    public Dictionary<string, GameObject> dicFightModeObj = new Dictionary<string, GameObject>();
    //战斗杂项缓存池
    public Dictionary<string, Queue<FightPrefabEntity>> dicPoolFightObj = new Dictionary<string, Queue<FightPrefabEntity>>();
    //战斗杂项列表
    public List<FightPrefabEntity> listFightPrefab = new List<FightPrefabEntity>();

    //倒计时
    public List<GameTimeCountDownBean> listTimeCountDown = new List<GameTimeCountDownBean>();
    public Queue<GameTimeCountDownBean> poolTimeCountDown = new Queue<GameTimeCountDownBean>();

    //掉落魔晶数据缓存池
    public Queue<FightDropCrystalBean> poolFightDropCrystalBean = new Queue<FightDropCrystalBean>();

    /// <summary>
    /// 仅清理在途的攻击模块(弹道)预制
    /// <para>用于战斗结束简易清场(ClearGameForSimple)：AI 实例被回收后，已发射的弹道仍会在 FightHandler.Update 中飞行并命中生物，</para>
    /// <para>触发已被回收(selfCreatureEntity 置空)的 AI 死亡意图导致空引用。提前销毁在途弹道可从源头阻断该执行链。</para>
    /// <para>仅销毁活跃弹道并清空活跃列表，不动对象池(后续完整 Clear 会统一处理)。</para>
    /// </summary>
    public void ClearAttackModePrefab()
    {
        var listAttackMode = dlAttackModePrefab.List;
        for (int i = 0; i < listAttackMode.Count; i++)
        {
            var item = listAttackMode[i];
            item.Destroy(true);
        }
        dlAttackModePrefab.Clear();
    }

    /// <summary>
    /// 清理所有数据
    /// </summary>
    public void Clear()
    {
        //战斗预制清理
        var listAttackMode = dlAttackModePrefab.List;
        for (int i = 0; i < listAttackMode.Count; i++)
        {
            var item = listAttackMode[i];
            item.Destroy(true);
        }
        dlAttackModePrefab.Clear();
        attackModeInstanceCounter = 0;
        foreach (var itemData in dicPoolAttackModeObj)
        {
            var queue = itemData.Value;
            while (queue.Count > 0)
            {
                var targetData = queue.Dequeue();
                targetData.Destroy(true);
            }
        }
        dicPoolAttackModeObj.Clear();

        //清理战斗逻辑缓存
        cachedGameFightLogic = null;

        //战斗杂项清理
        for (int i = 0; i < listFightPrefab.Count; i++)
        {
            var item = listFightPrefab[i];
            Destroy(item.gameObject);
        }
        listFightPrefab.Clear();
        foreach (var itemData in dicPoolFightObj)
        {
            var queue = itemData.Value;
            while (queue.Count > 0)
            {
                var targetData = queue.Dequeue();
                targetData.Destroy(true);
            }
        }
        dicPoolFightObj.Clear();

        //倒计时清理
        listTimeCountDown.Clear();
        poolTimeCountDown.Clear();

        poolFightDropCrystalBean.Clear();
        poolAttackModeData.Clear();
        poolFightUnderAttackData.Clear();

        //丢弃所有待回收项 (对应的对象池已被清空，再回收会污染状态)
        ClearPendingRecycles();
    }

    #region 掉落水晶
    /// <summary>
    /// 获取掉落数据类
    /// </summary>
    public FightDropCrystalBean GetFightDropCrystalBean(int crystalNum, Vector3 dropPos)
    {
        if (poolFightDropCrystalBean.Count > 0)
        {
            var targetData = poolFightDropCrystalBean.Dequeue();
            targetData.crystalNum = crystalNum;
            targetData.dropPos = dropPos;
            //从池中取出时清理掉落者标记 由调用方按需重新赋值 防止脏数据污染BUFF筛选
            targetData.dropperCreatureUUId = null;
            return targetData;
        }
        return new FightDropCrystalBean(crystalNum, dropPos);
    }

    public FightDropCrystalBean GetFightDropCrystalBean(FightDropCrystalBean targetData)
    {
        FightDropCrystalBean newTargetData = GetFightDropCrystalBean(targetData.crystalNum, targetData.dropPos);
        newTargetData.lifeTime = targetData.lifeTime;
        return newTargetData;
    }

    /// <summary>
    /// 移除掉落数据类
    /// </summary>
    /// <param name="targetData"></param>
    public void RemoveFightDropCrystalBean(FightDropCrystalBean targetData)
    {
        poolFightDropCrystalBean.Enqueue(targetData);
    }

    /// <summary>
    /// 获取掉落水晶预制
    /// </summary>
    public void GetDropCrystalPrefab(Action<FightPrefabEntity> actionForComplete)
    {
        GetFightPrefabCommon(pathDropCrystalPrefab, (targetPrefab) =>
        {
            targetPrefab.pathAsstes = pathDropCrystalPrefab;
            targetPrefab.SetState(GameFightPrefabStateEnum.None);
            actionForComplete?.Invoke(targetPrefab);
        });
    }

    /// <summary>
    /// 获取掉落魔力预制
    /// </summary>
    public void GetDropMagicPrefab(Action<FightPrefabEntity> actionForComplete)
    {
        GetFightPrefabCommon(pathDropMagicPrefab, (targetPrefab) =>
        {
            targetPrefab.pathAsstes = pathDropMagicPrefab;
            targetPrefab.SetState(GameFightPrefabStateEnum.None);
            targetPrefab.valueInt = 100;
            targetPrefab.lifeTime = 30;
            actionForComplete?.Invoke(targetPrefab);
        });
    }
    #endregion

    #region 倒计时
    /// <summary>
    /// 获取一个新的倒计时
    /// </summary>
    public GameTimeCountDownBean GetNewTimeCountDown()
    {
        if (poolTimeCountDown.Count > 0)
        {
            var targetData = poolTimeCountDown.Dequeue();
            targetData.Clear();
            listTimeCountDown.Add(targetData);
            return targetData;
        }
        GameTimeCountDownBean newData = new GameTimeCountDownBean();
        listTimeCountDown.Add(newData);
        return newData;
    }

    /// <summary>
    /// 移除倒计时
    /// </summary>
    /// <param name="targetData"></param>
    public void RemoveTimeCountDown(GameTimeCountDownBean targetData)
    {
        listTimeCountDown.Remove(targetData);
        targetData.Clear();
        poolTimeCountDown.Enqueue(targetData);
    }
    #endregion

    #region  FightPrefab
    /// <summary>
    /// 获取FightPrefab
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FightPrefabEntity GetFightPrefab(string id)
    {
        for (int i = 0; i < listFightPrefab.Count; i++)
        {
            var itemFightPrefab = listFightPrefab[i];
            if (itemFightPrefab.id.Equals(id))
            {
                return itemFightPrefab;
            }
        }
        LogUtil.LogError($"GetFightPrefab 失败没有找到id_{id}");
        return null;
    }

    /// <summary>
    /// 移除攻击模组
    /// </summary>
    /// <param name="targetMode"></param>
    public void RemoveFightPrefabCommon(FightPrefabEntity targetEntity)
    {
        listFightPrefab.Remove(targetEntity);
        if (dicPoolFightObj.TryGetValue(targetEntity.pathAsstes, out Queue<FightPrefabEntity> pool))
        {
            pool.Enqueue(targetEntity);
        }
        else
        {
            Queue<FightPrefabEntity> poolNew = new Queue<FightPrefabEntity>();
            poolNew.Enqueue(targetEntity);
            dicPoolFightObj.Add(targetEntity.pathAsstes, poolNew);
        }
    }


    /// <summary>
    /// 获取战斗预制-通用
    /// </summary>
    /// <param name="assetsPath"></param>
    /// <param name="actionForComplete"></param>
    public void GetFightPrefabCommon(string assetsPath, Action<FightPrefabEntity> actionForComplete)
    {
        if (dicPoolFightObj.TryGetValue(assetsPath, out Queue<FightPrefabEntity> pool))
        {
            if (pool.Count > 0)
            {
                FightPrefabEntity targetPrefab = pool.Dequeue();
                listFightPrefab.Add(targetPrefab);

                targetPrefab.id = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
                targetPrefab.gameObject.name = targetPrefab.id;
                targetPrefab.gameObject.SetActive(true);
                actionForComplete?.Invoke(targetPrefab);
                return;
            }
        }
        GameObject objModel = GetModelForAddressablesSync(dicFightModeObj, assetsPath);
        GameObject objTarget = Instantiate(gameObject, objModel);

        FightPrefabEntity fightPrefab = new FightPrefabEntity();
        fightPrefab.id = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        objTarget.gameObject.name = fightPrefab.id;
        objTarget.gameObject.SetActive(true);
        fightPrefab.gameObject = objTarget;
        listFightPrefab.Add(fightPrefab);
        actionForComplete?.Invoke(fightPrefab);
    }
    #endregion

    #region 攻击模块
    public FightUnderAttackBean GetFightUnderAttackData()
    {
        FightUnderAttackBean targetData;
        if (poolFightUnderAttackData.Count > 0)
        {
            targetData = poolFightUnderAttackData.Dequeue();
            return targetData;
        }
        targetData = new FightUnderAttackBean();
        return targetData;
    }
    
    /// <summary>
    /// 移除攻击模组
    /// </summary>
    public void RemoveFightUnderAttackData(FightUnderAttackBean fightUnderAttackData)
    {
        fightUnderAttackData.ClearData();
        poolFightUnderAttackData.Enqueue(fightUnderAttackData);
    }

    /// <summary>
    /// 获取攻击模组数据
    /// </summary>
    /// <returns></returns>
    public AttackModeBean GetAttackModeData(long attackModeId)
    {
        AttackModeBean targetData;
        if (poolAttackModeData.Count > 0)
        {
            targetData = poolAttackModeData.Dequeue();
            targetData.InitData(attackModeId);
            return targetData;
        }
        targetData = new AttackModeBean(attackModeId);
        return targetData;
    }

    /// <summary>
    /// 移除攻击模组
    /// </summary>
    public void RemoveAttackModeData(AttackModeBean attackModeData)
    {
        attackModeData.ClearData();
        poolAttackModeData.Enqueue(attackModeData);
    }

    /// <summary>
    /// 获取攻击模组
    /// </summary>
    public void GetAttackModePrefab(long attackModeId, Action<BaseAttackMode> actionForComplete)
    {
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (dicPoolAttackModeObj.TryGetValue(attackModeInfo.id, out Queue<BaseAttackMode> pool))
        {
            if (pool.Count > 0)
            {
                BaseAttackMode targetMode = pool.Dequeue();
                targetMode.instanceId = ++attackModeInstanceCounter;
                dlAttackModePrefab.Add(targetMode.instanceId, targetMode);
                actionForComplete?.Invoke(targetMode);
                return;
            }
        }

        BaseAttackMode targetModeNew = ReflexUtil.CreateInstance<BaseAttackMode>(attackModeInfo.class_name);
        if (!attackModeInfo.prefab_name.IsNull())
        {
            GameObject objModel = GetModelForAddressablesSync(dicAttackModeObj, $"{PathInfo.AttackModePrefabPath}/{attackModeInfo.prefab_name}.prefab");
            GameObject objTarget = Instantiate(gameObject, objModel);

            targetModeNew.gameObject = objTarget;
            targetModeNew.spriteRenderer = objTarget.GetComponentInChildren<SpriteRenderer>();
        }
        targetModeNew.attackModeInfo = attackModeInfo;
        targetModeNew.instanceId = ++attackModeInstanceCounter;

        dlAttackModePrefab.Add(targetModeNew.instanceId, targetModeNew);
        actionForComplete?.Invoke(targetModeNew);
    }

    /// <summary>
    /// 移除攻击模组
    /// </summary>
    /// <param name="targetMode"></param>
    public void RemoveAttackModePrefab(BaseAttackMode targetMode)
    {
        dlAttackModePrefab.RemoveByKey(targetMode.instanceId);
        if (dicPoolAttackModeObj.TryGetValue(targetMode.attackModeInfo.id, out Queue<BaseAttackMode> pool))
        {
            pool.Enqueue(targetMode);
        }
        else
        {
            Queue<BaseAttackMode> poolNew = new Queue<BaseAttackMode>();
            poolNew.Enqueue(targetMode);
            dicPoolAttackModeObj.Add(targetMode.attackModeInfo.id, poolNew);
        }
    }

    /// <summary>
    /// 移除一个攻击模组 (默认下一帧入池，等弹道/特效本帧逻辑跑完)
    /// </summary>
    public void RemoveAttackMode(BaseAttackMode targetMode)
    {
        RemoveAttackMode(targetMode, RecycleDelay.NextFrame);
    }

    /// <summary>
    /// 移除一个攻击模组
    /// </summary>
    /// <param name="targetMode">要回收的攻击模组</param>
    /// <param name="delay">回收时机；可用 <see cref="RecycleDelay.Immediate"/> / <see cref="RecycleDelay.NextFrame"/> / <see cref="RecycleDelay.Wait(float)"/></param>
    public void RemoveAttackMode(BaseAttackMode targetMode, RecycleDelay delay)
    {
        if (targetMode == null)
            return;
        //立即标记失效，让本帧后续逻辑跳过这个攻击模组
        targetMode.isValid = false;
        ScheduleRecycle(() =>
        {
            //回收预制
            if (targetMode.gameObject != null)
            {
                targetMode.gameObject.SetActive(false);
            }
            RemoveAttackModePrefab(targetMode);
            //回收数据
            if (targetMode.attackModeData != null)
            {
                RemoveAttackModeData(targetMode.attackModeData);
                targetMode.attackModeData = null;
            }
        }, delay);
    }
    #endregion

    #region 战斗逻辑缓存
    /// <summary>
    /// 获取缓存的战斗逻辑（懒加载，战斗 Clear 时自动失效）
    /// </summary>
    public GameFightLogic GetCachedFightLogic()
    {
        if (cachedGameFightLogic == null)
        {
            cachedGameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        }
        return cachedGameFightLogic;
    }
    #endregion
}
