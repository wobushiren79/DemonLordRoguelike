using System.Collections.Generic;
using UnityEngine;

public static class FightCreatureSearchUtil
{
    /// <summary>
    /// 搜索离自己最近的目标
    /// </summary>
    /// <param name="selfCreature">搜索者自己：回复系索敌（RoadAllyHPOrDRNoMax）中作为兜底候选排在列表末尾（其他目标优先，无其他可治疗目标时才选中自己），其他搜索类型不使用</param>
    public static List<FightCreatureEntity> FindCreatureEntity
    (
        CreatureSearchType searchType,
        CreatureFightTypeEnum searchCreatureFightType,
        Vector3 startSearchPosition,
        Vector3 direction,
        Vector3 halfEx,
        float searchRange = 1,
        int searchRoadIndex = 1,
        FightCreatureEntity selfCreature = null
    )
    {
        int searchLayoutInfo;
        //搜索范围
        if (searchCreatureFightType == CreatureFightTypeEnum.FightDefense)
        {
            searchLayoutInfo = 1 << LayerInfo.CreatureDef;
        }
        else if (searchCreatureFightType == CreatureFightTypeEnum.FightAttack)
        {
            searchLayoutInfo = 1 << LayerInfo.CreatureAtt;
        }
        else
        {
            return null;
        }

        switch (searchType)
        {
            case CreatureSearchType.Ray:
                //射线搜索-从自己射向远处
                return FindCreatureEntityByRay(startSearchPosition, direction, searchRange, searchCreatureFightType, searchLayoutInfo);
            case CreatureSearchType.RaySelf:
                //射线搜索-从远处射向自己
                return FindCreatureEntityByRaySelf(startSearchPosition, direction, searchRange, searchCreatureFightType, searchLayoutInfo);
            case CreatureSearchType.AreaSphere:
            case CreatureSearchType.AreaBox:
            case CreatureSearchType.AreaBoxHPNoMax:
            case CreatureSearchType.AreaBoxDRNoMax:
                //范围搜索
                return FindCreatureEntityByArea(searchType, searchCreatureFightType, direction, startSearchPosition, halfEx, searchRange, searchLayoutInfo);

            case CreatureSearchType.RoadAllyHPOrDRNoMax:
                //同一路友军搜索（回复系索敌，不走物理检测；只留 生命或护甲任一不满 者，自己作为兜底候选排最后）
                return FindCreatureEntityByRoadForAlly(searchType, searchCreatureFightType, searchRoadIndex, selfCreature);

            case CreatureSearchType.DisMinByAll:
                //所有生物距离最最近的
                return FindCreatureEntityForDisByAll(startSearchPosition, searchCreatureFightType, direction, 0);
            case CreatureSearchType.DisMinByRoad:
            case CreatureSearchType.DisMinByRoadAdjacentUpDown:
                //搜索路径
                return FindCreatureEntityForDisByRoad(searchRoadIndex, searchType, searchCreatureFightType, startSearchPosition, direction, 0);

            case CreatureSearchType.DisMaxByAll:
                //所有生物距离最最远的
                return FindCreatureEntityForDisByAll(startSearchPosition, searchCreatureFightType, direction, 1);
            case CreatureSearchType.DisMaxByRoad:
            case CreatureSearchType.DisMaxByRoadAdjacentUpDown:
                //搜索路径
                return FindCreatureEntityForDisByRoad(searchRoadIndex, searchType, searchCreatureFightType, startSearchPosition, direction, 1);
        }
        return null;
    }

    #region 指定目标搜索与距离候选共用
    /// <summary>
    /// 检查指定生物是否仍命中搜索：距离遍历类型仅取消最近/最远单选，其余候选条件与原搜索一致。
    /// <para>Dis 系沿用原距离搜索口径，不使用 searchRange 数值过滤；UUID 不符时先返回 false，不执行候选搜索。</para>
    /// </summary>
    /// <param name="targetCreature">需要确认仍在搜索候选中的目标</param>
    /// <param name="snapshotUUId">锁定目标时的 creatureUUId 快照，防止实体池复用误判</param>
    public static bool CheckCreatureEntityInSearchResult
    (
        CreatureSearchType searchType,
        CreatureFightTypeEnum searchCreatureFightType,
        FightCreatureEntity targetCreature,
        string snapshotUUId,
        Vector3 startSearchPosition,
        Vector3 direction,
        Vector3 halfEx,
        float searchRange = 1,
        int searchRoadIndex = 1,
        FightCreatureEntity selfCreature = null
    )
    {
        //身份校验先于候选搜索，目标死亡、回收或 UUID 变化时无需遍历
        if (!CheckCreatureEntityIdentity(targetCreature, snapshotUUId))
            return false;
        List<FightCreatureEntity> listTargetCreature;
        bool isDisSearch;
        switch (searchType)
        {
            case CreatureSearchType.DisMinByAll:
            case CreatureSearchType.DisMaxByAll:
                listTargetCreature = GetCreatureEntityForDisByAll(searchCreatureFightType);
                isDisSearch = true;
                break;
            case CreatureSearchType.DisMinByRoad:
            case CreatureSearchType.DisMinByRoadAdjacentUpDown:
            case CreatureSearchType.DisMaxByRoad:
            case CreatureSearchType.DisMaxByRoadAdjacentUpDown:
                listTargetCreature = GetCreatureEntityForDisByRoad(searchRoadIndex, searchType, searchCreatureFightType);
                isDisSearch = true;
                break;
            default:
                //非距离搜索直接复用原完整结果，不复制射线/范围规则
                listTargetCreature = FindCreatureEntity(searchType, searchCreatureFightType, startSearchPosition, direction, halfEx, searchRange, searchRoadIndex, selfCreature);
                isDisSearch = false;
                break;
        }
        if (listTargetCreature.IsNull())
            return false;
        if (isDisSearch)
            return CheckCreatureEntityForDis(listTargetCreature, startSearchPosition, direction, targetCreature);
        for (int i = 0; i < listTargetCreature.Count; i++)
        {
            if (ReferenceEquals(listTargetCreature[i], targetCreature))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 校验跨帧目标身份：目标存活、表现对象存在且当前 UUID 与锁定快照一致。
    /// </summary>
    private static bool CheckCreatureEntityIdentity(FightCreatureEntity targetCreature, string snapshotUUId)
    {
        if (targetCreature == null || string.IsNullOrEmpty(snapshotUUId))
            return false;
        return !targetCreature.IsDead() && targetCreature.creatureObj != null
            && targetCreature.fightCreatureData?.creatureData?.creatureUUId == snapshotUUId;
    }

    /// <summary>
    /// 在距离搜索候选中检查指定目标，跳过距离计算与最近/最远排名。
    /// </summary>
    private static bool CheckCreatureEntityForDis(List<FightCreatureEntity> listCreature, Vector3 startSearchPosition, Vector3 direction, FightCreatureEntity targetCreature)
    {
        for (int i = 0; i < listCreature.Count; i++)
        {
            var itemTargetEntity = listCreature[i];
            if (ReferenceEquals(itemTargetEntity, targetCreature) && IsCreatureMatchSearchDirection(itemTargetEntity, startSearchPosition, direction))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取距离路径搜索的候选生物，与原 FindCreatureEntityForDisByRoad 共用同一路径口径。
    /// </summary>
    private static List<FightCreatureEntity> GetCreatureEntityForDisByRoad(int roadIndex, CreatureSearchType creatureSearchType, CreatureFightTypeEnum searchCreatureFightType)
    {
        var gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        List<FightCreatureEntity> listTargetCreature = null;
        if (searchCreatureFightType == CreatureFightTypeEnum.FightAttack)
        {
            if (creatureSearchType == CreatureSearchType.DisMinByRoadAdjacentUpDown)
            {
                listTargetCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(new List<int> { roadIndex - 1, roadIndex + 1 });
            }
            else
            {
                listTargetCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(roadIndex);
            }
        }
        else if (searchCreatureFightType == CreatureFightTypeEnum.FightDefense)
        {
            if (creatureSearchType == CreatureSearchType.DisMinByRoadAdjacentUpDown)
            {
                listTargetCreature = gameFightLogic.fightData.GetDefenseCreatureByRoad(new List<int> { roadIndex - 1, roadIndex + 1 });
            }
            else
            {
                listTargetCreature = gameFightLogic.fightData.GetDefenseCreatureByRoad(roadIndex);
            }
        }
        return listTargetCreature;
    }

    /// <summary>
    /// 获取全场距离搜索的候选生物，与原 FindCreatureEntityForDisByAll 共用同一数据来源。
    /// </summary>
    private static List<FightCreatureEntity> GetCreatureEntityForDisByAll(CreatureFightTypeEnum searchCreatureFightType)
    {
        var gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        DictionaryList<string, FightCreatureEntity> dicTargetCreature = null;
        if (searchCreatureFightType == CreatureFightTypeEnum.FightAttack)
        {
            dicTargetCreature = gameFightLogic.fightData.dlAttackCreatureEntity;
        }
        else if (searchCreatureFightType == CreatureFightTypeEnum.FightDefense)
        {
            dicTargetCreature = gameFightLogic.fightData.dlDefenseCreatureEntity;
        }
        return dicTargetCreature.List;
    }

    /// <summary>
    /// 判断候选生物是否存活且位于指定方向，等号边界与原距离搜索一致。
    /// </summary>
    private static bool IsCreatureMatchSearchDirection(FightCreatureEntity itemTargetEntity, Vector3 startSearchPosition, Vector3 direction)
    {
        if (itemTargetEntity == null || itemTargetEntity.IsDead())
            return false;
        var creatureObj = itemTargetEntity.creatureObj;
        if (direction.x > 0)
        {
            return creatureObj.transform.position.x >= startSearchPosition.x;
        }
        return creatureObj.transform.position.x <= startSearchPosition.x;
    }
    #endregion

    /// <summary>
    /// 找寻生物-射线
    /// </summary>
    /// <returns></returns>
    public static List<FightCreatureEntity> FindCreatureEntityByRay(Vector3 startPosition, Vector3 direction, float maxDistance, CreatureFightTypeEnum searchCreatureFightType, int layoutInfo)
    {
        //NonAlloc 射线检测：写入共享缓冲、返回命中数，避免每次 RaycastAll 分配数组造成 GC
        int hitCount = RayUtil.RayToCastAllNonAlloc(startPosition, direction, maxDistance, layoutInfo, out RaycastHit[] hits);
        if (hitCount == 0)
        {
            return null;
        }
        //循环外缓存 GameFightLogic，避免热路径下每次命中都做一次 GetGameLogic 查询
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        List<FightCreatureEntity> listData = null;
        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            string creatureId = hit.collider.gameObject.name;
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, searchCreatureFightType);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                if (listData == null)
                {
                    listData = new List<FightCreatureEntity>(hitCount);
                }
                listData.Add(targetCreature);
            }
        }
        return listData;
    }

    /// <summary>
    /// 找寻生物-射线-远处射向自己
    /// </summary>
    /// <returns></returns>
    public static List<FightCreatureEntity> FindCreatureEntityByRaySelf(Vector3 startPosition, Vector3 direction, float maxDistance, CreatureFightTypeEnum searchCreatureFightType, int layoutInfo)
    {
        Vector3 startSearchPositionForSelf;
        if (direction.x > 0)
        {
            startSearchPositionForSelf = startPosition + new Vector3(maxDistance, 0, 0);
        }
        else
        {
            startSearchPositionForSelf = startPosition + new Vector3(-maxDistance, 0, 0);
        }
        return FindCreatureEntityByRay(startSearchPositionForSelf, -direction, maxDistance, searchCreatureFightType, layoutInfo);
    }

    /// <summary>
    /// 查询范围敌人
    /// </summary>
    /// <returns></returns>
    public static List<FightCreatureEntity> FindCreatureEntityByArea(CreatureSearchType creatureSearchType, CreatureFightTypeEnum searchCreatureFightType, Vector3 direction, Vector3 startPosition, Vector3 halfEx, float radius, int layoutInfo)
    {
        Collider[] colliders = null;
        Vector3 offsetPosition;
        switch (creatureSearchType)
        {
            case CreatureSearchType.AreaSphere:
                colliders = RayUtil.OverlapToSphere(startPosition, radius, layoutInfo);
                break;
            case CreatureSearchType.AreaSphereFront:
                if (direction.x > 0)
                {
                    offsetPosition = new Vector3(radius, 0, 0);
                }
                else
                {
                    offsetPosition = new Vector3(-radius, 0, 0);
                }
                colliders = RayUtil.OverlapToSphere(startPosition + offsetPosition, radius, layoutInfo);
                break;
            case CreatureSearchType.AreaBox:
            case CreatureSearchType.AreaBoxHPNoMax:
            case CreatureSearchType.AreaBoxDRNoMax:
                colliders = RayUtil.OverlapToBox(startPosition, halfEx, layoutInfo);
                break;
            case CreatureSearchType.AreaBoxFront:
                if (direction.x > 0)
                {
                    offsetPosition = new Vector3(halfEx.x, 0, 0);
                }
                else
                {
                    offsetPosition = new Vector3(-halfEx.x, 0, 0);
                }
                colliders = RayUtil.OverlapToBox(startPosition + offsetPosition, halfEx, layoutInfo);
                break;
            default:
                return null;
        }
        if (colliders.IsNull())
        {
            return null;
        }
        //循环外缓存 GameFightLogic，避免每个 collider 都做一次 GetGameLogic 查询
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        List<FightCreatureEntity> listData = new List<FightCreatureEntity>(colliders.Length);
        for (int i = 0; i < colliders.Length; i++)
        {
            string creatureId = colliders[i].gameObject.name;
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, searchCreatureFightType);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                var fightCreatureData = targetCreature.fightCreatureData;
                switch (creatureSearchType)
                {
                    case CreatureSearchType.AreaBoxHPNoMax:
                        //不是满血（int 截断口径，见 FightCreatureBean.IsHPNotFull）
                        if (fightCreatureData.IsHPNotFull())
                        {
                            listData.Add(targetCreature);
                        }
                        break;
                    case CreatureSearchType.AreaBoxDRNoMax:
                        //不是满甲（int 截断口径，见 FightCreatureBean.IsDRNotFull）
                        if (fightCreatureData.IsDRNotFull())
                        {
                            listData.Add(targetCreature);
                        }
                        break;
                    default:
                        listData.Add(targetCreature);
                        break;
                }
            }
        }
        return listData;
    }

    /// <summary>
    /// 找寻同一路的友军生物（回复系索敌：愈之魅魔 RoadAllyHPOrDRNoMax）：
    /// 遍历搜索者所在路线的同阵营生物，筛选 存活 + 生命或护甲任一不满；
    /// 搜索者自己满足条件时作为兜底候选追加到列表末尾——其他受伤友军优先，只有没有其他可治疗目标时才会选中自己（自奶）。
    /// <para>友军阵营取搜索者实际所在阵营：攻击索敌配置 attack_search_creature_fight_type 是「要攻击的目标阵营」，
    /// 治疗不能套用（议会愈之魅魔作为进攻方时配置=防守方，按配置会治疗玩家）；selfCreature 为空时回退原参数。</para>
    /// 返回全部命中（调用方取 [0]）
    /// </summary>
    /// <param name="selfCreature">搜索者自己（不为 null 时：按自身实际阵营取友军列表、其余友军优先、自己追加到列表末尾）</param>
    public static List<FightCreatureEntity> FindCreatureEntityByRoadForAlly(CreatureSearchType creatureSearchType, CreatureFightTypeEnum searchCreatureFightType, int searchRoadIndex, FightCreatureEntity selfCreature)
    {
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (gameFightLogic == null)
            return null;
        //友军阵营按搜索者实际所在阵营取（治疗语义），搜索者缺失时回退配置值
        CreatureFightTypeEnum allyFightType = (selfCreature != null && selfCreature.fightCreatureData != null)
            ? selfCreature.fightCreatureData.creatureFightType
            : searchCreatureFightType;
        //同一路的同阵营生物列表
        List<FightCreatureEntity> listRoadCreature;
        if (allyFightType == CreatureFightTypeEnum.FightAttack)
        {
            listRoadCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(searchRoadIndex);
        }
        else if (allyFightType == CreatureFightTypeEnum.FightDefense)
        {
            listRoadCreature = gameFightLogic.fightData.GetDefenseCreatureByRoad(searchRoadIndex);
        }
        else
        {
            return null;
        }
        if (listRoadCreature.IsNull())
            return null;
        List<FightCreatureEntity> listData = null;
        //搜索者自己：满足治疗条件时先挂起，等其他友军全部收集完再追加（保证其他目标优先）
        FightCreatureEntity selfCreatureTarget = null;
        for (int i = 0; i < listRoadCreature.Count; i++)
        {
            var targetCreature = listRoadCreature[i];
            if (targetCreature == null || targetCreature.IsDead())
                continue;
            var fightCreatureData = targetCreature.fightCreatureData;
            if (fightCreatureData == null)
                continue;
            //生命或护甲任一不满（int 截断口径，见 FightCreatureBean.IsHPNotFull/IsDRNotFull）
            bool isHit = fightCreatureData.IsHPNotFull() || fightCreatureData.IsDRNotFull();
            if (!isHit)
                continue;
            //自己入列时跳过本轮，留到列表末尾兜底
            if (selfCreature != null && targetCreature == selfCreature)
            {
                selfCreatureTarget = targetCreature;
                continue;
            }
            if (listData == null)
                listData = new List<FightCreatureEntity>();
            listData.Add(targetCreature);
        }
        //自己作为最后的兜底目标：无其他可治疗友军时才能奶自己
        if (selfCreatureTarget != null)
        {
            if (listData == null)
                listData = new List<FightCreatureEntity>(1);
            listData.Add(selfCreatureTarget);
        }
        return listData;
    }

    /// <summary>
    /// 找寻最近的生物-路径遍历
    /// </summary>
    /// <returns></returns>
    public static List<FightCreatureEntity> FindCreatureEntityForDisByRoad(int roadIndex, CreatureSearchType creatureSearchType, CreatureFightTypeEnum searchCreatureFightType, Vector3 startSearchPosition, Vector3 direction, int disType)
    {
        var listTargetCreature = GetCreatureEntityForDisByRoad(roadIndex, creatureSearchType, searchCreatureFightType);
        return FindCreatureEntityForDis(listTargetCreature, startSearchPosition, direction, disType);
    }

    /// <summary>
    /// 找寻最近的生物-所有遍历
    /// </summary>
    /// <returns></returns>
    public static List<FightCreatureEntity> FindCreatureEntityForDisByAll(Vector3 startSearchPosition, CreatureFightTypeEnum searchCreatureFightType, Vector3 direction, int disType)
    {
        var listTargetCreature = GetCreatureEntityForDisByAll(searchCreatureFightType);
        return FindCreatureEntityForDis(listTargetCreature, startSearchPosition, direction, disType);
    }

    /// <summary>
    /// 查询距离最近的生物
    /// </summary>
    public static List<FightCreatureEntity> FindCreatureEntityForDis(List<FightCreatureEntity> listCreature, Vector3 startSearchPosition, Vector3 direction, int disType)
    {
        if (listCreature.IsNull())
            return null;
        float disLimit;
        //距离最近
        if (disType == 0)
        {
            disLimit = float.MaxValue;
        }
        //距离最远
        else
        {
            disLimit = float.MinValue;
        }
        FightCreatureEntity targetEntity = null;
        for (int i = 0; i < listCreature.Count; i++)
        {
            var itemTargetEntity = listCreature[i];
            if (!IsCreatureMatchSearchDirection(itemTargetEntity, startSearchPosition, direction))
                continue;
            float dis = Vector3.Distance(itemTargetEntity.creatureObj.transform.position, startSearchPosition);
            if ((disType == 0 && dis < disLimit) || (disType == 1 && dis > disLimit))
            {
                disLimit = dis;
                targetEntity = itemTargetEntity;
            }
        }

        //未找到任何目标时直接返回 null，避免再分配一个 [null] 的单元素列表（调用方 FindCreatureEntityForSinge 会再取 [0] 也得到 null）
        if (targetEntity == null)
        {
            return null;
        }
        //命中一个目标时分配容量为 1 的列表，避免 List 内部默认 4 容量造成的额外内存
        List<FightCreatureEntity> listData = new List<FightCreatureEntity>(1) { targetEntity };
        return listData;
    }

    /// <summary>
    /// 找寻最前排的N个存活生物（按 x 坐标排名选取；进攻方向约定：敌人从 +x 向 -x 推进，故 FightAttack 前排=x 最小，FightDefense 前排=x 最大）
    /// </summary>
    /// <param name="listCreature">候选列表（某阵营全场生物列表，如 fightData.dlAttackCreatureEntity.List）</param>
    /// <param name="count">选取数量（存活不足时返回实际数量）</param>
    /// <param name="excludeCreature">排除的生物（如施法者自己，可空）</param>
    /// <param name="searchCreatureFightType">阵营（决定前排方向）</param>
    /// <returns>最前排生物列表（按从前到后排序）；无存活生物时返回 null</returns>
    public static List<FightCreatureEntity> FindFrontRowCreatures(List<FightCreatureEntity> listCreature, int count, FightCreatureEntity excludeCreature, CreatureFightTypeEnum searchCreatureFightType)
    {
        if (listCreature.IsNull() || count <= 0)
            return null;
        //收集存活候选（排除指定生物）
        List<FightCreatureEntity> listCandidate = CollectAliveCandidates(listCreature, excludeCreature);
        if (listCandidate.IsNull())
            return null;
        SortByFrontRowDirection(listCandidate, searchCreatureFightType);
        //截取前N个
        if (listCandidate.Count > count)
            listCandidate.RemoveRange(count, listCandidate.Count - count);
        return listCandidate;
    }

    /// <summary>
    /// 找寻"1个最前排 + 其余随机"的N个存活生物（援护护盾用：首个固定为最前排[按阵营前排方向排序第1个]，其余 count-1 个从剩余存活生物中随机不重复抽取）
    /// </summary>
    /// <param name="listCreature">候选列表（某阵营全场生物列表，如 fightData.dlAttackCreatureEntity.List）</param>
    /// <param name="count">选取数量（首个=最前排，其余随机；存活不足时返回实际数量）</param>
    /// <param name="excludeCreature">排除的生物（如施法者自己，可空）</param>
    /// <param name="searchCreatureFightType">阵营（决定前排方向）</param>
    /// <returns>目标列表（[0]=最前排，其后随机）；无存活生物时返回 null</returns>
    public static List<FightCreatureEntity> FindFrontRowAndRandomCreatures(List<FightCreatureEntity> listCreature, int count, FightCreatureEntity excludeCreature, CreatureFightTypeEnum searchCreatureFightType)
    {
        if (listCreature.IsNull() || count <= 0)
            return null;
        //收集存活候选（排除指定生物）
        List<FightCreatureEntity> listCandidate = CollectAliveCandidates(listCreature, excludeCreature);
        if (listCandidate.IsNull())
            return null;
        SortByFrontRowDirection(listCandidate, searchCreatureFightType);
        //首个固定取最前排
        var listResult = new List<FightCreatureEntity>(count) { listCandidate[0] };
        //其余从 [1..n) 中部分洗牌随机抽取（抽完即止，不重复）
        int randomCount = Mathf.Min(count - 1, listCandidate.Count - 1);
        for (int i = 0; i < randomCount; i++)
        {
            int randomIndex = Random.Range(1 + i, listCandidate.Count);
            var itemTemp = listCandidate[1 + i];
            listCandidate[1 + i] = listCandidate[randomIndex];
            listCandidate[randomIndex] = itemTemp;
            listResult.Add(listCandidate[1 + i]);
        }
        return listResult;
    }

    /// <summary>
    /// 收集存活候选生物（排除空实体/无表现对象/已死亡/指定排除项）
    /// </summary>
    private static List<FightCreatureEntity> CollectAliveCandidates(List<FightCreatureEntity> listCreature, FightCreatureEntity excludeCreature)
    {
        List<FightCreatureEntity> listCandidate = null;
        for (int i = 0; i < listCreature.Count; i++)
        {
            var itemEntity = listCreature[i];
            if (itemEntity == null || itemEntity.creatureObj == null || itemEntity.IsDead())
                continue;
            if (excludeCreature != null && itemEntity == excludeCreature)
                continue;
            if (listCandidate == null)
                listCandidate = new List<FightCreatureEntity>();
            listCandidate.Add(itemEntity);
        }
        return listCandidate;
    }

    /// <summary>
    /// 按前排方向排序（FightAttack 前排=x 最小升序，其余前排=x 最大降序；10秒级低频调用，排序分配可接受）
    /// </summary>
    private static void SortByFrontRowDirection(List<FightCreatureEntity> listCandidate, CreatureFightTypeEnum searchCreatureFightType)
    {
        if (searchCreatureFightType == CreatureFightTypeEnum.FightAttack)
        {
            listCandidate.Sort((a, b) => a.creatureObj.transform.position.x.CompareTo(b.creatureObj.transform.position.x));
        }
        else
        {
            listCandidate.Sort((a, b) => b.creatureObj.transform.position.x.CompareTo(a.creatureObj.transform.position.x));
        }
    }

}
