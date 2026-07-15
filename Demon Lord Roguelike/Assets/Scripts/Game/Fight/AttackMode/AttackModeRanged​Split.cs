using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged​Split : BaseAttackMode
{
    public List<GameObject> listSplitAttackObj;
    public List<int> listSplitRoad;
    public int splitNum = 2;
    //各子弹本帧的射线批处理命令索引（与 listSplitAttackObj 下标对齐，-1 表示该子弹本帧未入队）
    private List<int> listBatchRayIndex;

    public override void StartAttack()
    {
        base.StartAttack();
        //分裂子攻击
        CreatureSplitAttack();
    }

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        //分裂子攻击
        CreatureSplitAttack();
        actionForAttackEnd?.Invoke(this);
    }

    /// <summary>
    /// 收集本帧射线检测请求：为每个活跃子弹在其当前位置各入队一条射线（下标与 listSplitAttackObj 对齐）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        if (listBatchRayIndex == null)
            listBatchRayIndex = new List<int>();
        listBatchRayIndex.Clear();
        if (listSplitAttackObj == null)
            return;
        int layerMask = GetSearchLayerMask();
        CreatureSearchType searchType = attackModeInfo.GetCreatureSerachType();
        bool isRay = searchType == CreatureSearchType.Ray || searchType == CreatureSearchType.RaySelf;
        float dist = attackModeInfo.collider_size;
        for (int i = 0; i < listSplitAttackObj.Count; i++)
        {
            var itemObj = listSplitAttackObj[i];
            if (!isRay || layerMask == 0 || itemObj == null || !itemObj.activeSelf)
            {
                listBatchRayIndex.Add(-1);
                continue;
            }
            Vector3 pos = itemObj.transform.position;
            Vector3 dir = attackModeData.attackDirection;
            if (searchType == CreatureSearchType.RaySelf)
            {
                pos += dir.x > 0 ? new Vector3(dist, 0, 0) : new Vector3(-dist, 0, 0);
                dir = -dir;
            }
            if (dir == Vector3.zero)
            {
                listBatchRayIndex.Add(-1);
                continue;
            }
            listBatchRayIndex.Add(batch.Enqueue(pos, dir.normalized, dist, layerMask));
        }
    }

    public override void Update()
    {
        base.Update();
        HandleForMoveUpdate();
    }

    /// <summary>
    /// 删除
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        //如果是永久删除
        if (isPermanently)
        {
            if (!listSplitAttackObj.IsNull())
            {
                listSplitAttackObj.ForEach((index, itemData) =>
                {
                    GameObject.Destroy(itemData.gameObject);
                });
            }
        }
        //如果不是永久删除 就隐藏
        else
        {
            if (!listSplitAttackObj.IsNull())
            {
                listSplitAttackObj.ForEach((index, itemData) =>
                {
                    itemData.gameObject.SetActive(false);
                });
            }
        }
        base.Destroy(isPermanently);
    }

    #region  创建
    /// <summary>
    /// 创建分裂攻击
    /// </summary>
    public void CreatureSplitAttack()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (listSplitAttackObj == null)
        {
            listSplitAttackObj = new List<GameObject>() { gameObject };
        }
        listSplitRoad = new List<int> { (int)attackModeData.startPos.z };

        List<GameObject> listNewSplitAttackObj = new List<GameObject>();
        for (int i = 0; i < splitNum; i++)
        {
            //添加目标路径
            // 计算当前值：i/2 + 1，然后根据奇偶决定正负
            int roadOffset = (i / 2) + 1;
            if (i % 2 == 1) // 奇数索引为负
            {
                roadOffset = -roadOffset;
            }
            int targetRoadIndex = (int)attackModeData.startPos.z + roadOffset;
            if (targetRoadIndex <= gameFightLogic.fightData.sceneRoadNum && targetRoadIndex >= 1)
            {
                listSplitRoad.Add(targetRoadIndex);
            }
            //如果没有这条道路 则不生成子弹道
            else
            {
                continue;
            }

            //生成子弹道
            GameObject itemObj;
            if (listSplitRoad.Count <= listSplitAttackObj.Count)
            {
                itemObj = listSplitAttackObj[listSplitRoad.Count - 1];
            }
            else
            {
                itemObj = gameObject.Instantiate(gameObject.transform.parent);
                listNewSplitAttackObj.Add(itemObj);
            }
            itemObj.SetActive(true);
            itemObj.transform.position = gameObject.transform.position;
            itemObj.transform.eulerAngles = gameObject.transform.eulerAngles;
        }
        if (!listNewSplitAttackObj.IsNull())
        {
            listSplitAttackObj.AddRange(listNewSplitAttackObj);
        }
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 处理移动
    /// </summary>
    public virtual void HandleForMoveUpdate()
    {
        if (!listSplitAttackObj.IsNull())
        {
            bool isAllDestory = true;
            for (int i = 0; i < listSplitAttackObj.Count; i++)
            {
                var itemObj = listSplitAttackObj[i];
                if (itemObj.gameObject.activeSelf)
                {
                    int roadIndex =  listSplitRoad[i];
                    HandleForItemMove(itemObj, roadIndex, i);
                    isAllDestory = false;
                }
            }
            if (isAllDestory)
            {
                Destroy();
            }
        }
    }

    /// <summary>
    /// 处理移动item（先做命中判定，再移动、判边界）
    /// </summary>
    /// <param name="subIndex">子弹在 listSplitAttackObj 中的下标，用于取该子弹本帧的射线批处理结果</param>
    public virtual void HandleForItemMove(GameObject targetObj, int targetRoad, int subIndex)
    {
        if (targetObj == null || !targetObj.gameObject.activeSelf)
        {
            return;
        }
        //先做命中判定（读批处理结果，检测在移动前位置），命中即回收该子弹
        if (HandleForHitTarget(targetObj, subIndex))
        {
            return;
        }
        //如果还没到目标路径  需要望目标位置偏移
        if (Math.Abs(targetObj.transform.position.z - targetRoad) > 0.02f)
        {
            targetObj.transform.position = Vector3.MoveTowards
            (
                targetObj.transform.position,
                new Vector3(targetObj.transform.position.x, targetObj.transform.position.y, targetRoad),
                Time.deltaTime * GetMoveSpeed() * 2
            );
        }
        targetObj.transform.Translate(attackModeData.attackDirection * Time.deltaTime * GetMoveSpeed());
        //边界判定
        HandleForBound(targetObj, targetRoad);
    }

    /// <summary>
    /// 打击判定处理：读该子弹本帧的射线批处理结果，命中则扣血并回收该子弹
    /// </summary>
    /// <returns>是否命中并回收了该子弹</returns>
    public virtual bool HandleForHitTarget(GameObject targetObj, int subIndex)
    {
        int slot = (listBatchRayIndex != null && subIndex < listBatchRayIndex.Count) ? listBatchRayIndex[subIndex] : -1;
        FightCreatureEntity fightCreatureEntity = ResolveFirstAliveFromBatch(slot);
        if (fightCreatureEntity != null)
        {
            //扣血
            fightCreatureEntity.UnderAttack(this);
            targetObj.SetActive(false);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 边界处理 飞太远的情况
    /// </summary>
    public virtual bool HandleForBound(GameObject targetObj, int targetRoad)
    {
        if (CheckIsMoveBound(targetObj))
        {
            targetObj.SetActive(false);
            return true;
        }
        return false;
    }
    #endregion
}
