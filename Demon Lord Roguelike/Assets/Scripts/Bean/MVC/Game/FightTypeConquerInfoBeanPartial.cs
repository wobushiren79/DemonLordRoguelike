using System;
using System.Collections.Generic;
public partial class FightTypeConquerInfoBean
{
    #region 临时配置字段（待 Unity 导出工具重新生成 Bean.cs 后删除）
    // 说明：以下字段已加入 Excel 源表(excel_fight_type_conquer_info)，
    // 但因 Bean.cs 为自动生成且 hook 禁止手改，临时声明在此以便编译/反序列化。
    // 待在 Unity 编辑器运行配置导出工具(ExcelEditorWindow)重新生成 Bean.cs 后，
    // reward_exp / reward_exp_boss 会出现在 Bean.cs，届时必须删除此 region，否则字段重复编译报错。
    /// <summary>
    /// 奖励-普通关卡经验
    /// </summary>
    public int reward_exp;
    /// <summary>
    /// 奖励-BOSS关卡经验
    /// </summary>
    public int reward_exp_boss;
    #endregion

    protected long[] fightSceneIds;
    protected long[] fightSceneBossIds;
    protected long[] emenyIds;
    protected long[] emenyBossIds;

    /// <summary>
    /// 获取随机战斗场景
    /// </summary>
    public long GetRandomFightScene(bool isBoss)
    {
        long[] targetIds;
        if (isBoss)
        {
            targetIds = fightSceneBossIds;
        }
        else
        {
            targetIds = fightSceneIds;
        }
        if (targetIds == null)
        {

            if (isBoss)
            {
                targetIds = fight_scene_boss_ids.SplitForArrayLong('&');
            }
            else
            {
                targetIds = fight_scene_ids.SplitForArrayLong('&');
            }
        }
        return targetIds.GetRandomData();
    }

    /// <summary>
    /// 获取战斗敌人数据
    /// </summary>
    /// <param name="isBoss"></param>
    public long GetRandomEmenyId(bool isBoss)
    {
        long[] targetIds;
        if (isBoss)
        {
            targetIds = emenyBossIds;
        }
        else
        {
            targetIds = emenyIds;
        }
        if (targetIds == null)
        {

            if (isBoss)
            {
                targetIds = enemy_boss_ids.SplitForArrayLong('&');
            }
            else
            {
                targetIds = enemy_ids.SplitForArrayLong('&');
            }
        }
        return targetIds.GetRandomData();
    }
}
public partial class FightTypeConquerInfoCfg
{

    public static FightTypeConquerInfoBean GetItemData(long worldId, int difficultyLevel)
    {
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            FightTypeConquerInfoBean fightTypeConquerInfo = itemData.Value;
            if (fightTypeConquerInfo.world_id == worldId && fightTypeConquerInfo.level == difficultyLevel)
            {
                return fightTypeConquerInfo;
            }
        }
        return null;
    }
}
