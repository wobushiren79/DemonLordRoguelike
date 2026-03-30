using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeRegainHP : AttackModeRegain
{
    public override void HandleRegain(FightCreatureEntity attacker, FightCreatureEntity attacked)
    {
        base.HandleRegain(attacker, attacked);
        attacked.RegainHP(this);
    }
}
