using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreatureBean
{
    public long id;//ID;

    public int headId;//ͷ��ID
    public int hatId;//ñ��ID
    public int armorShoulderId;//�粿����ID
    public int armorPantsId;//���ӻ���ID
    public CreatureBean(long id)
    {
        this.id = id;
    }
}
