using UnityEngine;

/// <summary>
/// 终焉议会议案效果：魔物等级下降
/// <para>class_entity_data: "1"=等级下降1级(下限0级); "0"=等级直接归0</para>
/// <para>通过后弹出魔物选择窗，对选中的1只魔物执行降级：当前等级经验清零、已分配的升级属性点清空(供重新分配，创建时加成不动)</para>
/// </summary>
public class DoomCouncilEntityCreatureLevelDown : DoomCouncilBaseEntity
{
    #region 触发
    /// <summary>
    /// 首次添加时触发：打开魔物选择窗
    /// </summary>
    /// <returns>true=立即型议案，不进入常驻列表</returns>
    public override bool TriggerFirst()
    {
        UIHandler.Instance.CloseAllUI();
        DialogSelectCreatureBean dialogSelectCreatureData = new DialogSelectCreatureBean();
        dialogSelectCreatureData.selectNumMax = 1;
        //等级已是0级(最低)的魔物无法下降，不出现在可选列表中
        dialogSelectCreatureData.filterCreature = creature => creature.level > 0;
        dialogSelectCreatureData.actionSubmit = (selectView, selectData) =>
        {
            var targetSelectView = selectView as UIDialogSelectCreature;
            if (!targetSelectView.listSelect.IsNull())
            {
                ApplyLevelDown(targetSelectView.listSelect[0]);
            }
            else
            {
                BackDoomCouncilBill();
            }
        };
        dialogSelectCreatureData.actionCancel = (view, data) =>
        {
            BackDoomCouncilBill();
        };
        UIHandler.Instance.ShowDialogSelectCreature(dialogSelectCreatureData);
        return true;
    }
    #endregion

    #region 效果执行
    /// <summary>
    /// 对目标魔物执行等级下降并保存
    /// </summary>
    /// <param name="targetCreature">目标魔物</param>
    protected void ApplyLevelDown(CreatureBean targetCreature)
    {
        //等级已是0级时无法再下降
        if (targetCreature.level <= 0)
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000008), 1);
            BackDoomCouncilBill();
            return;
        }
        //"0"=归0，其余=下降1级
        if (doomCouncilInfo.class_entity_data.Equals("0"))
        {
            targetCreature.level = 0;
        }
        else
        {
            targetCreature.level = Mathf.Max(0, targetCreature.level - 1);
        }
        //当前等级经验清零，已分配升级属性点清空(供重新分配)
        targetCreature.levelExp = 0;
        targetCreature.creatureAttribute.dicAttributeLevelUp.Clear();
        //保存数据
        GameDataHandler.Instance.manager.SaveUserData();
        //弹出提示
        UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000006), 1);
        BackDoomCouncilBill();
    }
    #endregion
}
