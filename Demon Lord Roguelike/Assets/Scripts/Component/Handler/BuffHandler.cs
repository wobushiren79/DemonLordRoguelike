using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class BuffHandler : BaseHandler<BuffHandler, BuffManager>
{
    /// <summary>
    /// 更新数据
    /// </summary>
    public void UpdateData(float updateTime)
    {
        //战斗生物BUFF更新处理
        var fightCreatureBuffs = manager.dicFightCreatureBuffsActivie.List;
        if (fightCreatureBuffs.Count > 0)
        {
            for (int i = fightCreatureBuffs.Count - 1; i >= 0; i--)
            {
                List<BuffBaseEntity> listBuff = fightCreatureBuffs[i];
                //如果都删完了。再删除这个生物
                if (UpdateForActivieBuffs(updateTime, listBuff))
                {
                    manager.dicFightCreatureBuffsActivie.RemoveByValue(listBuff);
                }
            }
        }

        //深渊馈赠BUFF更新处理
        var abyssalBlessingBuffs = manager.dicAbyssalBlessingBuffsActivie.List;
        if (abyssalBlessingBuffs.Count > 0)
        {
            for (int i = abyssalBlessingBuffs.Count - 1; i >= 0; i--)
            {
                List<BuffBaseEntity> listBuff = abyssalBlessingBuffs[i];
                //如果都删完了。馈赠暂时不处理
                if (UpdateForActivieBuffs(updateTime, listBuff))
                {

                }
            }
        }
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    public bool UpdateForActivieBuffs(float updateTime, List<BuffBaseEntity> listBuff)
    {
        if (!listBuff.IsNull())
        {
            for (int f = listBuff.Count - 1; f >= 0; f--)
            {
                BuffBaseEntity itemBuffEntity = listBuff[f];
                //如果BUFF已经无效 则删除
                if (itemBuffEntity.buffEntityData.isValid == false)
                {
                    manager.RemoveBuffEntity(listBuff, itemBuffEntity);
                    continue;
                }
                itemBuffEntity.UpdateBuffTime(updateTime);
            }
            if (listBuff.Count == 0)
                return true;
            else
                return false;
        }
        else
            return true;
    }

    #region 深渊馈赠BUFF
    /// <summary>
    /// 增加深渊馈赠
    /// </summary>
    public void AddAbyssalBlessing(AbyssalBlessingEntityBean abyssalBlessingEntityData)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var defenseCoreCreature = gameLogic.fightData.GetCreatureById("", CreatureFightTypeEnum.FightDefenseCore);
        var defenseCoreCreatureUUID = defenseCoreCreature.fightCreatureData.creatureData.creatureUUId;
        AbyssalBlessingInfoBean abyssalBlessingInfo = abyssalBlessingEntityData.abyssalBlessingInfo;
        List<BuffBaseEntity> listBuffEntity = new List<BuffBaseEntity>();
        //创建BUFFEntity列表
        long[] buffIds = abyssalBlessingInfo.buff_ids.SplitForArrayLong(',');
        for (int i = 0; i < buffIds.Length; i++)
        {
            long buffId = buffIds[i];
            BuffInfoBean buffInfo = BuffInfoCfg.GetItemData(buffId);
            // 有等级的BUFF：替换旧的同族BUFF，并解析到正确的下一级
            if (buffInfo != null && buffInfo.buff_level > 0)
            {
                long parentId = buffInfo.buff_parent_id;
                int currentLevel = GetAbyssalBlessingCurrentLevel(parentId);
                RemoveAbyssalBlessingByParentId(parentId);
                BuffInfoBean nextLevelBuffInfo = BuffInfoCfg.GetBuffByParentAndLevel(parentId, currentLevel + 1);
                if (nextLevelBuffInfo != null)
                    buffId = nextLevelBuffInfo.id;
            }
            BuffBean buffData = new BuffBean(buffId);
            var buffEntity = manager.GetBuffEntity(buffData, defenseCoreCreatureUUID, defenseCoreCreatureUUID);
            if (buffEntity == null) continue;
            listBuffEntity.Add(buffEntity);
        }
        manager.dicAbyssalBlessingBuffsActivie.Add(abyssalBlessingEntityData, listBuffEntity);
        //事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.Buff_AbyssalBlessingChange, abyssalBlessingEntityData);
    }

    /// <summary>
    /// 获取深渊馈赠中指定父级BUFFID当前已有的等级（0表示未拥有）
    /// </summary>
    public int GetAbyssalBlessingCurrentLevel(long parentId)
    {
        var valueLists = manager.dicAbyssalBlessingBuffsActivie.List;
        for (int i = 0; i < valueLists.Count; i++)
        {
            var listBuff = valueLists[i];
            for (int f = 0; f < listBuff.Count; f++)
            {
                var buffInfo = listBuff[f].buffEntityData.GetBuffInfo();
                if (buffInfo != null && buffInfo.buff_parent_id == parentId && buffInfo.buff_level > 0)
                    return buffInfo.buff_level;
            }
        }
        return 0;
    }

    /// <summary>
    /// 移除深渊馈赠中指定父级BUFFID对应的条目（用于等级替换）
    /// </summary>
    private void RemoveAbyssalBlessingByParentId(long parentId)
    {
        List<BuffBaseEntity> targetList = null;
        var valueLists = manager.dicAbyssalBlessingBuffsActivie.List;
        for (int i = 0; i < valueLists.Count; i++)
        {
            var listBuff = valueLists[i];
            for (int f = 0; f < listBuff.Count; f++)
            {
                var buffInfo = listBuff[f].buffEntityData.GetBuffInfo();
                if (buffInfo != null && buffInfo.buff_parent_id == parentId)
                {
                    targetList = listBuff;
                    break;
                }
            }
            if (targetList != null) break;
        }
        if (targetList != null)
        {
            for (int f = targetList.Count - 1; f >= 0; f--)
            {
                targetList[f].buffEntityData.isValid = false;
                manager.RemoveBuffEntity(targetList, targetList[f]);
            }
            manager.dicAbyssalBlessingBuffsActivie.RemoveByValue(targetList);
        }
    }
    #endregion


    #region  战斗生物BUFF

    /// <summary>
    /// 移除战斗生物BUFF
    /// </summary>
    /// <remarks>
    /// 所有BUFF立即同步回收，事件订阅当场注销，避免泄漏。
    /// 注意：调用此方法前，正常死亡流程应先 TriggerEvent(GameFightLogic_CreatureDeadEnd)，
    /// 让 BuffEntityConditionalDead 有机会完成其触发逻辑；本方法不再为其保留BUFF。
    /// 若清理后 list 已空，立即从 dict 中移除，不留脏 entry。
    /// </remarks>
    public void RemoveFightCreatureBuffs(string creatureId)
    {
        List<BuffBaseEntity> listBuffBaseEntity = manager.GetFightCreatureBuffsActivie(creatureId);
        if (listBuffBaseEntity.IsNull())
        {
            return;
        }
        //反向遍历，立即同步回收所有BUFF（含已触发完毕的ConditionalDead）
        for (int i = listBuffBaseEntity.Count - 1; i >= 0; i--)
        {
            var buffEntity = listBuffBaseEntity[i];
            if (buffEntity.buffEntityData != null)
            {
                buffEntity.buffEntityData.isValid = false;
            }
            manager.RemoveBuffEntity(listBuffBaseEntity, buffEntity);
        }
        //list已空 立即从dict移除 不留脏entry
        manager.dicFightCreatureBuffsActivie.RemoveByValue(listBuffBaseEntity);
    }

    /// <summary>
    /// 移除战斗生物BUFF 指定类型
    /// </summary>
    public void RemoveFightCreatureBuffs<T>(string creatureId) where T : BuffBaseEntity
    {
        List<BuffBaseEntity> listBuffBaseEntity = manager.GetFightCreatureBuffsActivie(creatureId);
        if (listBuffBaseEntity.IsNull())
        {
            return;
        }
        for (int i = listBuffBaseEntity.Count - 1; i >= 0; i--)
        {
            var buffEntity = listBuffBaseEntity[i];
            if (buffEntity is T)
            {
                if (buffEntity.buffEntityData != null)
                {
                    buffEntity.buffEntityData.isValid = false;
                }
                manager.RemoveBuffEntity(listBuffBaseEntity, buffEntity);
            }
        }
        if (listBuffBaseEntity.Count == 0)
        {
            manager.dicFightCreatureBuffsActivie.RemoveByValue(listBuffBaseEntity);
        }
    }

    /// <summary>
    /// 添加BUFF 带有触发概率的判定
    /// </summary>
    public bool AddFightCreatureBuff(List<BuffBean> listBuffData, string applierCreatureId, string targetCreatureId)
    {
        if (listBuffData.IsNull())
        {
            return false;
        }
        //获取触发的buff 计算触发概率
        var listBuffEntityBean = CheckBuffCreate(listBuffData, applierCreatureId, targetCreatureId);
        if (!listBuffEntityBean.IsNull())
        {
            AddFightCreatureBuff(listBuffEntityBean, applierCreatureId, targetCreatureId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 添加BUFF
    /// </summary>
    public void AddFightCreatureBuff(List<BuffEntityBean> addListBuffEntityData, string applierCreatureId, string targetCreatureId)
    {
        //获取当前生物的BUFF
        List<BuffBaseEntity> listBuffEntityActivie = manager.GetFightCreatureBuffsActivie(targetCreatureId);
        //遍历需要添加的BUFF
        for (int i = 0; i < addListBuffEntityData.Count; i++)
        {
            var itemAddBuffEntityBean = addListBuffEntityData[i];
            //Instant类型BUFF：在SetData中立即触发并失效，每次添加都应独立触发，不走"刷新已有"分支
            bool isInstantBuff = IsInstantBuff(itemAddBuffEntityBean);
            //如果当前生物没有BUFF 则直接添加----------------------------------
            if (listBuffEntityActivie == null)
            {
                var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                if (buffEntity == null) continue;
                List<BuffBaseEntity> listBuffEntityNew = new List<BuffBaseEntity>() { buffEntity };
                manager.dicFightCreatureBuffsActivie.Add(targetCreatureId, listBuffEntityNew);
                listBuffEntityActivie = listBuffEntityNew;
            }
            //如果当前生物有BUFF列表 但是是空----------------------------------
            else if (listBuffEntityActivie.Count == 0)
            {
                var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                if (buffEntity == null) continue;
                listBuffEntityActivie.Add(buffEntity);
            }
            //如果当前生物有BUFF----------------------------------
            else
            {
                //查找同ID且有效的现存BUFF（Instant类型不查重，每次独立触发）
                BuffBaseEntity existingBuff = null;
                if (!isInstantBuff)
                {
                    for (int f = 0; f < listBuffEntityActivie.Count; f++)
                    {
                        var b = listBuffEntityActivie[f];
                        if (b.buffEntityData == null || b.buffEntityData.isValid == false) continue;
                        if (b.buffEntityData.buffId == itemAddBuffEntityBean.buffId)
                        {
                            existingBuff = b;
                            break;
                        }
                    }
                }
                //根据 stack_mode 决定如何合并新BUFF；返回 true 表示已被吸收，false 表示需要新增
                bool absorbed = false;
                if (existingBuff != null)
                {
                    absorbed = ApplyStackingPolicy(existingBuff, itemAddBuffEntityBean, targetCreatureId);
                }
                if (!absorbed)
                {
                    //新增分支
                    var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                    if (buffEntity == null) continue;
                    listBuffEntityActivie.Add(buffEntity);
                }
                else
                {
                    //新BUFF已合并到 existingBuff，回收entityBean
                    manager.RemoveBuffEntityBean(itemAddBuffEntityBean);
                }
            }
        }
        //发送事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.Buff_FightCreatureChange, applierCreatureId, targetCreatureId);
    }

    /// <summary>
    /// 按 BuffStackMode 决定如何合并新BUFF到已有同ID BUFF
    /// 返回 true 表示新BUFF已被现有实例吸收，调用方应回收entityBean
    /// 返回 false 表示调用方应走"新增"分支创建新实例（Independent / ReplaceStrongest命中时）
    /// </summary>
    private bool ApplyStackingPolicy(BuffBaseEntity existingBuff, BuffEntityBean incoming, string targetCreatureId)
    {
        var existingData = existingBuff.buffEntityData;
        var buffInfo = existingData.GetBuffInfo();
        switch (buffInfo.GetStackMode())
        {
            case BuffStackMode.Refresh:
                //默认：刷新次数+施加者；非次数触发型刷新计时
                existingData.triggerNumLeft = incoming.triggerNumLeft;
                existingData.applierCreatureUUId = incoming.applierCreatureUUId;
                if (existingData.GetTriggerNum() <= 0)
                {
                    existingData.timeUpdate = incoming.timeUpdate;
                }
                return true;

            case BuffStackMode.Stack:
                //叠层：层数+1，受 stack_max 限制
                int max = buffInfo.GetStackMax();
                int newStack = existingData.stackCount + 1;
                if (max > 0 && newStack > max) newStack = max;
                bool stackChanged = (newStack != existingData.stackCount);
                existingData.stackCount = newStack;
                //叠层惯例：上叠一层 = 刷新计时与剩余次数
                existingData.triggerNumLeft = incoming.triggerNumLeft;
                existingData.applierCreatureUUId = incoming.applierCreatureUUId;
                if (existingData.GetTriggerNum() <= 0)
                {
                    existingData.timeUpdate = 0;
                }
                //层数变了 通知目标生物刷新属性
                if (stackChanged)
                {
                    RefreshTargetCreatureAttribute(targetCreatureId);
                }
                return true;

            case BuffStackMode.Independent:
                //不吸收，走新增分支创建独立实例
                return false;

            case BuffStackMode.Ignore:
                //完全忽略新BUFF
                return true;

            case BuffStackMode.ReplaceStrongest:
                //新BUFF更强则替换：让旧的失效并走新增分支
                if (incoming.buffData.trigger_value > existingData.buffData.trigger_value)
                {
                    existingData.isValid = false;
                    return false;
                }
                //旧的更强 忽略新的
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 通知目标生物刷新属性（Stack 模式层数变化时调用）
    /// </summary>
    private void RefreshTargetCreatureAttribute(string targetCreatureUUId)
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null) return;
        var creature = gameFightLogic.fightData.GetCreatureById(targetCreatureUUId, CreatureFightTypeEnum.None);
        if (creature == null || creature.fightCreatureData == null || creature.IsDead()) return;
        creature.fightCreatureData.RefreshBaseAttribute();
    }

    /// <summary>
    /// 判断是否Instant类型BUFF（SetData中立即触发并失效）
    /// 委派给 BuffInfoBean.IsInstantBuffEntity()，基于 Type 继承检查（带缓存）
    /// </summary>
    private bool IsInstantBuff(BuffEntityBean buffEntityBean)
    {
        var buffInfo = buffEntityBean?.GetBuffInfo();
        return buffInfo != null && buffInfo.IsInstantBuffEntity();
    }
    #endregion

    #region 攻击时间BUFF
    /// <summary>
    /// 根据生物BUFF改变攻击时间数据
    /// </summary>
    /// <param name="creatureUUId">生物UUID</param>
    /// <param name="timeAttackPre">攻击前摇时间</param>
    /// <param name="timeAttacking">攻击动画时间</param>
    public void ChangeAttackTimeDataForBuff(string creatureUUId, ref float timeAttackPre, ref float timeAttacking)
    {
        var creatureBuffs = manager.GetFightCreatureBuffsActivie(creatureUUId);
        if (!creatureBuffs.IsNull())
        {
            for (int i = 0; i < creatureBuffs.Count; i++)
            {
                if (creatureBuffs[i] is BuffEntityAttributeAttackTime buffAttackTime)
                {
                    buffAttackTime.ChangeAttackTimeData(ref timeAttackPre, ref timeAttacking);
                }
            }
        }
    }
    #endregion

    #region 触发BUFF判定
    /// <summary>
    /// 尝试创建buff，根据创建概率看是否创建成功
    /// </summary>
    public List<BuffEntityBean> CheckBuffCreate(List<BuffBean> listBuffData, string applierCreatureId, string targetCreatureId)
    {
        List<BuffEntityBean> listData = new List<BuffEntityBean>();
        for (int i = 0; i < listBuffData.Count; i++)
        {
            var item = listBuffData[i];
            var targetEntityBean = CheckBuffCreate(item, applierCreatureId, targetCreatureId);
            //如果创建失败
            if (targetEntityBean == null)
            {
                continue;
            }
            listData.Add(targetEntityBean);
        }
        return listData;
    }

    /// <summary>
    /// 尝试创建buff，根据创建概率看是否创建成功
    /// </summary>
    public BuffEntityBean CheckBuffCreate(BuffBean buffData, string applierCreatureId, string targetCreatureId)
    {
        if (buffData.createRate > 0 && buffData.createRate < 1)
        {
            var randomRate = UnityEngine.Random.Range(0f, 1f);
            if (randomRate >= buffData.createRate)
            {
                return null;
            }
        }
        BuffEntityBean buffEntityData = manager.GetBuffEntityBean(buffData, applierCreatureId, targetCreatureId);
        return buffEntityData;
    }
    #endregion
}