using System;
using System.Collections.Generic;
using UnityEngine;

public class AttackModeFalluponChain : BaseAttackMode
{
    //连锁次数
    public int chainNum = 3;
    public float timeForChainChange = 0.1f;

    //已攻击过的生物ID
    private HashSet<string> listAttackedCreatureId = new HashSet<string>();
    //当前连锁次数
    private int currentChainCount = 0;
    //原始伤害
    private int originalDamage = 0;
    //攻击结束回调
    private Action<BaseAttackMode> actionForAttackEnd;
    //当前被攻击者（用于确定下一次检测的中心位置）
    private FightCreatureEntity currentAttacked;
    //攻击者
    private FightCreatureEntity attackerEntity;

    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    public override async void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        this.actionForAttackEnd = actionForAttackEnd;
        this.attackerEntity = attacker;
        this.currentAttacked = attacked;

        if (attacker == null || attacked == null || attacked.IsDead())
        {
            EndAttack();
            return;
        }

        //记录原始伤害
        originalDamage = attackModeData.attackerDamage;

        //执行初始攻击
        ExecuteAttack(attacked, originalDamage, true);

        //如果已达到连锁上限，结束
        if (currentChainCount >= chainNum)
        {
            EndAttack();
            return;
        }

        //连锁攻击
        for (int i = 0; i < chainNum; i++)
        {
            await new WaitForSeconds(timeForChainChange);

            //检测游戏状态
            if (!CheckGameState())
            {
                EndAttack();
                return;
            }

            //检测当前目标是否有效
            if (currentAttacked == null || currentAttacked.creatureObj == null || currentAttacked.IsDead())
            {
                EndAttack();
                return;
            }

            //执行连锁攻击
            bool hasNextTarget = HandleChainAttack();
            if (!hasNextTarget)
            {
                EndAttack();
                return;
            }
        }

        EndAttack();
    }

    /// <summary>
    /// 检测游戏状态
    /// </summary>
    private bool CheckGameState()
    {
        var gameLogic = GameHandler.Instance.manager.gameLogic;
        if (gameLogic == null)
        {
            return false;
        }
        return gameLogic.gameState == GameStateEnum.Gaming;
    }

    /// <summary>
    /// 处理连锁攻击
    /// </summary>
    private bool HandleChainAttack()
    {
        Vector3 checkPosition = currentAttacked.creatureObj.transform.position;
        List<FightCreatureEntity> listCandidate = new List<FightCreatureEntity>();
        Collider[] targetColliders = GetHitTargetAreaCollider(checkPosition);
        if (targetColliders != null)
        {
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            for (int i = 0; i < targetColliders.Length; i++)
            {
                string creatureId = targetColliders[i].gameObject.name;
                if (listAttackedCreatureId.Contains(creatureId))
                {
                    continue;
                }
                var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureFightTypeEnum.None);
                if (targetCreature != null && !targetCreature.IsDead())
                {
                    listCandidate.Add(targetCreature);
                }
            }
        }

        if (listCandidate.Count == 0)
        {
            return false;
        }

        //随机选择一个目标
        int randomIndex = UnityEngine.Random.Range(0, listCandidate.Count);
        FightCreatureEntity nextTarget = listCandidate[randomIndex];

        //连锁次数+1，伤害减半
        currentChainCount++;
        int chainDamage = originalDamage;
        for (int i = 0; i < currentChainCount; i++)
        {
            chainDamage /= 2;
        }
        if (chainDamage < 1)
        {
            chainDamage = 1;
        }

        //更新攻击方向与目标位置
        if (attackerEntity != null && nextTarget.creatureObj != null)
        {
            attackModeData.attackDirection = Vector3.Normalize(nextTarget.creatureObj.transform.position - attackerEntity.creatureObj.transform.position);
        }
        attackModeData.targetPos = nextTarget.creatureObj.transform.position;

        //执行攻击
        ExecuteAttack(nextTarget, chainDamage, false);
        currentAttacked = nextTarget;

        return true;
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    private void ExecuteAttack(FightCreatureEntity target, int damage, bool isFirst)
    {
        attackModeData.attackerDamage = damage;
        target.UnderAttack(this);
        PlayEffectForHit(target.creatureObj.transform.position);
        listAttackedCreatureId.Add(target.fightCreatureData.creatureData.creatureUUId);

        //是否是第一次攻击
        if (isFirst)
        {
            gameObject?.transform.Find("Start")?.GetComponent<ParticleSystem>()?.Play(); //播放粒子特效>
        }
        else
        {
            gameObject?.transform.Find("Chain")?.GetComponent<ParticleSystem>()?.Play(); //播放粒子特效>
        }
    }

    /// <summary>
    /// 结束攻击
    /// </summary>
    private void EndAttack()
    {
        Destroy();
        actionForAttackEnd?.Invoke(this);
    }
}
