

using System.Collections.Generic;
using System.Linq;

public partial class UIBaseStore : BaseUIComponent
{
    List<UIViewBaseStoreItem> listStoreItemView = new List<UIViewBaseStoreItem>();
    public override void CloseUI()
    {
        base.CloseUI();
        ClearData(true);
    }


    public override void OpenUI()
    {
        base.OpenUI();
        InitStoreItems(StoreInfoTypeEnum.Strengthen);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void InitStoreItems(StoreInfoTypeEnum storeType)
    {        
        ClearData(false);
        List<StoreInfoBean> listData = StoreInfoCfg.GetStoreInfoByType(storeType);

        listData.ForEach((index, itemData) =>
        {
            UIViewBaseStoreItem itemView;
            if (index < listStoreItemView.Count)
            {
                 itemView = listStoreItemView[index];
            }
            else
            {
                var newStoreItemObj = Instantiate(ui_Content.gameObject, ui_UIViewBaseStoreItem.gameObject);
                itemView =  newStoreItemObj.GetComponent<UIViewBaseStoreItem>();
            }
            itemView.gameObject.SetActive(true);
            itemView.SetData(itemData);
        });
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    /// <param name="isDestory"></param>
    public void ClearData(bool isDestory)
    {
        listStoreItemView.ForEach((index, itemView) =>
        {
            if (isDestory)
            {
                DestroyImmediate(itemView.gameObject);
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        });
        if (isDestory)
        {
            listStoreItemView.Clear();
        }
    }
}