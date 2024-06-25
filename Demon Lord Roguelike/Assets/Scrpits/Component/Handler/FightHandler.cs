using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightHandler : BaseHandler<FightHandler, FightManager>
{
    public void Update()
    {
        UpdateHandleForAttackModePrefab();
    }

    /// <summary>
    /// 更新攻击模组
    /// </summary>
    public void UpdateHandleForAttackModePrefab()
    {
        if (manager.listAttackModePrefab.Count > 0)
        {
            for (int i = 0; i < manager.listAttackModePrefab.Count; i++)
            {
                var itemAttackMode = manager.listAttackModePrefab[i];
                itemAttackMode.Update();
            }
        }
    }

    /// <summary>
    /// 创建一个攻击预制
    /// </summary>
    public void CreateAttackModePrefab(int attackModeId, Action<BaseAttackMode> actionForComplete)
    {
        manager.GetAttackModePrefab(attackModeId, (targetPrefab) =>
        {
            targetPrefab.gameObject.SetActive(true);
            actionForComplete?.Invoke(targetPrefab);
        });
    }

    /// <summary>
    /// 移除一个攻击预制
    /// </summary>
    public void RemoveAttackModePrefab(BaseAttackMode targetMode)
    {
        targetMode.gameObject.SetActive(false);
        manager.RemoveAttackModePrefab(targetMode);
    }
}
