using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffTypeEnum
{
    None = 0,
    Fight = 1,//攻击模块
    CreatureSelf = 2,//生物自带
    AbyssalBlessing = 3,//深渊馈赠

    CreatureRarityR = 11,//生物稀有度R BUFF
    CreatureRaritySR = 12,//生物稀有度SR BUFF
    CreatureRaritySSR = 13,//生物稀有度SSR BUFF
}


public enum GameStateEnum
{
    None = 0,
    Pre,//准备中
    Gaming,//游戏中
    Settlement,//结算中
    End,//游戏结束
}

public enum RarityEnum
{
    N = 1,
    R = 2,
    SR = 3,
    SSR = 4,
    UR = 5,
    L = 6
}

//卡片用途
public enum CardUseStateEnum
{
    Show,//展示
    ShowNoPopup,//展示但不弹详情
    Fight,//战斗
    Lineup,//阵容
    LineupBackpack,//阵容背包
    CreatureManager,//魔物管理
    CreatureSacrifice,//魔物献祭

    CreatureAscendTarget,//魔物进阶
    CreatureAscendMaterial,//魔物进阶

    SelectCreature,//生物选择
}

//卡片状态
public enum CardStateEnum
{
    None = 0,//空
    FightIdle = 101,//待机
    FightSelect = 102,//选择
    Fighting = 103,//上场战斗
    FightRest = 104,//休息

    LineupNoSelect = 201,//阵容未选中
    LineupSelect = 202,//阵容选中

    CreatureManagerNoSelect = 301,//生物管理未选中
    CreatureManagerSelect = 302,//生物管理选中

    CreatureSacrificeNoSelect = 401,//生物献祭未选中
    CreatureSacrificeSelect = 402,//生物献祭选中

    CreatureAscendNoSelect = 501,//生物进阶未选中
    CreatureAscendSelect = 502,//生物进阶选中

    SelectCreatureNoSelect = 601,//生物未选中
    SelectCreatureSelect = 602,//生物选中
}

//游戏战斗预制状态
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//拾取检测中，
    Droping = 2,//拾取中
}

//游戏场景(预制)
public enum GameSceneTypeEnum
{
    None = 0,
    BaseMain = 1,//基地主界面
    BaseGaming = 2,//基地游玩中(场景预制以这个为准)
    Fight = 3,//战斗
    RewardSelect = 4,//奖励选择
    DoomCouncil = 5,//终焉议会
}

public enum TestSceneTypeEnum
{
    None = 0,
    NormalGame = 1,//正常游戏启动(走真实开始流程，免去切换 GameScene)
    FightSceneTest = 2,//战斗场景测试
    CardTest = 3,//卡片效果测试
    Base = 4,//基地测试
    RewardSelect = 5,//奖励选择
    DoomCouncil = 6,//终焉议会
    NpcCreate = 7,//NPC创建
    ResearchUI = 8,//研究ui
    AbyssalBlessing = 9,//深渊馈赠UI
    CreatureSacrifice = 10,//生物献祭升级测试
    CreatureVat = 11,//魔物进阶(生物升阶容器)测试
}

/// <summary>
/// 战斗测试模式(战斗场景测试下的子模式)
/// </summary>
public enum FightTestModeEnum
{
    Normal = 0,//普通模式(自定义场景/敌人/BUFF的战斗测试)
    ConquerBoss = 1,//征服模式BOSS关(指定世界与难度，直接进入征服BOSS关)
}

public enum CinemachineCameraEnum
{
    None,
    Base,
    Fight,
}

//战斗类型
public enum GameFightTypeEnum
{
    None,
    Test,//测试模式
    Infinite,//无限模式
    Conquer,//征服模式
    DoomCouncil,//终焉议会
}

/// <summary>
/// 研究类型
/// </summary>
public enum ResearchInfoTypeEnum
{
    None = 0,//空
    Building = 1,//设施相关
    Strengthen = 2,//强化相关
    Creature = 3,//生物相关
    World = 4,//世界相关
}


/// <summary>
/// 终焉议会议案触发时机
/// </summary>
public enum TriggerTypeDoomCouncilEntityEnum
{
    WorldEnterGameForBaseScene,//进入游戏中 基地场景
    GameFightLogicEndGame,//战斗结束
    GameFightLogicAddExp,//战斗增加经验
    GameFightLogicDropAddCrystal,//战斗掉落增加水晶
}

/// <summary>
/// 重点解锁枚举-用于判断关键模块解锁
/// </summary>
public enum UnlockEnum : long
{
    CreatureVat = 100000000,//生物进阶
    CreatureVatAdd = 100000001,//生物进阶设置+1;
    CreatureVatBuffPreview = 100000006,//生物进阶-进阶增益BUFF数值范围预览(解锁后???替换成随机min~max范围;100000001~5被+1的Lv1~5占用故取6)
    CreatureVatAddProgress = 100000007,//生物进阶-魔晶加速研究(恒消耗1魔晶,研究等级=每次推进秒数=进度倍率;0级隐藏加速按钮,level_max=5)
    CreatureVatMaterialNum = 100000008,//生物进阶-素材魔物可选上限+1(每级+1,基础5,level_max=5,满级10)
    Altar = 100100001,//祭坛
    SacrificeNum = 100100002,//献祭祭品数量+1
    SacrificePityRate = 100100003,//献祭失败保底概率提升(每级失败累积的保底成功率+5%,满级+50%)
    SacrificeDifferentIdRate = 100100004,//不同生物id献祭成功率提升(每个不同id祭品单个成功率+5%,满级+50%)
    DoomCouncil = 100200001,//终焉议会模块
    ConquerReputationReward = 100200004,//征服通关获得声望(解锁后完整通关征服按难度reward_reputation增加玩家声望;前置=DoomCouncil)
    PortalShowNum = 100300001, //传送门显示数量
    PortalPreviewRoadNum = 100300002, //传送门详情-线路数量预览(研究门控)
    PortalPreviewFightNum = 100300003, //传送门详情-关卡数量预览(研究门控)
    PortalPreviewRoadLength = 100300004, //传送门详情-路径长度预览(研究门控)
    PortalPreviewReward = 100300005, //传送门详情-奖励预览(研究门控)
    PortalRefreshNum = 100300006, //传送门刷新次数(研究等级=可用刷新次数上限,通关世界回满)

    GashaponMachine = 100400000,//解锁孕育
    GashaponShowAll = 100400001,//显示所有抽取
    GashaponRarityR = 100401000,//稀有度R
    GashaponRarityRRate = 100401001,//稀有度R +1%
    GashaponRaritySR = 100402000,//稀有度SR    
    GashaponRaritySRRate = 100402001,//稀有度SR +1%
    GashaponRaritySSR = 100403000,//稀有度SSR    
    GashaponRaritySSRRate = 100403001,//稀有度SSR +1%
    GashaponRarityUR = 100404000,//稀有度UR
    GashaponRarityURRate = 100404001,//稀有度UR +1%
    GashaponRarityL = 100405000,//稀有度L
    GashaponRarityLRate = 100405001,//稀有度UR +1%

    LineupCreatureAddNum = 200000001,//解锁阵容生物上限1
    LineupNum = 200100001,//解锁多阵容
    DropCrystalLifeTime = 200200001,//魔晶掉落物存在时长提升(每级+5秒)
    DemonLordMPMax = 200300001,//魔王魔力上限提升(每级+10)
    DemonLordMPF = 200400001,//魔王魔力恢复速度提升(每级+1/秒)
    AbyssalBlessingRefreshNum = 200500001,//深渊馈赠刷新次数(研究等级=单次征服run内可用刷新次数上限,level_max=5)
    SpaceDash = 200600001,//空格突进(强化,level_max=3;1/2/3级分别向朝向突进1/2/3个距离单位)
    SpaceDashCD = 200700001,//空格突进冷却缩减(强化,level_max=4;默认3秒,每级-0.5秒,满级最低1秒;子研究,前置=SpaceDash)
    DemonLordAutoPickCrystal = 200800001,//魔王自动拾取魔晶(强化,level_max=10;拾取间隔=11-等级秒,10级10秒→满级1秒;每次拾取按FIFO取场上最先掉落的魔晶,基础1颗)
    DemonLordAutoPickCrystalNum = 200900001,//魔王每次拾取魔晶数量+1(强化,level_max=5;每次拾取数量=1+本研究等级;前置=DemonLordAutoPickCrystal)

    EquipRewardHuman = 300100301,//人类装备奖励
    EquipRewardSkeleton = 300200301,
    EquipRewardSlime = 300300301,
    EquipRewardSuccubus = 300400301,
    EquipRewardMinotaur = 300500301,
    EquipRewardGoblin = 300600301,
    EquipRewardOrc = 300700301,

    Achievement = 100500001,//成就系统

}

/// <summary>
/// 控制物体的交互枚举
/// </summary>
public enum ControlInteractionEnum
{
    None = 0,
    CoreInteraction,//核心交互
    PortalInteraction,//传送门交互
    DoomCouncilInteraction,//终焉议会交互
    DoomCouncilPodium,//终焉议会讲台
    Councilor,//议会成员
    AchievementInteraction,//成就石碑交互
    VatInteraction = 8,//魔物进阶容器交互(提示文本 textId=2000+值=2008;跳过7因2007已被"刷新次数已用完"占用)
}

/// <summary>
/// 排序筛选类型(用于 UIDialogOrderFilter 排序弹窗)。
/// 数值沿用生物列表历史排序约定(稀有度=1/等级=2/阵容=3/名字=4/同类=5),便于调用方映射各自的排序键。
/// </summary>
public enum OrderFilterTypeEnum
{
    Rarity = 1,//稀有度
    Level = 2,//等级
    Lineup = 3,//阵容
    Name = 4,//名字
    Class = 5,//同类(相同生物ID归并)
    Damage = 6,//造成的伤害
    Kill = 7,//击杀数
    DamageReceived = 8,//受到的伤害
    Exp = 9,//获得的经验
    ItemType = 10,//道具类型(命中置顶,多选;选项由调用方按上下文动态传入,如当前魔物的可装备类型)
}

