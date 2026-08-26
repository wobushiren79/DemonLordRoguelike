using System;
using System.Collections.Generic;
public partial class StoryInfoBean
{
    /// <summary>
    /// 获取触发类型
    /// </summary>
    public StoryTriggerTypeEnum GetTriggerType()
    {
        return (StoryTriggerTypeEnum)trigger_type;
    }

    /// <summary>
    /// 获取演出场景
    /// </summary>
    public StorySceneTypeEnum GetSceneType()
    {
        return (StorySceneTypeEnum)scene_type;
    }

    /// <summary>
    /// 获取触发条件
    /// </summary>
    public StoryTriggerConditionEnum GetTriggerCondition()
    {
        return (StoryTriggerConditionEnum)trigger_condition;
    }

    /// <summary>
    /// 是否只播一次
    /// </summary>
    public bool IsOnce()
    {
        return is_once == 1;
    }
}
public partial class StoryInfoCfg
{
    /// <summary>
    /// 获取指定触发条件的所有故事（按 priority 升序，同优先级按 id 升序保证稳定）
    /// </summary>
    public static List<StoryInfoBean> GetDataByCondition(StoryTriggerConditionEnum condition)
    {
        List<StoryInfoBean> list = new List<StoryInfoBean>();
        var arrayData = GetAllArrayData();
        for (int i = 0; i < arrayData.Length; i++)
        {
            StoryInfoBean itemData = arrayData[i];
            if (itemData.GetTriggerCondition() == condition)
            {
                list.Add(itemData);
            }
        }
        list.Sort((a, b) =>
        {
            int compare = a.priority.CompareTo(b.priority);
            return compare != 0 ? compare : a.id.CompareTo(b.id);
        });
        return list;
    }
}
