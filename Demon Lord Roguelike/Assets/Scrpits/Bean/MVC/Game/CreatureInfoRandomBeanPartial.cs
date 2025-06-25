using System.Collections.Generic;
public partial class CreatureInfoRandomBean
{
    protected Dictionary<CreatureSkinTypeEnum, List<long>> dicRandomData;

    public Dictionary<CreatureSkinTypeEnum, List<long>> GetAllRandomData()
    {
        if (dicRandomData == null)
        {
            dicRandomData = new Dictionary<CreatureSkinTypeEnum, List<long>>();
        }
        List<long> listRandomData = random_data.SplitForListLong(',', '-');
        for (int i = 0; i < listRandomData.Count; i++)
        {
            var itemId = listRandomData[i];
            var itemInfo = CreatureModelInfoCfg.GetItemData(itemId);
            if (dicRandomData.TryGetValue(itemInfo.GetPartType(), out var itemList))
            {
                itemList.Add(itemId);
            }
            else
            {
                dicRandomData.Add(itemInfo.GetPartType(), new List<long>() { itemId });
            }
        }
        return dicRandomData;
    }

    public List<long> GetRandomData()
    {
        List<long> listSkinRandom = new List<long>();
        var allRandomData = GetAllRandomData();
        foreach (var item in allRandomData)
        {
            List<long> listSkin = item.Value;
            int targetSkinRandomIndex = UnityEngine.Random.Range(0, listSkin.Count);
            long targetSkinRandom = listSkin[targetSkinRandomIndex];
            listSkinRandom.Add(targetSkinRandom);
        }
        return listSkinRandom;
    }
}

public partial class CreatureInfoRandomCfg
{

}
