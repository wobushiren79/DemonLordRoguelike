using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏设置-Mods页签
/// 列出 Mods 目录下所有可用Mod，每个Mod一个开关；新Mod默认关闭，改动在重启游戏后生效
/// </summary>
public class UIGameSettingForMods : UIGameSettingBase
{
    /// <summary>
    /// 当前列出的Mod名（与 listModCheckBox 顺序一致）
    /// </summary>
    protected List<string> listModName = new List<string>();

    /// <summary>
    /// 每个Mod对应的开关控件（与 listModName 顺序一致）
    /// </summary>
    protected List<UIViewGameSettingCheckBox> listModCheckBox = new List<UIViewGameSettingCheckBox>();

    public UIGameSettingForMods(GameObject objListContainer) : base(objListContainer)
    {

    }

    #region 页签开关

    /// <summary>
    /// 打开页签：扫描Mods目录列出所有可用Mod，并按存档中的开启状态初始化开关
    /// </summary>
    public override void Open()
    {
        base.Open();
        listModCheckBox.Clear();
        listModName = ModHandler.Instance.GetAvailableModNames();
        for (int i = 0; i < listModName.Count; i++)
        {
            string modName = listModName[i];
            var checkBox = CreatureItemForCheckBox(modName);
            checkBox.SetSelect(gameConfig.IsModEnable(modName));
            listModCheckBox.Add(checkBox);
        }
    }

    #endregion

    #region 控件回调

    /// <summary>
    /// 开关值变化：写入开启列表并落盘，提示重启游戏后生效
    /// </summary>
    public override void ActionForCheckBoxValueChange(UIViewGameSettingCheckBox targetView, bool isCheck)
    {
        base.ActionForCheckBoxValueChange(targetView, isCheck);
        int index = listModCheckBox.IndexOf(targetView);
        if (index < 0 || index >= listModName.Count)
            return;
        //开启状态写入GameConfig并立即保存，避免未关界面退出导致丢失
        gameConfig.SetModEnable(listModName[index], isCheck);
        GameDataHandler.Instance.manager.SaveGameConfig();
        //Mod的Catalog与JsonText合并发生在启动阶段且不可逆，运行中无法完整热切换，统一提示重启生效
        UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(43003), 1);
    }

    #endregion
}
