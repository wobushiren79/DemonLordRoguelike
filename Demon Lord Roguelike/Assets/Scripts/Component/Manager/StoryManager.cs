using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 故事演出管理器
/// 纯状态容器：初始化标记/演出播放状态/当前故事数据/暂停与镜头缓存/演出取消源
/// </summary>
public class StoryManager : BaseManager
{
    /// <summary>是否已经初始化(Handler 完成事件注册)</summary>
    public bool isInited;
    /// <summary>演出是否播放中(防重入;第一期不排队,播放中来的触发直接丢弃)</summary>
    public bool isStoryPlaying;
    /// <summary>当前播放中的故事配置</summary>
    public StoryInfoBean currentStoryData;
    /// <summary>演出前 Time.timeScale 缓存(战斗演出暂停/还原用)</summary>
    public float timeScaleOrigin = 1f;
    /// <summary>演出起始镜头目标位(back 标记解析与结束归还镜头用)</summary>
    public Vector3 storyCameraOriginPos;
    /// <summary>演出统一取消源(懒创建一次复用,链接 Handler gameObject 销毁自动取消)</summary>
    public GTaskCancel cancelForStory;

    /// <summary>各触发条件的候选故事缓存(配置表静态不变,首次用到时构建;避免掉晶等高频事件每次重建列表+排序)</summary>
    public Dictionary<StoryTriggerConditionEnum, List<StoryInfoBean>> dicConditionStories;
    /// <summary>各触发条件的耗尽标记(该条件候选全部为只播一次且已播完,事件直接秒退)</summary>
    public HashSet<StoryTriggerConditionEnum> setExhaustedCondition = new HashSet<StoryTriggerConditionEnum>();
    /// <summary>耗尽标记构建时对应的用户故事数据实例(切换存档槽后实例变更,据此自动重建标记防止误伤新档)</summary>
    public UserStoryBean exhaustedForStoryData;
}
