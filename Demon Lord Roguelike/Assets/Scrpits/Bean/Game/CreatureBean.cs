using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreatureBean
{
    public long id;//ID;

    public int headId;//头部ID
    public int hatId;//帽子ID
    public int armorShoulderId;//肩部护甲ID
    public int armorPantsId;//裤子护甲ID
    public CreatureBean(long id)
    {
        this.id = id;
    }
}
