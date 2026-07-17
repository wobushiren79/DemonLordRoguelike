using System;
using System.Collections.Generic;
using UnityEngine;

public class FightManager : BaseManager
{
    //攻击预制obj
    public Dictionary<string, GameObject> dicAttackModeObj = new Dictionary<string, GameObject>();
    //攻击模块视觉预制obj(DSP 批量渲染的 mesh+material 载体)，与 dicAttackModeObj 同为持久资源缓存(跨战斗不释放)
    public Dictionary<string, GameObject> dicAttackModeVisualObj = new Dictionary<string, GameObject>();
    //DSP 换图/自旋子桶的克隆材质缓存(key=桶签名)：基材质来自预制不可改，故按签名克隆一份改 _BaseMap/UV/自旋；整场结束随预制一起销毁
    public Dictionary<string, Material> dicAttackModeVisualMat = new Dictionary<string, Material>();
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
        //销毁 DSP 换图/自旋子桶克隆出的材质(基材质来自预制、不在此列、勿销毁)
        foreach (var itemMat in dicAttackModeVisualMat.Values)
        {
            if (itemMat != null)
                GameObject.Destroy(itemMat);
        }
        dicAttackModeVisualMat.Clear();
        //清空视觉桶(其持有的 sharedMesh/sharedMaterial 引用来自上面已释放的预制)；方案2 拖尾 VFX 实例/缓冲由 ClearVisuals 内部转交 EffectHandler 一并销毁
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
    /// 确保某攻击模块的 DSP 弹体基础桶已注册：visual_name 非空且未注册时，按 AttackModeVisualPath 懒加载视觉预制
    /// (内含 MeshFilter+MeshRenderer)，提取 sharedMesh/sharedMaterial 登记到 attackModeInstanceRenderer。
    /// <para>视觉预制与弹道 prefab 同为持久资源缓存(dicAttackModeVisualObj，跨战斗不释放)，故只加载一次、后续复用。</para>
    /// <para>⚠️只管弹体桶、**不派生拖尾桶**：本方法在发射前预热调用，此时换图/自旋参数尚未解析(要到 InitAttackModeShow 才有)，
    /// 在这里注册拖尾等于按基础 visual_name 建一个桶——而换图弹道实际用的是子桶签名，基础桶永远收不到采样点，
    /// 方案2 下会白白多出一个常驻空跑的 VFX 实例(场景里两个 AttackModeTrailVfx_*)。故拖尾统一交给下面按实际签名注册的重载。</para>
    /// </summary>
    public void EnsureAttackModeVisual(AttackModeInfoBean attackModeInfo)
    {
        if (attackModeInfo == null || attackModeInfo.visual_name.IsNull())
            return;
        //已注册则跳过重复加载
        if (attackModeInstanceRenderer.HasVisual(attackModeInfo.visual_name))
            return;
        if (!TryGetAttackModeVisualSource(attackModeInfo, out Mesh mesh, out Material material))
            return;
        //默认桶(无换图无自旋)直接用基础 sharedMesh/sharedMaterial(勿用 .mesh/.material，否则复制副本破坏 instancing 合批)
        attackModeInstanceRenderer.RegisterVisual(attackModeInfo.visual_name, mesh, material);
    }

    /// <summary>
    /// 懒加载某 visual_name 的视觉预制并取出其 sharedMesh/sharedMaterial（供默认桶注册与子桶克隆共用的取源）。
    /// <para>预制持久缓存于 dicAttackModeVisualObj(跨战斗不释放)，只加载一次；缺 MeshFilter/MeshRenderer 报错返 false。</para>
    /// </summary>
    private bool TryGetAttackModeVisualSource(AttackModeInfoBean attackModeInfo, out Mesh mesh, out Material material)
    {
        mesh = null;
        material = null;
        GameObject visualPrefab = GetModelForAddressablesSync(dicAttackModeVisualObj, $"{PathInfo.AttackModeVisualPath}/{attackModeInfo.visual_name}.prefab");
        if (visualPrefab == null)
            return false;
        var meshFilter = visualPrefab.GetComponentInChildren<MeshFilter>();
        var meshRenderer = visualPrefab.GetComponentInChildren<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null)
        {
            LogUtil.LogError($"AttackModeVisual 预制缺少 MeshFilter/MeshRenderer: {attackModeInfo.visual_name}");
            return false;
        }
        mesh = meshFilter.sharedMesh;
        material = meshRenderer.sharedMaterial;
        return true;
    }

    /// <summary>
    /// 按某个攻击模式实例的实际视觉参数(换图 ShowSprite + 自旋)算出视觉桶签名并确保对应子桶已注册。
    /// <para>在 BaseAttackMode.InitAttackModeShow 末尾调用(武器 attack_mode_data 已解析)。默认签名(无换图无自旋)复用基础桶；
    /// 有覆盖项时克隆一份基材质做子桶专属材质：换图子桶异步取图集 sprite 写 _BaseMap+UV 后再登记(首帧即正确)，仅自旋子桶直接登记(自旋由 RenderAll 写)。</para>
    /// <para>克隆材质缓存于 dicAttackModeVisualMat 兼作去重(已建则跳过，避免重复克隆/重复异步加载)，整场结束统一销毁。</para>
    /// <para>⚠️**拖尾桶只在此注册**(按实际签名，与弹道真正使用的桶一一对应)：预热用的 EnsureAttackModeVisual(config) 重载不派生拖尾，
    /// 否则换图弹道会多出一个基础 visual_name 的拖尾桶、永远收不到采样点(方案2 即常驻空跑的 VFX 实例)。</para>
    /// </summary>
    public void EnsureAttackModeVisual(BaseAttackMode attackMode)
    {
        if (attackMode == null || attackMode.attackModeInfo == null || attackMode.attackModeInfo.visual_name.IsNull())
        {
            //无 visual_name=不走 DSP，清空签名让 RenderAll 跳过
            if (attackMode != null)
                attackMode.visualBucketKey = null;
            return;
        }
        AttackModeInfoBean attackModeInfo = attackMode.attackModeInfo;
        //⚠️自旋必须在此**快照成局部量**再用：attackMode 来自对象池，换图子桶的注册在图集异步回调里(数帧后)才发生，
        //那时这个实例很可能已被回收重发(ResetVisualParams 把 spinSpeed 清 0)，回调里再读 attackMode.spinXxx 拿到的是别人的值——
        //而桶签名编码的是**当下**这份自旋，二者对不上就会把错误自旋写进该桶材质(整桶弹体+拖尾转错/不转)
        Vector3 spinAxis = attackMode.spinAxis;
        float spinSpeed = attackMode.spinSpeed;
        string key = AttackModeInstanceRenderer.BuildVisualBucketKey(attackModeInfo.visual_name, attackMode.visualSpriteName, spinAxis, spinSpeed);
        attackMode.visualBucketKey = key;
        //本发弹道按配置开启/关闭拖尾(count/interval≤0 时 EnableTrail 内部关闭；对象池复用每发都重设)
        AttackModeTrailConfig trailConfig = attackModeInfo.GetTrailConfig();
        attackMode.EnableTrail(trailConfig);
        //默认签名(与 visual_name 同 key)：基础桶由 EnsureAttackModeVisual(config) 保证，直接登记复用，再按配置派生该桶拖尾
        if (key == attackModeInfo.visual_name)
        {
            EnsureAttackModeVisual(attackModeInfo);
            attackModeInstanceRenderer.RegisterTrailFromVisual(key, trailConfig);
            return;
        }
        //该子桶已建过(克隆材质已在)：去重，避免重复克隆/重复异步加载
        if (dicAttackModeVisualMat.ContainsKey(key))
            return;
        //取基础预制的 sharedMesh + 基材质，克隆基材质做子桶专属材质(不同贴图/自旋互不覆盖)
        if (!TryGetAttackModeVisualSource(attackModeInfo, out Mesh mesh, out Material baseMat))
            return;
        Material subMat = new Material(baseMat);
        dicAttackModeVisualMat[key] = subMat;
        if (!attackMode.visualSpriteName.IsNull())
        {
            //换图子桶：异步取图集 Items 的 sprite → 写子材质 _BaseMap 及其在图集内的 UV 子区域 → 贴图就绪后再登记桶(未就绪的攻击模式本帧被 RenderAll 跳过)
            IconHandler.Instance.GetIconSprite(SpriteAtlasTypeEnum.Items, attackMode.visualSpriteName, (sprite) =>
            {
                //克隆材质可能在异步期间随整场结束被销毁(== null 对已销毁 UnityObject 成立)，此时直接放弃登记
                if (subMat == null)
                    return;
                if (sprite != null)
                {
                    //GetOuterUV 返回该 sprite 在图集纹理内的归一化 UV(xMin,yMin,xMax,yMax)：tiling=尺寸、offset=起点，喂给 _BaseMap_ST(shader 用 TRANSFORM_TEX 采样)
                    Vector4 outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                    subMat.SetTexture("_BaseMap", sprite.texture);
                    subMat.SetTextureScale("_BaseMap", new Vector2(outerUV.z - outerUV.x, outerUV.w - outerUV.y));
                    subMat.SetTextureOffset("_BaseMap", new Vector2(outerUV.x, outerUV.y));
                    //宽高比修正：按 sprite 像素宽高 contain 归一化(长边=1)写进材质 _VertexScaleXY，由 shader 在对象空间(自旋之前最内层)缩放，非方形 sprite 不再拉伸、且自旋不抖动
                    float spriteW = sprite.rect.width;
                    float spriteH = sprite.rect.height;
                    float spriteMax = Mathf.Max(spriteW, spriteH);
                    if (spriteMax > 0f)
                        subMat.SetVector("_VertexScaleXY", new Vector4(spriteW / spriteMax, spriteH / spriteMax, 1f, 1f));
                }
                //自旋用上面快照的局部量，勿改回读 attackMode(本回调时它可能已被对象池回收重发，见 spinAxis 快照处注释)
                attackModeInstanceRenderer.RegisterVisual(key, mesh, subMat, spinAxis, spinSpeed);
                //换图子桶贴图就绪后派生拖尾桶(拖尾复用该子桶贴图；tile 平铺越界风险见 RegisterTrailFromVisual 注释)
                attackModeInstanceRenderer.RegisterTrailFromVisual(key, attackModeInfo.GetTrailConfig());
            });
        }
        else
        {
            //仅自旋不同的子桶：贴图沿用基材质，自旋在 RegisterVisual 内一次性写入子材质(整桶自旋恒定)，直接登记
            attackModeInstanceRenderer.RegisterVisual(key, mesh, subMat, spinAxis, spinSpeed);
            //自旋子桶就绪后派生拖尾桶(贴图沿用基材质)
            attackModeInstanceRenderer.RegisterTrailFromVisual(key, attackModeInfo.GetTrailConfig());
        }
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
