using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged​Split : BaseAttackMode
{
    public List<GameObject> listSplitAttackObj;
    public List<int> listSplitRoad;
    public int splitNum = 2;
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        //分裂子攻击
        CreatureSplitAttack();
        actionForAttackEnd?.Invoke(this);
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
        listSplitRoad = new List<int> { (int)startPostion.z };

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
            int targetRoadIndex = (int)startPostion.z + roadOffset;
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
                    HandleForItemMove(itemObj, roadIndex);
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
    /// 处理移动item
    /// </summary>
    public virtual void HandleForItemMove(GameObject targetObj, int targetRoad)
    {
        if (targetObj == null || !targetObj.gameObject.activeSelf)
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
                Time.deltaTime * attackModeInfo.speed_move * 2
            );
        }
        targetObj.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
        //边界判定
        bool isBound = HandleForBound(targetObj, targetRoad);
        //如果没有超出边界 则进行打击判定
        if (!isBound)
        {
            HandleForHitTarget(targetObj, targetRoad);
        }
    }

    /// <summary>
    /// 打击判定处理
    /// </summary>
    public virtual void HandleForHitTarget(GameObject targetObj, int targetRoad)
    {
        //检测是否碰撞
        GameFightCreatureEntity gameFightCreatureEntity = CheckHitTargetForSingle(targetObj.transform.position);
        if (gameFightCreatureEntity != null)
        {
            //扣血
            gameFightCreatureEntity.UnderAttack(this);
            targetObj.SetActive(false);
        }
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
