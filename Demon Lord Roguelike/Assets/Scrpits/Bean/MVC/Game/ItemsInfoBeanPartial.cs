using System;
using System.Collections.Generic;
using UnityEngine;
public partial class ItemsInfoBean
{
    public Dictionary<ItemsInfoAttackModeDataEnum,string> dicAttackModeData;

    /// <summary>
    /// 获取道具类型
    /// </summary>
    public ItemTypeEnum GetItemType()
    {
        return (ItemTypeEnum)item_type;
    }

    /// <summary>
    /// 处理攻击模块数据
    /// </summary>
    public void HandleItemsInfoAttackModeData(BaseAttackMode attackMode)
    {
        if (dicAttackModeData == null)
        {
            dicAttackModeData = attack_mode_data.SplitForDictionary<ItemsInfoAttackModeDataEnum>();
        }
        //先还原一下预制
        if (attackMode.spriteRenderer != null)
        {
            attackMode.spriteRenderer.transform.localScale = Vector3.one;
        }
        bool isShowSprite = false;
        foreach (var item in dicAttackModeData)
        {
            switch (item.Key)
            {
                case ItemsInfoAttackModeDataEnum.VertexRotateAxis:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemVertexRotateAxis = item.Value.SplitForVector3(',');
                        attackMode.spriteRenderer.material.SetVector("_VertexRotateAxis", itemVertexRotateAxis);
                    }
                    break;
                case ItemsInfoAttackModeDataEnum.VertexRotateSpeed:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemVertexRotateSpeed = float.Parse(item.Value);
                        attackMode.spriteRenderer.material.SetFloat("_VertexRotateSpeed", itemVertexRotateSpeed);
                    }
                    break;
                case ItemsInfoAttackModeDataEnum.ShowSprite:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemShowSprite = item.Value;
                        IconHandler.Instance.SetItemIconForAttackMode(itemShowSprite, attackMode.spriteRenderer);
                        isShowSprite = true;
                    }
                    break;
                case ItemsInfoAttackModeDataEnum.StartPosition:
                    if (attackMode.gameObject != null)
                    {
                        var itemStartPosition = item.Value.SplitForVector3(',');
                        attackMode.gameObject.transform.position += itemStartPosition;
                    }
                    break;
                case ItemsInfoAttackModeDataEnum.StartSize:
                    if (attackMode.gameObject != null)
                    {
                        var itemStartSize = float.Parse(item.Value);
                        attackMode.spriteRenderer.transform.localScale = Vector3.one * itemStartSize;
                    }
                    break;
            }
        }
        //是否有展示精灵 如果没有需要展示？
        if (!isShowSprite)
        {
            IconHandler.Instance.GetUnKnowSprite((targetSprite) =>
            {
                if (attackMode.spriteRenderer != null)
                {
                    attackMode.spriteRenderer.sprite = targetSprite;
                }
            });
        }
    }
}
public partial class ItemsInfoCfg
{
    public static Dictionary<long,List<ItemsInfoBean>> dicDataForCreatureModel;

    /// <summary>
    /// 根据生物模型获取数据
    /// </summary>
    /// <param name="creatureModelId"></param>
    /// <returns></returns>
    public static List<ItemsInfoBean> GetDataByCreatureModelId(long creatureModelId)
    {
        if (dicDataForCreatureModel == null)
        {  
            dicDataForCreatureModel = new Dictionary<long, List<ItemsInfoBean>>();
            var allData = GetAllArrayData();
            for (int i = 0; i < allData.Length; i++)
            {
                var itemData = allData[i];
                var creatureModelInfo = CreatureModelInfoCfg.GetItemData(itemData.creature_model_info_id);
               
                if (dicDataForCreatureModel.ContainsKey(creatureModelInfo.model_id))
                {
                    dicDataForCreatureModel[creatureModelInfo.model_id].Add(itemData);
                }
                else
                {
                    dicDataForCreatureModel.Add(creatureModelInfo.model_id, new List<ItemsInfoBean>() { itemData });
                }
            }
        }
        if (dicDataForCreatureModel.TryGetValue(creatureModelId, out List<ItemsInfoBean> valueData))
        {
            return valueData;
        }
        return null;    
    }
}
