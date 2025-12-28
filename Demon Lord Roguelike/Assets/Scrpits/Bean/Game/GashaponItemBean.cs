using UnityEditor;
using UnityEngine;

public class GashaponItemBean
{
    //生物数据
    public CreatureBean creatureData;
    //是否打开
    public bool isBreak;

    public GashaponItemBean(long creatureId, GashaponMachineCreatureStruct gashaponMachineCreature)
    {
        isBreak = false;
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