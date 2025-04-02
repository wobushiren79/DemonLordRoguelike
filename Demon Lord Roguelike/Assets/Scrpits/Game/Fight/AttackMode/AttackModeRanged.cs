using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged : BaseAttackMode
{
    /// <summary>
    /// 初始化展示
    /// </summary>
    public override void InitAttackModeShow()
    {
        base.InitAttackModeShow();

        long targetWeaponId = 0;
        //首先检测是否有武器道具
        if (weaponItemId != 0)
        {
            targetWeaponId = weaponItemId;
        }
        //如果没有道具 则使用角色默认的武器
        else
        {
            if (creatureId != 0)
            {
                var creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
                if (creatureInfo != null)
                {
                    long baseWeaponId = creatureInfo.GetEquipBaseWeaponId();
                    if (baseWeaponId != 0)
                    {
                        targetWeaponId = baseWeaponId;
                    }
                }
            }
        }
        //如果没有找到对应武器 则使用?图标
        if (targetWeaponId == 0)
        {
            IconHandler.Instance.GetUnKnowSprite((targetSprite) =>
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = targetSprite;
                }
            });
        }
        else
        {
            IconHandler.Instance.SetItemIconForAttackMode(weaponItemId, spriteRenderer);
            var weaponItemInfo = ItemsInfoCfg.GetItemData(weaponItemId);
            if (weaponItemInfo != null && !weaponItemInfo.attack_mode_data.IsNull())
            {
                weaponItemInfo.HandleItemsInfoAttackModeData(spriteRenderer);
            }
        }  
    }

    /// <summary>
    /// 开始攻击
    /// </summary>
    public override void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        gameObject.transform.position = attacker.creatureObj.transform.position + new Vector3(0, 0.5f, 0);
        actionForAttackEnd?.Invoke();
    }

    public override void Update()
    {
        base.Update();
        RayUtil.RayToCast(gameObject.transform.position, attackDirection, attackModeInfo.collider_size, 1 << attackedLayer, out RaycastHit hit);
        if (hit.collider != null)
        {
            string creatureId = hit.collider.gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureTypeEnum.None);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                //扣血
                targetCreature.UnderAttack(this);
                //攻击完了就回收这个攻击
                Destory();
                return;
            }
        }
        gameObject.transform.Translate(attackDirection * Time.deltaTime * attackModeInfo.speed_move);
        //边界处理
        if (gameObject.transform.position.x > 15 || gameObject.transform.position.x < -5)
        {
            Destory();
        }
    }
}
