using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeRegainHP : AttackModeRegain
{
    public override void HandleRegain(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked)
    {
        base.HandleRegain(attacker, attacked);
        attacked.RegainHP(this);
    }
}
