using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 终焉议会议案效果：魔物稀有度下降
/// <para>class_entity_data: "1"=稀有度下降1级(下限N级); "0"=稀有度直接归0(降到N级)</para>
/// <para>通过后弹出魔物选择窗，对选中的1只魔物执行降稀有度：移除高于新稀有度档位的稀有度BUFF(dicRarityBuff)</para>
/// </summary>
public class DoomCouncilEntityCreatureRarityDown : DoomCouncilBaseEntity
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
        //稀有度已是N级(最低)的魔物无法下降，不出现在可选列表中
        dialogSelectCreatureData.filterCreature = creature => creature.rarity > (int)RarityEnum.N;
        dialogSelectCreatureData.actionSubmit = (selectView, selectData) =>
        {
            var targetSelectView = selectView as UIDialogSelectCreature;
            if (!targetSelectView.listSelect.IsNull())
            {
                ApplyRarityDown(targetSelectView.listSelect[0]);
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
    /// 对目标魔物执行稀有度下降并保存
    /// </summary>
    /// <param name="targetCreature">目标魔物</param>
    protected void ApplyRarityDown(CreatureBean targetCreature)
    {
        //稀有度已是N级时无法再下降
        if (targetCreature.rarity <= (int)RarityEnum.N)
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000009), 1);
            BackDoomCouncilBill();
            return;
        }
        //"0"=归0(降到N级)，其余=下降1级
        if (doomCouncilInfo.class_entity_data.Equals("0"))
        {
            targetCreature.rarity = (int)RarityEnum.N;
        }
        else
        {
            targetCreature.rarity = Mathf.Max((int)RarityEnum.N, targetCreature.rarity - 1);
        }
        //移除高于新稀有度档位的稀有度BUFF
        var listRemoveKey = new List<RarityEnum>();
        foreach (var item in targetCreature.dicRarityBuff)
        {
            if ((int)item.Key > targetCreature.rarity)
            {
                listRemoveKey.Add(item.Key);
            }
        }
        foreach (var key in listRemoveKey)
        {
            targetCreature.dicRarityBuff.Remove(key);
        }
        //保存数据
        GameDataHandler.Instance.manager.SaveUserData();
        //弹出提示
        UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000007), 1);
        BackDoomCouncilBill();
    }
    #endregion
}
