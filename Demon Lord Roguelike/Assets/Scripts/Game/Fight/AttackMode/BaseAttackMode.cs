using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class BaseAttackMode
{
    #region 核心状态与标识
    /// <summary>是否有效（对象池复用/销毁时的存活标记）</summary>
    public bool isValid = true;
    /// <summary>实例ID（由 FightManager 分配，用于 DictionaryList 快速移除）</summary>
    public long instanceId;
    /// <summary>当前 obj（预制字段保留：DSP 迁移过渡期仍作渲染/兼容载体，位置真实源已改为 position）</summary>
    public GameObject gameObject;
    /// <summary>
    /// 弹道当前世界坐标（DSP 方案B 位置权威源，脱离 transform）
    /// <para>gameObject 非空时同步写回其 transform，供 AttackModeInstanceRenderer 批量绘制读取。</para>
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// 上一帧渲染时的位置：供 AttackModeInstanceRenderer 帧差分算世界速度矢量(灌 shader 逐实例 _VelocityWS)，使火星等特效脱离弹体留在世界空间。
    /// <para>纯运行时字段(非配置参数)，仅"桶材质带 _VelocityWS"的弹道会被读写；Init 里与 position 同步初始化，防对象池复用时残留上一发位置算出跨屏速度。</para>
    /// </summary>
    public Vector3 lastRenderPosition;
    #endregion

    #region DSP 视觉参数（由武器 attack_mode_data 写入，供 AttackModeInstanceRenderer 构建实例矩阵/写桶材质；spriteRenderer 渲染并行生效）
    /// <summary>缩放（武器 StartSize）；-1=未配置，DSP 渲染时矩阵取1、大小改由桶材质自身 _VertexScale 决定</summary>
    public float visualScale = -1f;
    /// <summary>起始旋转角（武器 StartRotate，度，绕视图前向）</summary>
    public float visualStartAngle = 0f;
    /// <summary>自旋速度（武器 VertexRotateSpeed，度/秒；DSP 侧写入桶材质 _RotateSpeed 由 shader 按 _Time 自转）</summary>
    public float spinSpeed = 0f;
    /// <summary>自旋轴（武器 VertexRotateAxis；与 spinSpeed 相乘得每轴度/秒写入材质）</summary>
    public Vector3 spinAxis = Vector3.one;
    /// <summary>per-instance 随机相位（度，发射时随机）：材质自转全桶同相，靠此静态角让每发起始角度错开</summary>
    public float spinPhase = 0f;
    /// <summary>换图名（武器 ShowSprite，解析自 attack_mode_data）；非空表示该弹道用图集内指定 sprite 换皮，参与 DSP 子桶分桶</summary>
    public string visualSpriteName;
    /// <summary>DSP 视觉桶签名（visual_name + 换图 + 自旋 组合，InitAttackModeShow 末尾算出并缓存）；RenderAll 按它取桶，空=不走 DSP</summary>
    public string visualBucketKey;
    #endregion

    #region 渲染与配置数据
    /// <summary>sprite 渲染器（不一定有）</summary>
    public SpriteRenderer spriteRenderer;
    /// <summary>攻击模块信息（配置表 AttackModeInfoBean）</summary>
    public AttackModeInfoBean attackModeInfo;
    /// <summary>攻击模块数据（运行时 AttackModeBean）</summary>
    public AttackModeBean attackModeData;
    /// <summary>攻击搜索的生物战斗类型（由 attackedLayerTarget 推导，StartAttack 时缓存，避免每帧重算）</summary>
    protected CreatureFightTypeEnum searchCreatureType = CreatureFightTypeEnum.None;
    /// <summary>本帧射线批处理命令索引（>=0 表示本帧已入队射线，检测时读批处理结果；-1 表示未入队，走 live 路径）</summary>
    protected int batchRayStart = -1;
    #endregion

    #region 数值常量（策划调整入口）
    /// <summary>攻速 ASPD=100 时弹道飞行速度的最大加成倍率</summary>
    public const float SpeedRateASPDMax = 3f;
    /// <summary>弹道起点 Y 轴随机扰动幅度（±值，避免弹道起点完全重合）</summary>
    public const float StartPosRandomRangeY = 0.05f;
    #endregion

    /// <summary>
    /// 初始化攻击样式
    /// </summary>
    public virtual void InitAttackModeShow()
    {
        //先把 DSP per-instance 视觉参数还原为默认(对象池复用不残留上一发武器视觉；武器分支随后按需覆盖)
        ResetVisualParams();
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
        //登记 DSP 视觉桶(按 visual_name + 换图 + 自旋 分桶；异步换图就绪后才登记桶，未就绪本帧先不画)
        FightHandler.Instance.manager.EnsureAttackModeVisual(this);
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
        //按目标层级推导并缓存搜索类型（两条发射路径都要，故放在这里而非 StartAttack(attacker,...) 里）
        RefreshSearchCreatureType();
        //每发随机一个自旋相位，避免同桶所有实例(材质统一自转)角度完全同步
        spinPhase = UnityEngine.Random.Range(0f, 360f);
        //位置真实源置为起点（即使无 gameObject 也生效），再同步到 transform
        position = attackModeData.startPos;
        //渲染差分基准同步归位：不重置则对象池复用时会拿上一发的终点作基准，首帧算出跨屏速度把火星甩到天边
        lastRenderPosition = position;
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
                    //获取被攻击者的层级（搜索类型由末尾 StartAttackBase → RefreshSearchCreatureType 据此推导）
                    attackModeData.attackedLayerTarget = attacker.fightCreatureData.GetCreatureLayer(true);
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

    /// <summary>
    /// 还原 DSP per-instance 视觉参数为默认值(缩放-1即未配置/起始角0/无自旋)，供对象池复用时不残留上一发武器视觉。
    /// <para>默认值与 HandleItemsInfoAttackModeData 里"还原预制"一致(自旋轴默认 Vector3.one)；visualScale=-1 表示未配置，大小由桶材质自身 _VertexScale 决定。</para>
    /// </summary>
    public void ResetVisualParams()
    {
        visualScale = -1f;
        visualStartAngle = 0f;
        spinSpeed = 0f;
        spinAxis = Vector3.one;
        visualSpriteName = null;
        //拖尾默认关闭：对象池复用不残留上一发拖尾，由 EnableTrail 按配置重新开启
        trailMode = AttackModeTrailType.None;
        //拖尾染色复位为白(不改贴图原色)，避免对象池复用残留上一发颜色
        trailColor = Vector3.one;
    }
    #endregion

    #region 拖尾历史(轨迹：环形缓冲按 interval 间隔记录最近位置+自旋角，供 AttackModeInstanceRenderer 逐年龄档批量绘制轨迹)
    /// <summary>拖尾历史环形缓冲最大点数（轨迹段数上限；轨迹 count 会被 clamp 到此值）</summary>
    public const int TrailMaxPoints = 32;
    /// <summary>本发拖尾模式（由 EnableTrail 按配置 type 设；对象池复用默认 None）。None=不启用；Instanced=方案1(CPU 环形缓冲+DrawMeshInstanced 年龄档)；Vfx=方案2(不采样，每帧位置由渲染器读 position 喂 VFX 特效)。三态互斥，取代旧的 trailEnabled/trailVfxEnabled 双 bool</summary>
    public AttackModeTrailType trailMode = AttackModeTrailType.None;
    /// <summary>本发拖尾染色 rgb（=自身 trail_data 的 color；方案2 逐弹上传 ColorBuffer 实现"同一 VFX 内每发子弹各自颜色"，故必须逐弹持有而非取桶级配置）。默认白=不改贴图原色</summary>
    public Vector3 trailColor = Vector3.one;
    /// <summary>环形位置缓冲（懒分配 TrailMaxPoints，仅启用拖尾的弹道分配一次、跨复用保留）</summary>
    public Vector3[] trailPoints;
    /// <summary>环形自旋角缓冲（与 trailPoints 一一对应，记录采样时刻弹体的时间自转角，供轨迹复现旋转姿态；无自旋时恒 0）</summary>
    public float[] trailSpinAngles;
    /// <summary>已填充点数（≤TrailMaxPoints）</summary>
    public int trailCount;
    /// <summary>环形写指针（下一个写入下标）</summary>
    public int trailHead;
    /// <summary>上次采样时刻（秒），控制按 trailSampleInterval 间隔采样</summary>
    public float trailLastSampleTime;
    /// <summary>采样间隔（=config.interval，相邻轨迹的时间间距）</summary>
    public float trailSampleInterval;

    /// <summary>
    /// 启用拖尾并按配置初始化：懒分配环形缓冲(位置+自旋角)、取采样间隔、清空历史；count/interval≤0 的配置视为不启用。
    /// <para>在 InitAttackModeShow 末尾（EnsureAttackModeVisual 时）按 attackModeInfo.GetTrailConfig() 调用；无 visual_name 不走 DSP 时不应调用。</para>
    /// </summary>
    public void EnableTrail(AttackModeTrailConfig config)
    {
        if (!config.enable)
        {
            trailMode = AttackModeTrailType.None;
            return;
        }
        //逐弹记下自身配置的拖尾染色：方案2 靠它实现同一 VFX 内每发子弹各自颜色(方案1 用桶级 baseColor，此值不参与)
        trailColor = new Vector3(config.color.r, config.color.g, config.color.b);
        //方案2(VFX)：无需 CPU 环形历史缓冲，仅标记参与 VFX 轨迹；每帧位置由渲染器直接读 position 喂 VFX 特效
        if (config.type == AttackModeTrailType.Vfx)
        {
            trailMode = AttackModeTrailType.Vfx;
            return;
        }
        //方案1(Instanced)：CPU 环形缓冲按 interval 采样历史位置，供渲染器逐年龄档批量绘制
        trailMode = AttackModeTrailType.Instanced;
        if (trailPoints == null)
            trailPoints = new Vector3[TrailMaxPoints];
        if (trailSpinAngles == null)
            trailSpinAngles = new float[TrailMaxPoints];
        //采样间隔 = 配置 interval（相邻轨迹时间间距；下限避免除零/过密）
        trailSampleInterval = Mathf.Max(config.interval, 0.001f);
        ResetTrail();
    }

    /// <summary>
    /// 清空拖尾历史（对象池复用/重新发射时调，避免上一段轨迹残留）。
    /// </summary>
    public void ResetTrail()
    {
        trailCount = 0;
        trailHead = 0;
        //置为极小值保证下一次 SampleTrail 必采首点
        trailLastSampleTime = float.NegativeInfinity;
    }

    /// <summary>
    /// 按采样间隔把当前 position 与当前时间自转角(spinSpeed×now，绕 spinAxis)记入环形缓冲（渲染器每帧对启用拖尾的弹道调用；未到间隔则跳过）。
    /// </summary>
    /// <param name="now">当前时刻（秒，须与 shader 时间自转同基准，即 Time.timeSinceLevelLoad≈_Time.y，使轨迹旋转与弹体连续）</param>
    public void SampleTrail(float now)
    {
        if (trailMode != AttackModeTrailType.Instanced || trailPoints == null)
            return;
        if (now - trailLastSampleTime < trailSampleInterval)
            return;
        trailLastSampleTime = now;
        trailPoints[trailHead] = position;
        //记录采样时刻的时间自转角(与桶材质 shader 同式：绕 spinAxis 转 spinSpeed×now 度)；无自旋恒 0
        trailSpinAngles[trailHead] = spinSpeed * now;
        trailHead = (trailHead + 1) % TrailMaxPoints;
        if (trailCount < TrailMaxPoints)
            trailCount++;
    }

    /// <summary>
    /// 按时间顺序取第 orderIndex 个历史点（0=最老，trailCount-1=最新）。
    /// </summary>
    public Vector3 GetTrailPoint(int orderIndex)
    {
        return trailPoints[GetTrailRingIndex(orderIndex)];
    }

    /// <summary>
    /// 按时间顺序取第 orderIndex 个历史点采样时的时间自转角（度，绕 spinAxis）；供轨迹复现当时旋转姿态。
    /// </summary>
    public float GetTrailSpinAngle(int orderIndex)
    {
        return trailSpinAngles[GetTrailRingIndex(orderIndex)];
    }

    /// <summary>
    /// 一次取回第 orderIndex 个历史点的位置与采样时自转角：两者同下标，合并取用只算一次环形下标换算(热路径每帧×档数×弹道数调用，故避免 GetTrailPoint+GetTrailSpinAngle 各算一遍取模)。
    /// </summary>
    public void GetTrailSample(int orderIndex, out Vector3 point, out float spinAngle)
    {
        int ringIndex = GetTrailRingIndex(orderIndex);
        point = trailPoints[ringIndex];
        spinAngle = trailSpinAngles[ringIndex];
    }

    /// <summary>
    /// 把时间顺序下标(0=最老)换算成环形缓冲物理下标（最老点 = 写指针回退 trailCount）。
    /// </summary>
    private int GetTrailRingIndex(int orderIndex)
    {
        int start = (trailHead - trailCount + TrailMaxPoints) % TrailMaxPoints;
        return (start + orderIndex) % TrailMaxPoints;
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
    /// 按 attackModeData.attackedLayerTarget 推导并缓存被攻击者战斗类型（用于射线/范围检测筛选 layer），避免每帧重算
    /// <para>⚠️必须在 StartAttackBase 里调，使「生物对战」(StartAttack(attacker,...)) 与「纯数据发射」(StartAttack()) 两条路径都能拿到：
    /// 后者是分裂弹子弹道等由发射器创建的弹道走的路径，漏了它会导致 GetSearchLayerMask 返回 0、射线不入队、永远打不到人。</para>
    /// </summary>
    protected void RefreshSearchCreatureType()
    {
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

    /// <summary>
    /// 获取射线检测的层级掩码（由 StartAttackBase 缓存的 searchCreatureType 推导）
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
