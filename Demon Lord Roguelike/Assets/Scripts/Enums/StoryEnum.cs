/// <summary>
/// 故事触发类型（故事大类；触发判定逻辑集中在 StoryHandler.TryTriggerStory）
/// </summary>
public enum StoryTriggerTypeEnum
{
    None = 0,
    Guide = 1,//引导
    Plot = 2,//剧情(预留)
}

/// <summary>
/// 故事演出场景（播放演出时所在的场景）
/// </summary>
public enum StorySceneTypeEnum
{
    None = 0,
    Base = 1,//基地
    Fight = 2,//战斗
    DoomCouncil = 3,//终焉议会
}

/// <summary>
/// 故事触发条件（具体判定钩子；新增条件时在 StoryHandler.InitData 补事件注册、TryTriggerStory 补分支）
/// </summary>
public enum StoryTriggerConditionEnum
{
    None = 0,
    EnterBaseSceneFirst = 1,//首次进入基地场景后
    EnterFightSceneFirst = 2,//首次进入战斗场景后
    FightFirstDropCrystal = 3,//战斗中首次掉落魔晶
    //EnterDoomCouncilFirst = 4,//首次进入终焉议会(预留,第二期接 EnterDoomCouncilScene 链尾事件)
}

/// <summary>
/// 故事演出步骤类型（param_1~4 语义随类型而定，详见 StoryDetailsInfoBeanPartial 注释与 excel_story_details_info 表头）
/// </summary>
public enum StoryStepTypeEnum
{
    None = 0,
    Talk = 1,//对话(param_1=对话ID,&分隔连播)
    CameraMove = 2,//镜头移动(param_1=目标标记/back, param_2=时长秒, param_3=缓动序号)
    Wait = 3,//等待(param_1=秒,实时)
    Effect = 4,//特效(param_1=特效ID, param_2=目标标记空=核心/魔王, param_3=尺寸倍率)
    Audio = 5,//音效(param_1=音效ID)
    Fade = 6,//淡入淡出(param_1=out淡出/in淡入, param_2=时长秒)
}
