using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UITestBase : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.manager.EnableAllControl(false);
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType== InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnAddAllCreature)
        {
            OnClickForAddAllCreature();
        }
    }

    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        GameControlHandler.Instance.SetBaseControl();
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    /// <summary>
    /// 点击添加所有生物
    /// </summary>
    public void OnClickForAddAllCreature()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var allCreature = CreatureInfoCfg.GetAllData();
        foreach (var itemData in allCreature)
        {
            var itemCreatureInfo = itemData.Value;
            CreatureBean creatureData = new CreatureBean(itemCreatureInfo.id);
            creatureData.rarity = Random.Range(1,7);
            creatureData.AddAllSkin();
            creatureData.creatureId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
            userData.AddBackpackCreature(creatureData);
        }

        UIHandler.Instance.ToastHint<ToastView>("添加成功！");
    }
}