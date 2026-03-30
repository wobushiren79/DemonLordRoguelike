using NUnit.Framework.Interfaces;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
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
        if (inputType == InputActionUIEnum.ESC)
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
        else if (viewButton == ui_BtnAddCoin)
        {
            OnClickForAddCrystal();
        }
        else if (viewButton == ui_BtnAddReputation)
        {
            OnClickForAddReputation();
        }
        else if (viewButton == ui_BtnAddItem)
        {
            OnClickForAddItem();
        }
        else if (viewButton == ui_BtnAddAllCreature)
        {
            OnClickForAddAllCreature();
        }
        else if (viewButton == ui_BtnAddTestCreature)
        {
            OnClickForAddTestCreature();
        }
        else if (viewButton == ui_BtnAddUnlock)
        {
            OnClickForAddUnlock();
        }
    }

    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    /// <summary>
    /// 点击增加魔晶
    /// </summary>
    public void OnClickForAddCrystal()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        string inputData = ui_InputData.text;
        if (inputData.IsNull())
        {
            userData.AddCrystal(999999);
        }
        else
        {
            if (long.TryParse(inputData, out var addCoin))
            {
                userData.AddCrystal(addCoin);
            }
            else
            {
                LogUtil.LogError("请输入数字");
            }
        }
        UIHandler.Instance.ToastHintText("添加成功！",1);
        GameDataHandler.Instance.manager.SaveUserData();
    }

    /// <summary>
    /// 点击添加声望
    /// </summary>
    public void OnClickForAddReputation()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        string inputData = ui_InputData.text;
        if (inputData.IsNull())
        {
            userData.AddReputation(999999);
        }
        else
        {
            if (long.TryParse(inputData, out var addCoin))
            {
                userData.AddReputation(addCoin);
            }
            else
            {
                LogUtil.LogError("请输入数字");
            }
        }
        UIHandler.Instance.ToastHintText("添加成功！",1);
        GameDataHandler.Instance.manager.SaveUserData();
    }

    /// <summary>
    /// 点击添加道具
    /// </summary>
    public void OnClickForAddItem()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        string inputData = ui_InputData.text;
        if (inputData.IsNull())
        {
            var allData = ItemsInfoCfg.GetAllData();
            foreach(var itemData in allData)
            {
                userData.AddBackpackItem(itemData.Value.id);
            }
        }
        else
        {
            if (long.TryParse(inputData, out var itemId))
            {
                userData.AddBackpackItem(itemId);
            }
            else
            {
                LogUtil.LogError("请输入数字");
            }
        }
        UIHandler.Instance.ToastHintText("添加成功！",1);
        GameDataHandler.Instance.manager.SaveUserData();
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
            creatureData.rarity = Random.Range(1, 7);
            creatureData.level = 0;
            creatureData.AddSkinForBase();
            userData.AddBackpackCreature(creatureData);
        }

        UIHandler.Instance.ToastHintText("添加成功！",1);
        GameDataHandler.Instance.manager.SaveUserData();
    }

    /// <summary>
    /// 添加测试生物
    /// </summary>
    public void OnClickForAddTestCreature()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        string inputData = ui_InputData.text;
        if (inputData.IsNull() || !long.TryParse(inputData, out long targetId))
        {
            LogUtil.LogError("添加测试生物失败，请输入生物ID");
            return;
        }
        CreatureBean creatureData = new CreatureBean(targetId);
        creatureData.rarity = Random.Range(1, 7);
        creatureData.starLevel = Random.Range(0, 11);
        creatureData.level = Random.Range(0, 101);
        creatureData.AddSkinForBase();
        userData.AddBackpackCreature(creatureData);

        UIHandler.Instance.ToastHintText("添加成功！",1);
        GameDataHandler.Instance.manager.SaveUserData();
    }

    /// <summary>
    /// 添加解锁信息
    /// </summary>
    public void OnClickForAddUnlock()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlockData = userData.GetUserUnlockData();
        string inputData = ui_InputData.text;
        if (inputData.IsNull())
        {
            var all = UnlockInfoCfg.GetAllData();
            foreach (var item in all)
            {
                var researchInfo = ResearchInfoCfg.GetItemDataByUnlockId(item.Key);
                if (researchInfo == null)
                {
                    userUnlockData.AddUnlock(item.Key);
                }
                else
                {
                    userUnlockData.AddUnlock(item.Key, researchInfo.level_max);
                }
            }
            LogUtil.Log($"解锁所有成功");
            return;
        }
        if (long.TryParse(inputData, out long outValue))
        {
            var researchInfo = ResearchInfoCfg.GetItemDataByUnlockId(outValue);
            if(researchInfo == null)
            {
                userUnlockData.AddUnlock(outValue);
            }
            else
            {
                userUnlockData.AddUnlock(outValue, researchInfo.level_max);  
            }
            LogUtil.Log("添加成功");
        }
        else
        {
            LogUtil.LogError("输入数据错误 必须是long类型");
            return;
        }
        GameDataHandler.Instance.manager.SaveUserData();
    }
}