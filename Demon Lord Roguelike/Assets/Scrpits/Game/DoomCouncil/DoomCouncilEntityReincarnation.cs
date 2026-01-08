using System;
using System.Collections.Generic;

public class DoomCouncilEntityReincarnation : DoomCouncilBaseEntity
{

    public override bool TriggerFirst()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UICreatureChange>();
        List<long> listSelectCreature = new List<long>()
        {
            long.Parse(doomCouncilInfo.class_entity_data)
        };
        Action<CreatureBean> actionForComplete = (selectCreatureData) =>
        {  
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            //清空装备
            userData.selfCreature.RemoveAllEquipToBackpack();
            //设置魔王
            userData.selfCreature.ClearSkin();
            //设置魔王皮肤 
            List<long> listSkin = new List<long>();
            foreach (var itemSkin in selectCreatureData.dicSkinData)
            {
                listSkin.Add(itemSkin.Value.skinId);
            }
            //设置魔王
            userData.selfCreature.creatureId = selectCreatureData.creatureId;
            userData.selfCreature.InitSkin(listSkin);
            //保存数据
            GameDataHandler.Instance.manager.SaveUserData(userData);
            //弹出提示
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000005), 1);
            BackDoomCouncilMain();
        };
        Action actionForCancel = () => 
        {
            BackDoomCouncilMain(); 
        };
        string contentStr = TextHandler.Instance.GetTextById(63001);
        targetUI.SetData(listSelectCreature, actionForComplete, actionForCancel, contentStr: contentStr);
        return true;
    }

}