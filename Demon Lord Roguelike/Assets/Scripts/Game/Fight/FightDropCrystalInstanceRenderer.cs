using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 战斗掉落魔晶 GPU Instancing 批量渲染器（DSP 式：与 <see cref="FightTextInstanceRenderer"/>/<see cref="AttackModeInstanceRenderer"/> 同思路——"只记槽位，一起绘制"）。
/// <para>【核心】不再每颗魔晶一个 GameObject+BoxCollider+SpriteRenderer+DOTween，而是每颗魔晶一个纯数据槽：
/// 掉落抛物线/拾取飞回由 CPU 参数化求值（精确复刻旧 DOJump 曲线），待机上下浮动由 shader 时间驱动，
/// 每帧全屏魔晶一次或多次 Graphics.DrawMeshInstanced 画完（≤1023 单批、超出满批即绘收尾补绘，不设数量上限）。</para>
/// <para>【零 GameObject】拾取检测不走物理：鼠标点选走「魔晶到鼠标射线的世界距离」判定，生物范围拾取走世界距离判定，魔王自动拾取直接按槽序 FIFO。</para>
/// <para>【静态矩阵优化】魔晶落地(Landed)后位置恒定(浮动在 shader 里)，槽集无变化帧跳过填充与上传，直接复用上次缓冲绘制(同 FightTextInstanceRenderer 的 dirty 思路)；
/// 槽内不缓存矩阵——填充时由 currentPos 现算(只含平移+缩放,直写 8 个 float,比每槽缓存 64B 矩阵更省内存与复制带宽)。</para>
/// <para>【寿命与移除】落地时把寿命换算成「渲染器时钟(游戏速度时间累计)」上的到期时间戳，Landed 态每帧只读比较、到期/到账打删除标记，帧末一次性稳定压缩移除
/// (替代逐颗 RemoveAt 的 O(N²) 反复搬运；压缩保序以维持 FIFO 拾取语义)。飞回到账不即时回调——先记账到 arrivedNumBuffer，帧末压缩后统一派发，
    /// 避免回调在槽遍历中途重入 Clear/Add 破坏迭代(派发时重入亦安全：Clear 清空缓冲后派发循环按动态 Count 自然终止)。</para>
/// <para>【渲染资源】Setup 注册 Quad mesh + DropCrystalInstanced1 材质；材质 _BaseMap 引用 Crystal_1 独立贴图(不经图集，规避 atlas 子 UV 问题)，
/// 世界尺寸按贴图像素/100PPU×0.7 换算(对齐旧预制 SpriteRenderer 表现)。未 Setup 时一切接口零副作用(装配失败会在 EnsureDropCrystalVisual 报错)。</para>
/// </summary>
public class FightDropCrystalInstanceRenderer
{
    #region 常量
    //Graphics.DrawMeshInstanced 单批矩阵上限(Unity 硬限制 1023)；超过则满批即绘、收尾补绘，不设总数上限
    private const int MaxInstancesPerBatch = 1023;
    //掉落抛物线参数(对齐旧 DOTween DOJump)：跳高 0.3、2 跳、0.8 秒、Linear 缓动
    private const float DropJumpHeight = 0.3f;
    private const int DropJumpCount = 2;
    private const float DropJumpDuration = 0.8f;
    //拾取飞回参数(对齐旧逻辑)：飞行速度 5 单位/秒(时长=距离/5)、1 跳、OutCubic 缓动、跳力随机 0~0.5
    private const float FlyBackMoveSpeed = 5f;
    //点击拾取的世界半径(对齐旧 BoxCollider 0.4 的命中手感，略放大到 0.25 提升点按体验)
    private const float PickRadiusWorld = 0.25f;
    //生物范围拾取时魔晶自身的等效半径(对齐旧 BoxCollider 0.4 半宽)
    private const float CrystalBodyRadius = 0.2f;
    //魔晶贴图的 PPU(Crystal_1.png meta 的 spritePixelsToUnits=100)；世界尺寸=贴图像素/PPU×旧预制缩放 0.7
    private const float SpritePixelsPerUnit = 100f;
    private const float SpriteWorldScale = 0.7f;
    #endregion

    #region 内部结构：魔晶槽
    /// <summary>
    /// 魔晶槽状态机：Dropping(掉落抛物线,不可拾取) → Landed(落地,可拾取,到期时间戳判定) → FlyBack(飞回核心,不可拾取) → 到账移除。
    /// </summary>
    public enum SlotStateEnum
    {
        Dropping,
        Landed,
        FlyBack,
    }

    /// <summary>
    /// 魔晶槽：一颗在屏魔晶的全部数据，诞生时一次算好静态部分，动画仅推进时钟并重算位置。
    /// <para>不缓存实例矩阵：填充绘制缓冲时由 currentPos 现算(直写 8 个 float)，省去每槽 64B 缓存与整份结构体复制带宽。</para>
    /// </summary>
    private struct CrystalSlot
    {
        public SlotStateEnum state;
        //当前段动画的起点(Dropping=出生点/FlyBack=拾取瞬间位置)
        public Vector3 startPos;
        //当前段动画的终点(Dropping=落地点/FlyBack=核心偏移点)；Landed 态即常驻位置
        public Vector3 endPos;
        //当前位置(每帧由状态机推进；Landed 恒等于 endPos；billboard 在 shader 展开,填充时现算平移+缩放矩阵)
        public Vector3 currentPos;
        //状态内已进行时长(游戏速度时间,GameFightLogic.GetFightDeltaTime 驱动)
        public float animClock;
        //当前段动画总时长(Dropping 恒 0.8；FlyBack=距离/5)
        public float animDuration;
        //当前段抛物线跳高(Dropping 恒 0.3；FlyBack 为拾取时的随机 0~0.5)
        public float jumpHeight;
        //价值(飞回到账时入账)
        public int crystalNum;
        //配置的落地后存在时长(秒,>0 才会到期;仅落地瞬间换算 expireAt 用,之后不再读取)
        public float lifeTime;
        //到期时间戳(渲染器时钟;仅 Landed 有意义,落地时=rendererClock+lifeTime;lifeTime<=0 恒 float.MaxValue 永不到期)
        public float expireAt;
        //删除标记(Update 内到期/到账时置位,帧末统一压缩;替代逐颗 RemoveAt)
        public bool dead;
    }
    #endregion

    #region 字段
    //渲染资源(Setup 注册)：Quad 网格 + instanced 材质；未注册时本渲染器零副作用
    private Mesh mesh;
    private Material material;
    //魔晶世界尺寸(Setup 时按材质 _BaseMap 贴图像素/PPU×0.7 换算)
    private Vector2 baseScale = new Vector2(0.224f, 0.224f);
    //活跃魔晶槽(保持生成序——魔王自动拾取按 FIFO 取最先掉落；删除走帧末稳定压缩保序,不用 swap-back)
    private readonly List<CrystalSlot> listSlot = new List<CrystalSlot>(256);
    //本帧绘制缓冲(定容 1023 复用,热路径零分配)
    private readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxInstancesPerBatch];
    //上次填充缓冲时的实例数(dirty=false 帧按此数直接复用旧缓冲绘制)
    private int lastFillCount;
    //槽集脏标记:填槽/压缩/任何槽动画推进时置位;无变化帧复用旧缓冲直接绘制(Landed 位置恒定,浮动全在 shader,矩阵填充时由 currentPos 现算)
    private bool dirty = true;
    //渲染器时钟(Update 内按游戏速度帧时间累计,供 expireAt 到期比较;暂停/倍速与全场时钟同源)
    private float rendererClock;
    //飞回到账回调(crystalNum 入账逻辑由 GameFightLogic 注入,渲染器不碰用户数据;Update 内到账先记账,帧末压缩后统一派发)
    public Action<int> actionForCrystalArrived;
    //拾取查询的复用结果缓冲(避免每次查询分配)
    private readonly List<int> pickResultBuffer = new List<int>(64);
    //本帧到账的价值记账缓冲(Update 遍历中途不 Invoke——回调若重入 Clear/Add 会破坏槽迭代;帧末压缩后统一派发,派发时回调重入亦安全)
    private readonly List<int> arrivedNumBuffer = new List<int>(64);
    #endregion

    #region 注册与清理
    /// <summary>
    /// 是否已注册网格与材质(未注册时 AddCrystal/Update/RenderAll/拾取查询全部零副作用)。
    /// </summary>
    public bool IsReady => mesh != null && material != null;

    /// <summary>
    /// 注册魔晶渲染资源(Quad mesh + DropCrystalInstanced1 材质)；重复调用为替换。
    /// <para>材质未开 Enable GPU Instancing 时克隆一份开启(不污染共享资源)；世界尺寸从材质 _BaseMap 贴图像素换算(缺贴图用默认 0.224 并告警)。</para>
    /// </summary>
    public void Setup(Mesh mesh, Material material)
    {
        this.mesh = mesh;
        //DrawMeshInstanced 要求材质开启 instancing；共享材质未开时克隆一份开启,避免运行时改资源资产
        if (material != null && !material.enableInstancing)
            material = new Material(material) { enableInstancing = true };
        this.material = material;
        //世界尺寸 = 贴图像素/PPU×0.7(对齐旧预制 SpriteRenderer 的实际显示大小)
        Texture baseMap = material != null ? material.GetTexture("_BaseMap") : null;
        if (baseMap != null)
        {
            baseScale = new Vector2(
                baseMap.width / SpritePixelsPerUnit * SpriteWorldScale,
                baseMap.height / SpritePixelsPerUnit * SpriteWorldScale);
        }
        else
        {
            LogUtil.LogError("魔晶 instanced 材质缺少 _BaseMap 贴图,世界尺寸按默认 0.224 处理");
        }
    }

    /// <summary>
    /// 清空活跃在屏魔晶槽(战斗结束调用)；渲染资源保留,跨场复用；到账回调一并清空(其归属旧战斗逻辑实例,下一场由 InitFightConstData 重新注入)。
    /// </summary>
    public void Clear()
    {
        listSlot.Clear();
        lastFillCount = 0;
        dirty = true;
        rendererClock = 0f;
        actionForCrystalArrived = null;
        arrivedNumBuffer.Clear();
    }
    #endregion

    #region 魔晶生成与拾取
    /// <summary>
    /// 生成一颗魔晶：计算出生点(掉落点+0.5y)与落地点(±0.5 随机散布,+0.1y),填槽进入 Dropping 态。
    /// <para>朝向由 shader billboard 逐帧对齐相机(永远面向摄像头),CPU 不存旋转。未 Setup 时零副作用。</para>
    /// </summary>
    /// <param name="dropPos">掉落基准世界坐标(死亡生物位置)</param>
    /// <param name="crystalNum">价值(飞回到账时入账)</param>
    /// <param name="lifeTime">落地后存在时长(秒,含研究加成,由调用方算好)</param>
    public void AddCrystal(Vector3 dropPos, int crystalNum, float lifeTime)
    {
        if (!IsReady)
            return;
        CrystalSlot slot;
        slot.state = SlotStateEnum.Dropping;
        slot.startPos = dropPos + new Vector3(0, 0.5f, 0);
        //落地点随机散布(与旧逻辑同一公式:±0.5,+0.1y)
        float randomX = UnityEngine.Random.Range(-0.5f, 0.5f);
        float randomZ = UnityEngine.Random.Range(-0.5f, 0.5f);
        slot.endPos = new Vector3(dropPos.x + randomX, dropPos.y + 0.1f, dropPos.z + randomZ);
        slot.currentPos = slot.startPos;
        slot.animClock = 0f;
        slot.animDuration = DropJumpDuration;
        slot.jumpHeight = DropJumpHeight;
        slot.crystalNum = crystalNum;
        slot.lifeTime = lifeTime;
        //到期时间戳落地瞬间才换算(届时=rendererClock+lifeTime),Dropping 态给永不到期哨兵即可
        slot.expireAt = float.MaxValue;
        slot.dead = false;
        listSlot.Add(slot);
        dirty = true;
    }

    /// <summary>
    /// 鼠标点选：把鼠标屏幕点一次转成世界射线，取「魔晶到射线的垂直距离 ≤ 拾取半径 0.25」中最近的一颗 Landed 魔晶。
    /// <para>世界空间判定与旧「逐颗投影屏幕距离」等价(像素拾取半径随深度自适应、对齐旧 BoxCollider 命中手感)，但每颗只剩向量点积/减法的纯标量运算，无逐颗矩阵投影。</para>
    /// </summary>
    /// <returns>命中返回 true 且 index 为槽下标(仅当帧有效,须立即配合 StartFlyBack 使用)</returns>
    public bool TryPickByScreenPoint(Vector2 mouseScreenPos, out int index)
    {
        index = -1;
        Camera mainCamera = CameraHandler.Instance.manager.mainCamera;
        if (mainCamera == null)
            return false;
        //鼠标点一次转世界射线(方向已归一化)
        Ray pickRay = mainCamera.ScreenPointToRay(mouseScreenPos);
        float bestDistSqr = PickRadiusWorld * PickRadiusWorld;
        for (int i = 0; i < listSlot.Count; i++)
        {
            CrystalSlot slot = listSlot[i];
            if (slot.state != SlotStateEnum.Landed)
                continue;
            Vector3 toCrystal = slot.currentPos - pickRay.origin;
            float alongRay = Vector3.Dot(toCrystal, pickRay.direction);
            //相机身后(射线反方向)的点直接跳过
            if (alongRay <= 0f)
                continue;
            //垂直距离平方 = |toCrystal|² - alongRay²(勾股定理,免去开方)
            float distSqr = toCrystal.sqrMagnitude - alongRay * alongRay;
            if (distSqr <= bestDistSqr)
            {
                bestDistSqr = distSqr;
                index = i;
            }
        }
        return index >= 0;
    }

    /// <summary>
    /// 生物范围拾取：收集球心 radius 内的 Landed 槽(魔晶等效半径 0.2 并入判定,对齐旧 OverlapSphere 命中 BoxCollider 的效果)。
    /// <para>结果写入复用缓冲并经 out 返回下标数组快照;调用方须在当帧立即消费(下标仅当帧有效)。</para>
    /// </summary>
    /// <returns>命中槽下标列表(复用缓冲,下次查询即失效)</returns>
    public List<int> PickBySphere(Vector3 center, float radius)
    {
        pickResultBuffer.Clear();
        float pickRange = radius + CrystalBodyRadius;
        float sqrRange = pickRange * pickRange;
        for (int i = 0; i < listSlot.Count; i++)
        {
            CrystalSlot slot = listSlot[i];
            if (slot.state != SlotStateEnum.Landed)
                continue;
            if ((slot.currentPos - center).sqrMagnitude <= sqrRange)
                pickResultBuffer.Add(i);
        }
        return pickResultBuffer;
    }

    /// <summary>
    /// 魔王自动拾取(FIFO)：按槽生成序取最先掉落的 count 颗 Landed 魔晶。
    /// <para>结果写入复用缓冲并经 out 返回;调用方须在当帧立即消费(下标仅当帧有效)。</para>
    /// </summary>
    /// <returns>命中槽下标列表(复用缓冲,下次查询即失效)</returns>
    public List<int> PickFIFO(int count)
    {
        pickResultBuffer.Clear();
        for (int i = 0; i < listSlot.Count && pickResultBuffer.Count < count; i++)
        {
            if (listSlot[i].state == SlotStateEnum.Landed)
                pickResultBuffer.Add(i);
        }
        return pickResultBuffer;
    }

    /// <summary>
    /// 启动拾取飞回：指定槽转入 FlyBack 态(快照当前位置为起点、核心偏移点为终点、时长=距离/5、跳力随机 0~0.5)。
    /// <para>到账后由 actionForCrystalArrived 回调入账;转入 FlyBack 即不可再被拾取(对齐旧 Droping 态关碰撞)。</para>
    /// </summary>
    /// <param name="index">拾取查询返回的槽下标(仅当帧有效)</param>
    /// <param name="corePos">收集点(魔王/核心)世界坐标(未加偏移;内部按旧逻辑 +(0,0.5,0.5) 作为飞行终点)</param>
    public void StartFlyBack(int index, Vector3 corePos)
    {
        if (index < 0 || index >= listSlot.Count)
            return;
        CrystalSlot slot = listSlot[index];
        if (slot.state != SlotStateEnum.Landed)
            return;
        slot.state = SlotStateEnum.FlyBack;
        slot.startPos = slot.currentPos;
        slot.endPos = corePos + new Vector3(0f, 0.5f, 0.5f);
        slot.animClock = 0f;
        //时长按未偏移的核心距离算(与旧逻辑 Vector3.Distance(targetPos, pos)/5 一致)
        slot.animDuration = Vector3.Distance(corePos, slot.currentPos) / FlyBackMoveSpeed;
        slot.jumpHeight = UnityEngine.Random.Range(0f, 0.5f);
        listSlot[index] = slot;
        dirty = true;
    }
    #endregion

    #region 每帧更新与渲染入口
    /// <summary>
    /// 每帧更新(FightHandler.Update)：推进渲染器时钟与掉落/飞回抛物线(游戏速度时钟),Landed 到期判定(只读比较时间戳),飞回到账记账；
    /// 到期/到账统一打删除标记,帧末一次性稳定压缩(保序,FIFO 依赖生成序),压缩后统一派发到账回调(遍历中途不 Invoke,避免回调重入 Clear/Add 破坏槽迭代)。
    /// <para>抛物线公式精确复刻 DOTween DOJump:每跳 y += jumpHeight×4×phase×(1-phase),phase 为单跳相位;掉落 Linear、飞回 OutCubic。</para>
    /// </summary>
    public void Update()
    {
        int count = listSlot.Count;
        if (count == 0)
            return;
        //与旧 FightPrefabEntity 同一时钟源(跟随游戏速度/暂停)
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        rendererClock += deltaTime;
        bool anyDead = false;
        for (int i = 0; i < count; i++)
        {
            CrystalSlot slot = listSlot[i];
            switch (slot.state)
            {
                case SlotStateEnum.Dropping:
                {
                    slot.animClock += deltaTime;
                    if (slot.animClock >= slot.animDuration)
                    {
                        //落地:转入可拾取态,位置钉在落地点,寿命换算成到期时间戳(lifeTime<=0 永不到期)
                        slot.state = SlotStateEnum.Landed;
                        slot.currentPos = slot.endPos;
                        slot.expireAt = slot.lifeTime > 0 ? rendererClock + slot.lifeTime : float.MaxValue;
                        listSlot[i] = slot;
                        dirty = true;
                        continue;
                    }
                    //Linear 缓动:相位线性推进
                    float t = slot.animClock / slot.animDuration;
                    slot.currentPos = EvalJumpPosition(slot.startPos, slot.endPos, t, slot.jumpHeight, DropJumpCount);
                    listSlot[i] = slot;
                    dirty = true;
                    break;
                }
                case SlotStateEnum.Landed:
                {
                    //到期只读比较(时间戳落地时算好,无需每帧写回;对齐旧 FightPrefabEntity 只在 DropCheck 计寿命)
                    if (rendererClock >= slot.expireAt)
                    {
                        slot.dead = true;
                        listSlot[i] = slot;
                        anyDead = true;
                    }
                    break;
                }
                case SlotStateEnum.FlyBack:
                {
                    slot.animClock += deltaTime;
                    if (slot.animClock >= slot.animDuration)
                    {
                        //到账:打删除标记并记账,回调延后到帧末压缩后统一派发(对齐旧 DOJump OnComplete 的 AddCrystal+事件+销毁)
                        arrivedNumBuffer.Add(slot.crystalNum);
                        slot.dead = true;
                        listSlot[i] = slot;
                        anyDead = true;
                        continue;
                    }
                    //OutCubic 缓动:1-(1-t)^3(对齐旧 SetEase(Ease.OutCubic))
                    float t = slot.animClock / slot.animDuration;
                    float easeT = 1f - (1f - t) * (1f - t) * (1f - t);
                    slot.currentPos = EvalJumpPosition(slot.startPos, slot.endPos, easeT, slot.jumpHeight, 1);
                    listSlot[i] = slot;
                    dirty = true;
                    break;
                }
            }
        }
        //帧末统一压缩删除标记槽(单次 O(N) 遍历,替代逐颗 RemoveAt 的 O(N²) 反复搬运)
        if (anyDead)
        {
            CompactSlots();
            dirty = true;
        }
        //帧末统一派发到账回调(槽集已压缩稳定,回调内重入 Clear/Add 均安全;重入 Clear 会清空本缓冲,循环按动态 Count 自然终止)
        for (int i = 0; i < arrivedNumBuffer.Count; i++)
            actionForCrystalArrived?.Invoke(arrivedNumBuffer[i]);
        arrivedNumBuffer.Clear();
    }

    /// <summary>
    /// 每帧渲染(FightHandler.Update)：≤1023 时仅槽集变化帧重灌缓冲、无变化帧(全 Landed 静止)复用旧缓冲一次绘制；超单批上限逐帧重灌分批绘制(极端情况)。
    /// <para>矩阵由槽内 currentPos 现算填充(只含平移+缩放)；显式不投影不受影(shader 也无 ShadowCaster pass)；无活跃槽不绘制。</para>
    /// </summary>
    public void RenderAll()
    {
        if (!IsReady)
            return;
        int total = listSlot.Count;
        if (total == 0)
        {
            lastFillCount = 0;
            return;
        }
        if (total <= MaxInstancesPerBatch)
        {
            //单批可容:仅 dirty 帧重灌(恰好装满 1023 也走此处一次绘完,不留尾巴)
            if (dirty)
            {
                dirty = false;
                for (int i = 0; i < total; i++)
                    matrixBuffer[i] = BuildSlotMatrix(listSlot[i].currentPos);
                lastFillCount = total;
            }
            if (lastFillCount > 0)
                Graphics.DrawMeshInstanced(mesh, 0, material, matrixBuffer, lastFillCount, null, ShadowCastingMode.Off, false);
        }
        else
        {
            //超单批上限:缓冲装不下全量,满批即绘、收尾补绘(每帧重灌;极端情况,常态远达不到)
            int fillCount = 0;
            for (int i = 0; i < total; i++)
            {
                matrixBuffer[fillCount++] = BuildSlotMatrix(listSlot[i].currentPos);
                if (fillCount >= MaxInstancesPerBatch)
                {
                    Graphics.DrawMeshInstanced(mesh, 0, material, matrixBuffer, fillCount, null, ShadowCastingMode.Off, false);
                    fillCount = 0;
                }
            }
            if (fillCount > 0)
                Graphics.DrawMeshInstanced(mesh, 0, material, matrixBuffer, fillCount, null, ShadowCastingMode.Off, false);
        }
    }
    #endregion

    #region 内部工具
    /// <summary>
    /// 帧末统一压缩：移除全部 dead 标记槽(稳定保序,FIFO 拾取依赖生成序;单次 O(N) 遍历,替代逐颗 RemoveAt 的 O(N²) 反复搬运)。
    /// </summary>
    private void CompactSlots()
    {
        int count = listSlot.Count;
        int writeIndex = 0;
        for (int readIndex = 0; readIndex < count; readIndex++)
        {
            CrystalSlot slot = listSlot[readIndex];
            if (slot.dead)
                continue;
            if (writeIndex != readIndex)
                listSlot[writeIndex] = slot;
            writeIndex++;
        }
        if (writeIndex < count)
            listSlot.RemoveRange(writeIndex, count - writeIndex);
    }

    /// <summary>
    /// 求抛物线跳跃的当前位置：水平线性插值,垂直方向按单跳相位叠加 jumpHeight×4×phase×(1-phase) 的抛物线弧(复刻 DOTween DOJump)。
    /// </summary>
    private static Vector3 EvalJumpPosition(Vector3 startPos, Vector3 endPos, float t, float jumpHeight, int jumpCount)
    {
        Vector3 pos = Vector3.Lerp(startPos, endPos, t);
        //单跳相位:t×跳数取小数部,每跳一条完整抛物线(峰顶在相位 0.5,峰值=跳高)
        float phase = t * jumpCount % 1f;
        pos.y += jumpHeight * 4f * phase * (1f - phase);
        return pos;
    }

    /// <summary>
    /// 构建单颗魔晶的实例矩阵(只含平移+缩放,无旋转——billboard 在 shader 里用相机右/上轴展开,永远面向摄像头)。
    /// <para>与 FightTextInstanceRenderer.BuildCharMatrix 同写法：直接填对角矩阵,跳过 Matrix4x4.TRS 原生调用;填充绘制缓冲时由 currentPos 现算,槽内不缓存。</para>
    /// </summary>
    private Matrix4x4 BuildSlotMatrix(Vector3 position)
    {
        Matrix4x4 matrix = default;
        matrix.m00 = baseScale.x;
        matrix.m11 = baseScale.y;
        matrix.m22 = 1f;
        matrix.m33 = 1f;
        matrix.m03 = position.x;
        matrix.m13 = position.y;
        matrix.m23 = position.z;
        return matrix;
    }
    #endregion
}
