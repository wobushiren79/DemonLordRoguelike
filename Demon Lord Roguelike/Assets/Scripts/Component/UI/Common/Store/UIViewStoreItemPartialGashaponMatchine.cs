

using System;
using System.Collections.Generic;
using System.Text;

public partial class UIViewStoreItem
{
    private StoreGashaponMachineInfoBean storeGashaponMachineInfoData;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(int storeIndex, StoreGashaponMachineInfoBean itemData, Action<int> actionForOnClickBuy)
    {
        this.storeIndex = storeIndex;
        this.storeGashaponMachineInfoData = itemData;
        this.actionForOnClickBuy = actionForOnClickBuy;

        string name = storeGashaponMachineInfoData.name_language;
        SetName(name);
        SetPrice(storeGashaponMachineInfoData.pay_crystal);
        SetIcon(storeGashaponMachineInfoData.icon_res);
        SetContentShow();
    }

    /// <summary>
    /// 设置孕育详情弹窗:列出本扭蛋可抽到的生物及各稀有度的实际抽中概率,稀有度文本带对应颜色
    /// </summary>
    private void SetContentShow()
    {
        if (ui_ContentShow == null)
        {
            return;
        }
        //稀有度概率与具体抽到哪只生物无关,所有生物共用同一份概率文本
        var listRarityProbability = GashaponItemBean.GetRarityProbabilityList();
        string rarityText = BuildRarityProbabilityText(listRarityProbability);

        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        var listCreatureId = storeGashaponMachineInfoData.GetCreatureIds();
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < listCreatureId.Count; i++)
        {
            var creatureInfo = CreatureInfoCfg.GetItemData(listCreatureId[i]);
            if (creatureInfo == null)
            {
                continue;
            }
            //与实际抽取一致:仅展示职业已解锁的生物(unlock_id 未解锁的抽不到也不展示)
            if (!userUnlock.CheckIsUnlock(creatureInfo.unlock_id))
            {
                continue;
            }
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append("\n");
            }
            //每行:生物名:普通X% 稀有Y%...
            stringBuilder.Append($"{creatureInfo.name_language}:{rarityText}");
        }
        ui_ContentShow.SetData(stringBuilder.ToString(), PopupEnum.Text);
    }

    /// <summary>
    /// 构建稀有度概率文本:"普通50% 稀有10%",每个稀有度名+百分比整体按其配置颜色(ui_board_color)着色
    /// </summary>
    private string BuildRarityProbabilityText(List<KeyValuePair<RarityEnum, float>> listRarityProbability)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var itemRarity in listRarityProbability)
        {
            var rarityInfo = RarityInfoCfg.GetItemData(itemRarity.Key);
            if (rarityInfo == null)
            {
                continue;
            }
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(" ");
            }
            //百分比保留整数(GetPercentage 第二参为小数位数)
            float percentage = MathUtil.GetPercentage(itemRarity.Value, 0);
            string segment = $"{rarityInfo.name_language}{percentage}%";
            //ui_board_color 为"主色,暗色"渐变对,取主色(首段)作为 TMP 颜色标签包裹整段
            string colorHex = rarityInfo.ui_board_color?.Split(',')[0];
            if (!string.IsNullOrEmpty(colorHex))
            {
                stringBuilder.Append($"<color={colorHex}>{segment}</color>");
            }
            else
            {
                stringBuilder.Append(segment);
            }
        }
        return stringBuilder.ToString();
    }
}