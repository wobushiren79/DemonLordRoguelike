using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 幻化药 other_data 键值解析结果（按 &amp; 拆项、每项首个 : 拆 key/value，缺省键=对应字段为 null）。
/// <para>用结构体承载替代多 out 参数：新增键时只需加字段 + ParseTransformOtherData 加 case，全部调用点零改动；值类型无 GC 分配。</para>
/// </summary>
public struct TransformOtherData
{
    /// <summary>show_res：默认展示形象（世界/战斗/普通卡片；可空=仅详情UI幻化道具，世界/小卡回落原生物形象）</summary>
    public string showRes;
    /// <summary>ui_show_res：ui_show_spine 高清展示资源（详情UI，可空）</summary>
    public string uiShowRes;
    /// <summary>ui_show_data：详情UI尺寸「scale;x,y」（可空）</summary>
    public string uiShowData;
    /// <summary>show_data：默认展示小卡UI尺寸「scale;x,y」（可空）</summary>
    public string showData;
    /// <summary>world_data：世界显示尺寸/偏移「scale;x,y」（可空，x=横向偏移,y=竖向抬升）</summary>
    public string worldData;
    /// <summary>ui_show_skin：ui_show 高清展示指定皮肤名（可空，ArkReSpine 单资源多皮肤按皮肤出药时配置；支持「|」分隔多皮肤叠加组合，CherryTaleSpine 组合皮肤药如 Eye_01|Mouth_01）</summary>
    public string uiShowSkin;
    /// <summary>idle_anim：show 骨架替代待机动画名（可空，生成期检测：骨架无标准待机候选时写入首个含 idle 字段动画）</summary>
    public string idleAnim;
    /// <summary>ui_show_idle_anim：ui_show 骨架替代待机动画名（可空，规则同 idle_anim，仅详情UI播放消费）</summary>
    public string uiShowIdleAnim;
}

public partial class CreatureBean
{
    #region 终焉议会议员
    /// <summary>
    /// 获取该生物作为NPC的类型(非NPC返回None)
    /// </summary>
    public NpcTypeEnum GetNpcType()
    {
        var npcData = GetCreatureNpcData();
        if (npcData == null || npcData.npcId == 0)
            return NpcTypeEnum.None;
        var npcInfo = NpcInfoCfg.GetItemData(npcData.npcId);
        if (npcInfo == null)
            return NpcTypeEnum.None;
        return npcInfo.GetNpcType();
    }

    /// <summary>
    /// 是否为议会固定NPC(拥有独立持久化的好感系统)
    /// </summary>
    public bool IsFixedCouncilor()
    {
        return GetNpcType() == NpcTypeEnum.Councilor;
    }

    /// <summary>
    /// 议会议员: 设置显示名。固定议员用其自身NPC名字(NpcInfo.name), 随机议员走通用命名(NpcInfoBean.GetCouncilorRandomDisplayName 评级称谓名)
    /// </summary>
    public void SetCouncilorDisplayName()
    {
        var npcData = GetCreatureNpcData();
        if (npcData == null || npcData.npcId == 0)
            return;
        var npcInfo = NpcInfoCfg.GetItemData(npcData.npcId);
        if (npcInfo == null)
            return;
        //固定议员: 使用NPC自身名字
        if (npcInfo.GetNpcType() == NpcTypeEnum.Councilor)
        {
            creatureName = npcInfo.name_language;
            return;
        }
        //随机议员: 使用评级称谓名
        creatureName = npcInfo.GetCouncilorRandomDisplayName();
    }
    #endregion

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    public int order;//排序

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    public float RCDTimeUpdate = 0;    //生物复活更新时间

    //固定属性表(测试模式专用运行时数据, 不入存档): 设置后 GetAttribute 该项属性的基础值替换为固定值, 加点/装备/BUFF/深渊馈赠等修正仍在固定值上叠加
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    public Dictionary<CreatureAttributeTypeEnum, float> dicFixedAttribute;

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureInfoBean _creatureInfo;

    /// <summary>
    /// 生物配置(按 creatureId 懒加载缓存)
    /// <para>自校验: creatureId 会被中途改写(终焉议会转生直接赋值/对象池复用 SetData), 缓存 id 与当前 creatureId 不一致时重新解析, 避免沿用旧种类配置</para>
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public CreatureInfoBean creatureInfo
    {
        get
        {
            if (_creatureInfo == null || _creatureInfo.id != creatureId)
            {
                _creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
                if(_creatureInfo == null)
                {
                    LogUtil.LogError($"获取CreatureInfoBean失败 id_{creatureId}");
                }
            }
            return _creatureInfo;
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureModelBean _creatureModel;

    /// <summary>
    /// 生物模型配置(按 creatureInfo.model_id 懒加载缓存)
    /// <para>自校验: 随 <see cref="creatureInfo"/> 联动失效, 模型 id 与当前种类配置不一致时重新解析, 避免沿用旧种类的 Spine 资源</para>
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public CreatureModelBean creatureModel
    {
        get
        {
            var info = creatureInfo;
            if (info == null)
                return null;
            if (_creatureModel == null || _creatureModel.id != info.model_id)
            {
                _creatureModel = CreatureModelCfg.GetItemData(info.model_id);
            }
            return _creatureModel;
        }
    }

    //献祭升级保底成功率: 上一次献祭失败时记录为"当次成功率的一半",下一次献祭叠加在祭品成功率之上;献祭成功后清零。
    public float sacrificePityRate;

    #region 魔王
    /// <summary>
    /// 是否为魔王本体(与玩家存档中的 selfCreature 同一 UUId)。
    /// 魔王独立存储于 UserDataBean.selfCreature,不在背包/阵容列表内;判定用于:管理列表置顶、稀有度按L显示、隐藏等级、战斗不加经验等特殊处理。
    /// </summary>
    /// <returns>true=魔王本体</returns>
    public bool IsDemonLord()
    {
        var selfCreature = GameDataHandler.Instance.manager.GetUserData()?.selfCreature;
        if (selfCreature == null)
            return false;
        return !creatureUUId.IsNull() && creatureUUId == selfCreature.creatureUUId;
    }
    #endregion

    #region 稀有度
    /// <summary>
    /// 获取归一化后的稀有度值:rarity≤0(旧存档/未初始化)统一视为 N。
    /// 收口散落在升阶链路(UICreatureVat)与 CMP 计算等处的 "rarity<=0?N:rarity" 重复判断。
    /// </summary>
    /// <returns>归一化稀有度值(≥N)</returns>
    public int GetRarityValue()
    {
        return rarity <= 0 ? (int)RarityEnum.N : rarity;
    }
    #endregion

    #region 等级升级
    /// <summary>
    /// 获取下一级所需的等级配置(达到上限返回 null)
    /// </summary>
    /// <returns>下一级的 LevelInfoBean;若已满级则为 null</returns>
    public LevelInfoBean GetNextLevelInfo()
    {
        return LevelInfoCfg.GetItemData(level + 1);
    }

    /// <summary>
    /// 是否已达到等级上限(没有下一级配置)
    /// </summary>
    /// <returns>已满级返回 true</returns>
    public bool IsMaxLevel()
    {
        var nextLevelInfo = GetNextLevelInfo();
        return nextLevelInfo == null || nextLevelInfo.id == 0;
    }

    /// <summary>
    /// 当前经验是否已满足升到下一级(未满级且 levelExp >= 下一级所需经验)
    /// </summary>
    /// <returns>可升级返回 true</returns>
    public bool CanUpLevel()
    {
        var nextLevelInfo = GetNextLevelInfo();
        if (nextLevelInfo == null || nextLevelInfo.id == 0)
            return false;
        return levelExp >= long.Parse(nextLevelInfo.level_exp);
    }

    /// <summary>
    /// 通过献祭升级一级: 经验清 0、等级+1,并返回本次升级获得的可分配属性加点数。
    /// <para>升级成功后 levelExp 直接归零(不保留溢出余量),后续经验从 0 重新累积。</para>
    /// <para>属性加点不再自动加成, 改由玩家在 UICreatureAddAttribute 界面手动分配(见献祭升级成功流程)。</para>
    /// <para>仅在献祭成功时调用;调用前应已通过 CanUpLevel() 校验。</para>
    /// </summary>
    /// <returns>本次升级获得的属性加点数(取下一级 LevelInfo.attribute_point, 未配置默认 1);已满级返回 0</returns>
    public int UpLevelForSacrifice()
    {
        var nextLevelInfo = GetNextLevelInfo();
        //已满级,不再升级
        if (nextLevelInfo == null || nextLevelInfo.id == 0)
            return 0;
        //升级成功后经验清 0(不保留溢出余量)
        levelExp = 0;
        //等级+1
        level++;
        //本次升级获得的加点数(配置驱动, 未配置默认 1)
        int attributePoint = nextLevelInfo.attribute_point;
        if (attributePoint <= 0)
            attributePoint = 1;
        return attributePoint;
    }
    #endregion

    #region 创建随机属性
    /// <summary>
    /// 创建生物时随机属性加点(孕育扭蛋/新建存档初始魔物共用)
    /// <para>总点数取自 UserLimmitBean.gashaponRandomAttributeNum, 配置异常(小于等于0)时兜底不加点。</para>
    /// <para>随机池按 creatureInfo.show_attribute 配置过滤(如烂泥/毒液史莱姆只会随机到ATK);creatureInfo 为 null(纯对话NPC)时底层兜底默认池。</para>
    /// </summary>
    /// <param name="userData">用户数据(新建存档时 GameDataHandler 尚未 SetUserData, 需显式传入新建的 UserDataBean)</param>
    public void RandomAttributeForCreate(UserDataBean userData)
    {
        if (userData == null)
            return;
        int randomAttributeNum = userData.GetUserLimmitData().gashaponRandomAttributeNum;
        if (randomAttributeNum <= 0)
            return;
        creatureAttribute.AddRandomAttributeForCreate(randomAttributeNum, creatureInfo?.GetShowAttributeList());
    }

    /// <summary>
    /// 创建生物时固定属性加点(新建存档赠送的初始魔物专用)
    /// <para>总点数取自 UserLimmitBean.gashaponRandomAttributeNum(与孕育扭蛋一致), 但不再随机分配,</para>
    /// <para>而是把全部点数固定堆到指定属性上(单点增量见 CreatureUtil.GetAttributePointAddValue)。</para>
    /// <para>配置异常(小于等于0)时兜底不加点。</para>
    /// </summary>
    /// <param name="userData">用户数据(新建存档时 GameDataHandler 尚未 SetUserData, 需显式传入新建的 UserDataBean)</param>
    /// <param name="attributeType">要固定堆叠的属性类型</param>
    public void FixedAttributeForCreate(UserDataBean userData, CreatureAttributeTypeEnum attributeType)
    {
        if (userData == null)
            return;
        int attributeNum = userData.GetUserLimmitData().gashaponRandomAttributeNum;
        if (attributeNum <= 0)
            return;
        creatureAttribute.AddFixedAttributeForCreate(attributeNum, attributeType);
    }
    #endregion

    #region 创建随机稀有度BUFF
    /// <summary>
    /// 创建生物时按当前稀有度逐级随机授予稀有度BUFF(孕育扭蛋/测试添加生物等创建链路共用)
    /// <para>从 R 档起逐级到当前 rarity,每档经 BuffUtil.CreateRandomRarityBuff 随机生成一条填入 dicRarityBuff;</para>
    /// <para>仅 R/SR/SSR 配有对应BUFF类型,N/UR/L 档生成为 null 自动跳过(只升稀有度不授BUFF)。</para>
    /// <para>需先设置好 rarity 再调用;高稀有度会累积各低档各 1 条(如 SSR 得 R+SR+SSR 各 1)。</para>
    /// </summary>
    public void RandomRarityBuffForCreate()
    {
        for (int rarityValue = (int)RarityEnum.R; rarityValue <= rarity; rarityValue++)
        {
            RarityEnum rarityEnum = (RarityEnum)rarityValue;
            BuffBean buffData = BuffUtil.CreateRandomRarityBuff(rarityEnum);
            if (buffData == null)
                continue;
            dicRarityBuff[rarityEnum] = buffData;
        }
    }
    #endregion

    /// <summary>
    /// 清理临时数据
    /// <para>会清空皮肤/装备/等级/稀有度等所有数据，仅用于"一次性 Bean 入池复用"（复用时会通过 SetData 重建）。</para>
    /// <para>切勿对与玩家存档共享引用的阵容生物 Bean 调用此方法，否则会清空其皮肤数据导致 Spine 无法显示，请改用 <see cref="ClearFightTempData"/>。</para>
    /// </summary>
    public void ClearTempData()
    {
        order = 0;
        RCDTimeUpdate = 0;
        creatureState = CreatureStateEnum.Idle;
        level = 0;
        levelExp = 0;
        sacrificePityRate = 0;
        rarity = 0;
        relationship = 0;
        creatureNpcData = null;
        dicSkinData.Clear();
        dicEquipItemData.Clear();
        dicRarityBuff.Clear();
        transformItemId = 0;
    }

    /// <summary>
    /// 清理战斗运行时临时状态
    /// <para>仅重置战斗期间产生的运行时状态（排序、复活计时、生物状态），</para>
    /// <para>保留皮肤(dicSkinData)/装备/等级/稀有度等持久核心数据。</para>
    /// <para>用于战斗结束后还原与玩家存档共享引用的阵容生物 Bean，使其回到可用的待机状态。</para>
    /// </summary>
    public void ClearFightTempData()
    {
        order = 0;
        RCDTimeUpdate = 0;
        creatureState = CreatureStateEnum.Idle;
    }

    #region 幻化相关
    /// <summary>已日志过的缺失幻化配置ID（防列表刷新每只生物都调解析时错误日志刷屏，每id每会话只记一次）</summary>
    protected static HashSet<long> loggedMissingTransformIds = new HashSet<long>();

    /// <summary>
    /// 获取当前幻化道具的配置（transformItemId 指向的 TransformPotion 道具配置），各幻化解析入口共用。
    /// <para>transformItemId=0 / 配置缺失(Mod移除) / 类型非幻化药(防 Mod id 被复用覆盖) / other_data 为空时均返回 null；</para>
    /// <para>只存道具ID实时查配置：Mod 提供幻化药时 Mod 移除（配置失效）即自动失效，Mod 装回自动恢复；幻原药置0可主动清除。</para>
    /// </summary>
    /// <returns>幻化道具配置；无幻化或配置异常返回 null</returns>
    public ItemsInfoBean GetTransformItemInfo()
    {
        if (transformItemId == 0)
            return null;
        ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(transformItemId);
        if (itemInfo == null)
        {
            if (loggedMissingTransformIds.Add(transformItemId))
                LogUtil.LogError($"获取幻化资源失败 没有找到道具配置:{transformItemId}(Mod移除或配置被删,幻化自动失效)");
            return null;
        }
        if (itemInfo.GetItemType() != ItemTypeEnum.TransformPotion)
            return null;
        if (itemInfo.other_data.IsNull())
            return null;
        return itemInfo;
    }

    /// <summary>
    /// 获取当前幻化状态应替换的 spine 资源名（SkeletonDataAsset 的 Addressables 资源名），全项目唯一形象解析入口。
    /// <para>other_data 键值格式中本方法只取 show_res 键（世界/战斗/普通卡片的默认展示形象）。</para>
    /// </summary>
    /// <returns>spine 资源名；无幻化或配置异常返回 null</returns>
    public string GetTransformSpineRes()
    {
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return null;
        return ParseTransformOtherData(itemInfo.other_data).showRes;
    }

    /// <summary>
    /// 获取幻化的 ui_show_spine 高清展示资源名（other_data 的 ui_show_res 键，详情UI专用）；
    /// 未配置 ui_show_res 键返回 null（调用方回落 show_res），无幻化/配置异常返回 null。
    /// </summary>
    public string GetTransformUIShowSpineRes()
    {
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return null;
        return ParseTransformOtherData(itemInfo.other_data).uiShowRes;
    }

    /// <summary>
    /// 获取幻化 ui_show 高清展示指定皮肤名（other_data 的 ui_show_skin 键，详情UI专用；ArkReSpine 单资源多皮肤套装按皮肤出药时配置；
    /// 支持「|」分隔多皮肤叠加组合，CherryTaleSpine 组合皮肤药如 Eye_01|Mouth_01，调用方拆分后走多皮肤换肤）；
    /// 未配置 ui_show_skin 键返回 null（调用方保持骨架默认皮肤），无幻化/配置异常返回 null。
    /// </summary>
    public string GetTransformUIShowSkin()
    {
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return null;
        return ParseTransformOtherData(itemInfo.other_data).uiShowSkin;
    }

    /// <summary>
    /// 获取幻化 show 骨架的替代待机动画名（other_data 的 idle_anim 键，世界/战斗/小卡等 show 段播放消费）；
    /// 生成期检测：show 骨架无标准待机候选(idle,wait,idle1,wait1,stand)时取首个含 idle 字段的动画名写入；未配置返回 null（调用方走框架候选解析）。
    /// </summary>
    public string GetTransformIdleAnim()
    {
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return null;
        return ParseTransformOtherData(itemInfo.other_data).idleAnim;
    }

    /// <summary>
    /// 获取幻化 ui_show 骨架的替代待机动画名（other_data 的 ui_show_idle_anim 键，仅详情UI播放消费）；
    /// 生成期检测：ui_show 骨架无标准待机候选时取首个含 idle 字段的动画名写入；未配置返回 null（调用方走框架候选解析）。
    /// </summary>
    public string GetTransformUIShowIdleAnim()
    {
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return null;
        return ParseTransformOtherData(itemInfo.other_data).uiShowIdleAnim;
    }

    /// <summary>
    /// 获取幻化高清展示自带的详情UI尺寸配置（other_data 的 ui_show_data 键「scale;x,y」，格式同 CreatureModelBean.ui_data_b，
    /// 生成器按 Avator 骨架高度校准）；原生物 ui_data_b 按原骨架校准、不适用于 Mod 高清骨架，故由道具自带。
    /// 编辑器下测试覆盖层(TransformPotionUITestOverride)有值时优先于配置返回(幻化药测试面板调参实时预览用)。
    /// </summary>
    /// <returns>是否配置了尺寸段（true 时 scale/pos 有效）</returns>
    public bool GetTransformUIShowData(out float scale, out Vector2 pos)
    {
        scale = 1;
        pos = Vector2.zero;
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return false;
        string uiData = ParseTransformOtherData(itemInfo.other_data).uiShowData;
#if UNITY_EDITOR
        //测试覆盖层优先(幻化药测试面板调参实时预览用, 打包无此逻辑)
        if (TransformPotionUITestOverride.TryGetUiShowData(transformItemId, out string overrideShowData))
            uiData = overrideShowData;
#endif
        if (uiData.IsNull())
            return false;
        string[] uiDataStr = uiData.Split(';');
        if (uiDataStr.Length < 2 || !float.TryParse(uiDataStr[0], out scale))
        {
            scale = 1;
            return false;
        }
        pos = uiDataStr[1].SplitForVector2(',');
        return true;
    }

    /// <summary>
    /// 获取幻化默认展示形象自带的小卡UI尺寸配置（other_data 的 show_data 键「scale;x,y」，格式同 CreatureModelBean.ui_data_s，
    /// 生成器按默认展示骨架高度校准）；原生物 ui_data_s 按原骨架校准、不适用于 Mod 骨架，故由道具自带。
    /// 编辑器下测试覆盖层(TransformPotionUITestOverride)有值时优先于配置返回(幻化药测试面板调参实时预览用)。
    /// </summary>
    /// <returns>是否配置了尺寸段（true 时 scale/pos 有效）</returns>
    public bool GetTransformShowData(out float scale, out Vector2 pos)
    {
        scale = 1;
        pos = Vector2.zero;
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return false;
        string showData = ParseTransformOtherData(itemInfo.other_data).showData;
#if UNITY_EDITOR
        //测试覆盖层优先(幻化药测试面板调参实时预览用, 打包无此逻辑)
        if (TransformPotionUITestOverride.TryGetShowData(transformItemId, out string overrideChessData))
            showData = overrideChessData;
#endif
        if (showData.IsNull())
            return false;
        string[] uiDataStr = showData.Split(';');
        if (uiDataStr.Length < 2 || !float.TryParse(uiDataStr[0], out scale))
        {
            scale = 1;
            return false;
        }
        pos = uiDataStr[1].SplitForVector2(',');
        return true;
    }

    /// <summary>
    /// 解析幻化药 other_data 键值格式：「show_res:X&ui_show_res:X&ui_show_data:scale;x,y&show_data:scale;x,y&world_data:scale;x,y&ui_show_skin:LV1&idle_anim:X&ui_show_idle_anim:X」。
    /// 按 &amp; 拆项、每项以第一个 : 拆 key/value（与 attack_mode other_data 同规约），缺省键=对应字段为 null；未知键静默忽略（向后兼容）。
    /// 各键语义见 <see cref="TransformOtherData"/> 字段注释；新增键=TransformOtherData 加字段+此处加 case，调用点零改动。
    /// </summary>
    public static TransformOtherData ParseTransformOtherData(string otherData)
    {
        TransformOtherData data = new TransformOtherData();
        if (otherData.IsNull())
            return data;
        string[] items = otherData.Split('&');
        for (int i = 0; i < items.Length; i++)
        {
            string item = items[i];
            if (string.IsNullOrEmpty(item))
                continue;
            int sep = item.IndexOf(':');
            if (sep <= 0)
                continue;
            string value = item.Substring(sep + 1).Trim();
            switch (item.Substring(0, sep).Trim())
            {
                case "show_res":
                    data.showRes = value;
                    break;
                case "ui_show_res":
                    data.uiShowRes = value;
                    break;
                case "ui_show_data":
                    data.uiShowData = value;
                    break;
                case "show_data":
                    data.showData = value;
                    break;
                case "world_data":
                    data.worldData = value;
                    break;
                case "ui_show_skin":
                    data.uiShowSkin = value;
                    break;
                case "idle_anim":
                    data.idleAnim = value;
                    break;
                case "ui_show_idle_anim":
                    data.uiShowIdleAnim = value;
                    break;
            }
        }
        return data;
    }

    /// <summary>
    /// 获取幻化世界显示自带的尺寸/偏移配置（other_data 的 world_data 键「scale;x,y」，x=横向偏移,y=竖向抬升）；
    /// 消费点=CreatureHandler.SetCreatureData 世界缩放/位置注入（战斗/基地/议会等世界空间 SkeletonAnimation 显示）。
    /// 编辑器下测试覆盖层(TransformPotionUITestOverride)有值时优先于配置返回(幻化药测试面板调参实时预览用)。
    /// </summary>
    /// <returns>是否配置了世界显示段（true 时 scale/pos 有效）</returns>
    public bool GetTransformWorldData(out float scale, out Vector2 pos)
    {
        scale = 1;
        pos = Vector2.zero;
        ItemsInfoBean itemInfo = GetTransformItemInfo();
        if (itemInfo == null)
            return false;
        string worldData = ParseTransformOtherData(itemInfo.other_data).worldData;
#if UNITY_EDITOR
        //测试覆盖层优先(幻化药测试面板调参实时预览用, 打包无此逻辑)
        if (TransformPotionUITestOverride.TryGetWorldData(transformItemId, out string overrideWorldData))
            worldData = overrideWorldData;
#endif
        if (worldData.IsNull())
            return false;
        string[] worldDataStr = worldData.Split(';');
        if (worldDataStr.Length < 2 || !float.TryParse(worldDataStr[0], out scale))
        {
            scale = 1;
            return false;
        }
        pos = worldDataStr[1].SplitForVector2(',');
        return true;
    }
    #endregion

}
