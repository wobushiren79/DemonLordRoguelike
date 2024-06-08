using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseLauncher : BaseMonoBehaviour
{
    private void Start()
    {
        Launch();
    }

    /// <summary>
    /// 启动
    /// </summary>
    public virtual void Launch()
    {
        //初始化图集
        IconHandler.Instance.InitData();
        //先清理一下内存
        SystemUtil.GCCollect();
    }
}
