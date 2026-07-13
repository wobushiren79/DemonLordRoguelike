using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
public partial class ItemsInfoBean
{
    public Dictionary<ItemInfoAttackModeDataEnum, string> dicAttackModeData;

    // 奖励可出的稀有度白名单缓存（解析自 reward_rarity 逗号串；null 表示尚未解析）
    private List<int> listRewardRarityCache;

    /// <summary>
    /// 获取道具类型
    /// </summary>
    public ItemTypeEnum GetItemType()
    {
        return (ItemTypeEnum)item_type;
    }

    /// <summary>
    /// 获取「奖励可出稀有度白名单」列表（解析 reward_rarity 逗号串，结果缓存）。
    /// 空/未配置返回空列表，表示全稀有度适配。
    /// </summary>
    public List<int> GetRewardRarityList()
    {
        if (listRewardRarityCache != null)
            return listRewardRarityCache;
        listRewardRarityCache = new List<int>();
        if (string.IsNullOrEmpty(reward_rarity))
            return listRewardRarityCache;
        string[] parts = reward_rarity.Split(',');
        foreach (var part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;
            if (int.TryParse(trimmed, out int rarity) && !listRewardRarityCache.Contains(rarity))
                listRewardRarityCache.Add(rarity);
        }
        return listRewardRarityCache;
    }

    /// <summary>
    /// 该道具是否可作为指定稀有度的奖励产出。
    /// reward_rarity 未配置(空)=全稀有度适配返回 true；配置了则仅白名单内的稀有度返回 true。
    /// </summary>
    /// <param name="rarity">目标稀有度ID(RarityEnum: N=1~L=6)</param>
    public bool IsMatchRewardRarity(int rarity)
    {
        var listRarity = GetRewardRarityList();
        if (listRarity.Count == 0)
            return true;
        return listRarity.Contains(rarity);
    }

    /// <summary>
    /// 获取武器类型（只有武器类型道具有效）
    /// </summary>
    public ItemTypeWeaponEnum GetWeaponType()
    {
        return (ItemTypeWeaponEnum)item_weapon_type;
    }

    /// <summary>
    /// 判断生物是否可以装备该道具
    /// </summary>
    /// <param name="creatureInfo">生物配置信息</param>
    /// <returns>是否可以装备</returns>
    public bool CanEquipForCreature(CreatureInfoBean creatureInfo)
    {
        if (creatureInfo == null)
        {
            return false;
        }

        ItemTypeEnum itemType = GetItemType();

        // 检查生物是否可以装备该类型的道具
        List<ItemTypeEnum> creatureEquipTypes = creatureInfo.GetEquipItemsType();
        if (!creatureEquipTypes.Contains(itemType))
        {
            return false;
        }

        // 检查种族模组是否匹配（creature_model_id 为 0 表示通用装备，任何种族可装；否则须与生物 model_id 一致）
        if (creature_model_id != 0 && creature_model_id != creatureInfo.model_id)
        {
            return false;
        }

        // 如果是武器类型，需要检查武器类型是否匹配
        if (itemType == ItemTypeEnum.Weapon)
        {
            ItemTypeWeaponEnum weaponType = GetWeaponType();
            // equip_items_weapon_type 为 0 表示可以装备所有武器类型
            if (creatureInfo.equip_items_weapon_type != 0 &&
                creatureInfo.equip_items_weapon_type != (int)weaponType)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断生物是否可以装备该道具（通过 creatureId）
    /// </summary>
    /// <param name="creatureId">生物 ID</param>
    /// <returns>是否可以装备</returns>
    public bool CanEquipForCreature(long creatureId)
    {
        CreatureInfoBean creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        return CanEquipForCreature(creatureInfo);
    }

    /// <summary>
    /// 处理攻击模块数据
    /// </summary>
    public void HandleItemsInfoAttackModeData(BaseAttackMode attackMode)
    {
        if (dicAttackModeData == null)
        {
            dicAttackModeData = attack_mode_data.SplitForDictionary<ItemInfoAttackModeDataEnum>();
        }
        //先还原一下预制
        if (attackMode.spriteRenderer != null)
        {
            attackMode.spriteRenderer.transform.localScale = Vector3.one;
            attackMode.spriteRenderer.material.SetFloat("_StartAngle", 0);
            attackMode.spriteRenderer.material.SetFloat("_VertexRotateSpeed", 0);
            attackMode.spriteRenderer.material.SetVector("_VertexRotateAxis", Vector3.one);
        }
        bool isShowSprite = false;
        foreach (var item in dicAttackModeData)
        {
            switch (item.Key)
            {
                case ItemInfoAttackModeDataEnum.VertexRotateAxis:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemVertexRotateAxis = item.Value.SplitForVector3(',');
                        attackMode.spriteRenderer.material.SetVector("_VertexRotateAxis", itemVertexRotateAxis);
                    }
                    break;
                case ItemInfoAttackModeDataEnum.VertexRotateSpeed:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemVertexRotateSpeed = float.Parse(item.Value);
                        attackMode.spriteRenderer.material.SetFloat("_VertexRotateSpeed", itemVertexRotateSpeed);
                    }
                    break;
                case ItemInfoAttackModeDataEnum.ShowSprite:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemShowSprite = item.Value;
                        IconHandler.Instance.SetItemIconForAttackMode(itemShowSprite, attackMode.spriteRenderer);
                        isShowSprite = true;
                    }
                    break;
                case ItemInfoAttackModeDataEnum.StartPosition:
                    if (attackMode.gameObject != null)
                    {
                        var itemStartPosition = item.Value.SplitForVector3(',');
                        attackMode.gameObject.transform.position += itemStartPosition;
                    }
                    break;
                case ItemInfoAttackModeDataEnum.StartSize:
                    if (attackMode.gameObject != null)
                    {
                        var itemStartSize = float.Parse(item.Value);
                        attackMode.spriteRenderer.transform.localScale = Vector3.one * itemStartSize;
                    }
                    break;
                case ItemInfoAttackModeDataEnum.StartRotate:
                    if (attackMode.spriteRenderer != null)
                    {
                        var itemStartAngle = float.Parse(item.Value);
                        attackMode.spriteRenderer.material.SetFloat("_StartAngle", itemStartAngle);
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
    public static Dictionary<long, List<ItemsInfoBean>> dicDataForCreatureModel;

    /// <summary>
    /// 是否包含生物模型
    /// </summary>
    public static bool ContainsKeyForCreatureModelId(long creatureModelId)
    {
        InitDataForCreatureModel();
        if (dicDataForCreatureModel.ContainsKey(creatureModelId))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 根据生物模型获取数据
    /// </summary>
    public static List<ItemsInfoBean> GetDataByCreatureModelId(long creatureModelId)
    {
        InitDataForCreatureModel();
        if (dicDataForCreatureModel.TryGetValue(creatureModelId, out List<ItemsInfoBean> valueData))
        {
            return valueData;
        }
        return null;
    }

    /// <summary>
    /// 初始化生物模型数据
    /// </summary>
    public static void InitDataForCreatureModel()
    {
        if (dicDataForCreatureModel == null)
        {
            dicDataForCreatureModel = new Dictionary<long, List<ItemsInfoBean>>();
            var allData = GetAllArrayData();
            for (int i = 0; i < allData.Length; i++)
            {
                var itemData = allData[i];
                if (itemData.creature_model_info_id == 0)
                {
                    continue;
                }
                var creatureModelInfo = CreatureModelInfoCfg.GetItemData(itemData.creature_model_info_id);
                if (creatureModelInfo == null)
                {
                    LogUtil.LogError($"初始化生物模型数据失败 没有 ItemInfo.creature_model_info_id:{itemData.creature_model_info_id}");
                    continue;
                }
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
    }
}
