using System;
using System.Collections.Generic;

/// <summary>
/// 用户故事演出数据存档Bean
/// 保存玩家已播放的故事演出记录（StoryInfo.id），以及后续故事系统的其它运行时状态
/// 已拆分为独立存档 UserStory_{slot}（不随 UserData 主档序列化，由 UserDataService 在加载/保存时注入与落盘）
/// </summary>
[Serializable]
public class UserStoryBean
{
    #region 数据字段

    /// <summary>
    /// 已播放的故事记录
    /// Key：故事ID（StoryInfo.id）
    /// Value：播放完成时间戳（DateTime.Ticks，仅作审计/调试参考）
    /// 用字典而非列表：后续故事/事件多了查询仍是 O(1)
    /// </summary>
    public Dictionary<long, long> dicPlayedStory = new Dictionary<long, long>();

    #endregion

    #region 数据获取

    /// <summary>
    /// 获取已播放故事记录（旧存档缺该字段时兜底懒初始化）
    /// </summary>
    public Dictionary<long, long> GetDicPlayedStory()
    {
        if (dicPlayedStory == null)
            dicPlayedStory = new Dictionary<long, long>();
        return dicPlayedStory;
    }

    #endregion

    #region 播放记录操作

    /// <summary>
    /// 检测故事是否已播放过
    /// </summary>
    /// <param name="storyId">故事ID（StoryInfo.id）</param>
    public bool IsStoryPlayed(long storyId)
    {
        return GetDicPlayedStory().ContainsKey(storyId);
    }

    /// <summary>
    /// 标记故事已播放（重复标记只刷新时间戳，不产生重复条目）
    /// </summary>
    /// <param name="storyId">故事ID（StoryInfo.id）</param>
    public void MarkStoryPlayed(long storyId)
    {
        GetDicPlayedStory()[storyId] = DateTime.Now.Ticks;
    }

    #endregion
}
