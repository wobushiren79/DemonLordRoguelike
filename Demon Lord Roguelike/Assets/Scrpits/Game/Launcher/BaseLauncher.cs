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
    /// ����
    /// </summary>
    public virtual void Launch()
    {
        //��ʼ��ͼ��
        IconHandler.Instance.InitData();
        //������һ���ڴ�
        SystemUtil.GCCollect();
    }
}
