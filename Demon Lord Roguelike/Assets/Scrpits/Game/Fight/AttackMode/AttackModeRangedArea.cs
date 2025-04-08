using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class AttackModeRangedArea : AttackModeRanged
{
    public override void HandleForHitTarget(GameFightCreatureEntity gameFightCreatureEntity)
    {
        //播放一个范围攻击特效
        if(!attackModeInfo.effect_hit.IsNull())
        {
            EffectHandler.Instance.ShowBoomEffect(attackModeInfo.effect_hit, gameObject.transform.position, attackModeInfo.collider_area_size);
        }
        //检测范围内的敌人
        CheckHitTargetArea((targetCreature) =>
        {
            targetCreature.UnderAttack(this);
        });
        Destory();
    }

    /// <summary>
    /// 检测范围内敌人
    /// </summary>
    public bool CheckHitTargetArea(Action<GameFightCreatureEntity> actionForHitItem)
    {
        bool hasHitter = false;
        Collider[] targetColliders = RayUtil.OverlapToSphere(gameObject.transform.position, attackModeInfo.collider_area_size, 1 << attackedLayer);
        //绘制测试范围
        DrawTestArea(gameObject.transform.position, attackModeInfo.collider_area_size, 1);

        if (targetColliders != null)
        {
            for (int i = 0; i < targetColliders.Length; i++)
            {
                var itemHitterCollder = targetColliders[i];
                string creatureId = itemHitterCollder.gameObject.name;
                GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureTypeEnum.None);
                if (targetCreature != null && !targetCreature.IsDead())
                {
                    hasHitter = true;
                    actionForHitItem?.Invoke(targetCreature);
                }
            }
        }
        return hasHitter;
    }

    public void DrawTestArea(Vector3 startPostion, float areaSize, float duration)
    {
#if UNITY_EDITOR
        Debug.DrawRay(startPostion, new Vector3(areaSize, 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(-areaSize, 0, 0), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, areaSize), Color.red, duration);
        Debug.DrawRay(startPostion, new Vector3(0, 0, -areaSize), Color.red, duration);
#endif
    }
}
