using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreatureSkinBean
{
    public long skinId;

    public CreatureSkinBean(long skinId)
    {
        this.skinId = skinId;
    }
}
