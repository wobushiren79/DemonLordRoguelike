using UnityEditor;
using UnityEngine;

public class GashaponItemBean
{
    public CreatureBean creatureData;

    public GashaponItemBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        creatureData.AddAllSkin();
    }
}