using System;
using System.Collections.Generic;

public partial class CreatureBean
{
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    public int order;//排序

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    //生物状态 默认闲置
    public CreatureStateEnum creatureState = CreatureStateEnum.Idle;

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    //生物状态持续时间
    public float creatureStateTimeUpdate = 0;

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureInfoBean _creatureInfo;

    [Newtonsoft.Json.JsonIgnore]
    public CreatureInfoBean creatureInfo
    {
        get
        {
            if(_creatureInfo == null)
            {
                _creatureInfo = CreatureInfoCfg.GetItemData(id);
                if(_creatureInfo == null)
                {
                    LogUtil.LogError($"获取CreatureInfoBean失败 id_{id}");
                }
            }
            return _creatureInfo;
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureModelBean _creatureModel;

    [Newtonsoft.Json.JsonIgnore]
    public CreatureModelBean creatureModel
    {
        get
        {
            if (_creatureModel == null)
            {
                _creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
            }
            return _creatureModel;
        }
    }

    /// <summary>
    /// 清理临时数据
    /// </summary>
    public void ClearTempData()
    {
        order = 0;
        creatureStateTimeUpdate = 0;
        creatureState = CreatureStateEnum.Idle;
    }

}
