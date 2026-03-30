using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeRegainDR : AttackModeRegain
{
    public override void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {
        base.HandleRegain(attacker, attacked);
        attacked.RegainDR(this);
    }
}
