using UnityEditor;
using UnityEngine;

public class GashaponItemBean
{
    public CreatureBean creatureData;

    public GashaponItemBean(long creatureId, GashaponMachineCreatureStruct gashaponMachineCreature)
    {
        creatureData = new CreatureBean(creatureId);
        //随机皮肤
        foreach (var item in gashaponMachineCreature.randomCreatureMode)
        {
            var listSkin = item.Value;
            int randomIndex = Random.Range(0, listSkin.Count);
            var randomSkin = listSkin[randomIndex];
            creatureData.AddSkin(randomSkin);
        }
        //添加基础皮肤
        creatureData.AddSkinForBase();
        //creatureData.AddAllSkin();
    }
}