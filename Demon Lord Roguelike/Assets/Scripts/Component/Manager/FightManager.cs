using System;
using System.Collections.Generic;
using UnityEngine;

public class FightManager : BaseManager
{
    //攻击预制obj
    public Dictionary<string, GameObject> dicAttackModeObj = new Dictionary<string, GameObject>();
    //攻击模块视觉预制obj(DSP 批量渲染的 mesh+material 载体)，与 dicAttackModeObj 同为持久资源缓存(跨战斗不释放)
    public Dictionary<string, GameObject> dicAttackModeVisualObj = new Dictionary<string, GameObject>();
    //攻击预制的缓存池
    public Dictionary<long, Queue<BaseAttackMode>> dicPoolAttackModeObj = new Dictionary<long, Queue<BaseAttackMode>>();
    //攻击预制列表（用 DictionaryList 便于按 instanceId 快速 Remove 同时保留可遍历 List）
    public DictionaryList<long, BaseAttackMode> dlAttackModePrefab = new DictionaryList<long, BaseAttackMode>();
    //攻击模块实例ID自增计数器
    private long attackModeInstanceCounter = 0;
    //攻击模块射线检测的批量调度器(RaycastCommand)，替代每个弹道各自 Physics.RaycastAll
    public FightRaycastBatch raycastBatch = new FightRaycastBatch();
    //攻击模块(弹道)GPU Instancing 批量渲染器(DSP 式"记录位置一起绘制")，按 visual_name 分桶；常开但 visual_name 空/未注册桶零副作用
    public AttackModeInstanceRenderer attackModeInstanceRenderer = new AttackModeInstanceRenderer();

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
        //释放攻击模块 Addressables 资源缓存(弹道预制 + DSP 视觉预制)并清空视觉桶(实例已在上面销毁，此处释放源资源句柄)
        ClearAttackModeAssetCache();

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

        //释放射线批处理的 NativeArray(下一场战斗首次入队时按需重新分配)
        raycastBatch.Dispose();

        //丢弃所有待回收项 (对应的对象池已被清空，再回收会污染状态)
        ClearPendingRecycles();
    }

    /// <summary>
    /// 释放攻击模块相关的 Addressables 资源缓存(弹道预制 dicAttackModeObj + DSP 视觉预制 dicAttackModeVisualObj)并清空视觉桶。
    /// <para>仅在整场战斗结束(ClearGame→Clear，已打完所有关卡)时随 Clear 调用；关卡间的 ClearGameForSimple/ClearAttackModePrefab 不释放，保留缓存供下关复用。</para>
    /// <para>调用前须先销毁由这些预制实例化出的对象(dlAttackModePrefab/dicPoolAttackModeObj)，再释放源资源句柄，避免释放后仍有实例引用。</para>
    /// </summary>
    private void ClearAttackModeAssetCache()
    {
        //释放弹道预制资源句柄
        foreach (var itemData in dicAttackModeObj)
        {
            if (itemData.Value != null)
                LoadAddressablesUtil.Release(itemData.Value);
        }
        dicAttackModeObj.Clear();
        //释放 DSP 视觉预制资源句柄
        foreach (var itemData in dicAttackModeVisualObj)
        {
            if (itemData.Value != null)
                LoadAddressablesUtil.Release(itemData.Value);
        }
        dicAttackModeVisualObj.Clear();
        //清空视觉桶(其持有的 sharedMesh/sharedMaterial 引用来自上面已释放的预制)
        attackModeInstanceRenderer.ClearVisuals();
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
    /// 确保某攻击模块的 DSP 视觉桶已注册：visual_name 非空且未注册时，按 AttackModeVisualPath 懒加载视觉预制
    /// (内含 MeshFilter+MeshRenderer)，提取 sharedMesh/sharedMaterial 登记到 attackModeInstanceRenderer。
    /// <para>视觉预制与弹道 prefab 同为持久资源缓存(dicAttackModeVisualObj，跨战斗不释放)，故只加载一次、后续复用。</para>
    /// </summary>
    public void EnsureAttackModeVisual(AttackModeInfoBean attackModeInfo)
    {
        if (attackModeInfo == null || attackModeInfo.visual_name.IsNull())
            return;
        //已注册则跳过，避免重复加载(懒注册去重)
        if (attackModeInstanceRenderer.HasVisual(attackModeInfo.visual_name))
            return;
        GameObject visualPrefab = GetModelForAddressablesSync(dicAttackModeVisualObj, $"{PathInfo.AttackModeVisualPath}/{attackModeInfo.visual_name}.prefab");
        if (visualPrefab == null)
            return;
        var meshFilter = visualPrefab.GetComponentInChildren<MeshFilter>();
        var meshRenderer = visualPrefab.GetComponentInChildren<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null)
        {
            LogUtil.LogError($"AttackModeVisual 预制缺少 MeshFilter/MeshRenderer: {attackModeInfo.visual_name}");
            return;
        }
        //取 sharedMesh/sharedMaterial(勿用 .mesh/.material，否则复制副本破坏 instancing 合批)
        attackModeInstanceRenderer.RegisterVisual(attackModeInfo.visual_name, meshFilter.sharedMesh, meshRenderer.sharedMaterial);
    }

    /// <summary>
    /// 获取攻击模组
    /// </summary>
    public void GetAttackModePrefab(long attackModeId, Action<BaseAttackMode> actionForComplete)
    {
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        //播放攻击模块创建起始音效（sound_start 默认 0 表示不播放）
        if (attackModeInfo.sound_start != 0)
        {
            AudioHandler.Instance.PlaySound(attackModeInfo.sound_start);
        }
        //懒注册 DSP 视觉桶(visual_name 非空且未注册时加载视觉预制并登记，一次加载后续复用)
        EnsureAttackModeVisual(attackModeInfo);
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
