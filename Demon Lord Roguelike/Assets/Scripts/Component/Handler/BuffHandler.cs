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
                //判断一下是否有相同的buff
                bool hadBuff = false;
                //Instant类型每次都独立触发，不查重
                if (!isInstantBuff)
                {
                    for (int f = 0; f < listBuffEntityActivie.Count; f++)
                    {
                        var buffEntityActivie = listBuffEntityActivie[f];
                        //已失效的BUFF视为不存在 让本次添加按新增处理
                        if (buffEntityActivie.buffEntityData == null || buffEntityActivie.buffEntityData.isValid == false)
                        {
                            continue;
                        }
                        //如果有相同的BUFF 则只是刷新时间和次数
                        if (buffEntityActivie.buffEntityData.buffId == itemAddBuffEntityBean.buffId)
                        {
                            hadBuff = true;
                            buffEntityActivie.buffEntityData.triggerNumLeft = itemAddBuffEntityBean.triggerNumLeft;
                            buffEntityActivie.buffEntityData.applierCreatureUUId = itemAddBuffEntityBean.applierCreatureUUId;
                            //如果不是次数触发型 则刷新时间
                            int triggerNum = buffEntityActivie.buffEntityData.GetTriggerNum();
                            if (triggerNum <= 0)
                            {
                                buffEntityActivie.buffEntityData.timeUpdate = itemAddBuffEntityBean.timeUpdate;
                            }
                            break;
                        }
                    }
                }
                //如果没有相同的buff 则添加一个新的
                if (!hadBuff)
                {
                    var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                    if (buffEntity == null) continue;
                    listBuffEntityActivie.Add(buffEntity);
                }
                else
                {
                    //移除数据到缓存
                    manager.RemoveBuffEntityBean(itemAddBuffEntityBean);
                }
            }
        }
        //发送事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.Buff_FightCreatureChange, applierCreatureId, targetCreatureId);
    }

    /// <summary>
    /// 判断是否Instant类型BUFF（SetData中立即触发并失效）
    /// </summary>
    private bool IsInstantBuff(BuffEntityBean buffEntityBean)
    {
        var buffInfo = buffEntityBean?.GetBuffInfo();
        if (buffInfo == null || buffInfo.class_entity.IsNull())
        {
            return false;
        }
        return buffInfo.class_entity.StartsWith("BuffEntityInstant");
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