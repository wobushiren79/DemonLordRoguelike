

using UnityEngine.UI;

public partial class UITestNpcCreate : BaseUIComponent
{
    public override void Awake()
    {
        base.Awake();
                
        //测试标准模型
        CreatureBean creatureNormalTest = new CreatureBean(2001);
        creatureNormalTest.AddSkinForBase();

        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_NormalModel, creatureNormalTest);

        SpineHandler.Instance.PlayAnim(ui_NormalModel, SpineAnimationStateEnum.Idle, creatureNormalTest, true);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_LoadBtn)
        {
            OnClickForLoadNpc();
        }
    }

    /// <summary>
    /// 点击加载NPC
    /// </summary>
    public void OnClickForLoadNpc()
    {
        int npcId = int.Parse(ui_LoadInput.text);
        NpcInfoBean npcInfoData = NpcInfoCfg.GetItemData(npcId);
        CreatureBean creatureData = new CreatureBean(npcInfoData);
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_TargetModel, creatureData);
        //播放待机动画
        SpineHandler.Instance.PlayAnim(ui_TargetModel, SpineAnimationStateEnum.Idle, creatureData, true);
    }
}