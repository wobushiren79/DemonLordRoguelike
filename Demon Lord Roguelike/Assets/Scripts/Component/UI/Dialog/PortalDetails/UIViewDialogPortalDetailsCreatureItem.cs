using UnityEngine;

/// <summary>
/// 传送门详情弹窗-来袭魔物item（挑战100勇士模式专用：显示冻结配置行的一个魔物 Spine 图标，由 UIDialogPortalDetails 动态实例化）
/// </summary>
public partial class UIViewDialogPortalDetailsCreatureItem : BaseUIView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="npcId">魔物npcInfoId</param>
    public void SetData(long npcId)
    {
        var npcInfo = NpcInfoCfg.GetItemData(npcId);
        if (npcInfo == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        //设置 Spine 图标（Simple 管线：不含武器/场景）
        var creatureData = new CreatureBean(npcInfo);
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
    }
}
