using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherGame : BaseLauncher
{
    public override void Launch()
    {
        base.Launch();
        WorldHandler.Instance.EnterMainForBaseScene();
    }
}
