using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FightRecordsBean
{
    public Dictionary<string, FightRecordsCreatureBean> dicRecordsCreatureData = new Dictionary<string, FightRecordsCreatureBean>();

    //总共添加的经验
    public long totalAddExp = 0;

    //防守方造成的总伤害
    public long totalDamageForDef = 0;
    //进攻方造成的总伤害
    public long totalDamageForAtk = 0;

    //防守方受到的伤害
    public long totalDamageReceivedForDef = 0;
    //进攻方受到的伤害
    public long totalDamageReceivedForAtk = 0;

    //防守方杀敌总数
    public long totalKillNumForDef = 0;
    //进攻方杀敌总数
    public long totalKillNumForAtk = 0;

    //防守方总回复血量
    public long totalRegainHPForDef = 0;
    //进攻方总回复血量
    public long totalRegainHPForAtk = 0;

    //防守方总回复护甲
    public long totalRegainDRForDef = 0;
    //进攻方总回复护甲
    public long totalRegainDRForAtk = 0;

    /// <summary>
    /// 获取生物记录
    /// </summary>
    public List<FightRecordsCreatureBean> GetRecordsForCreatureData()
    {
        List<FightRecordsCreatureBean> listData = new List<FightRecordsCreatureBean>();
        foreach (var item in dicRecordsCreatureData)
        {
            listData.Add(item.Value);
        }
        return listData;
    }

    /// <summary>
    /// 获取生物记录
    /// </summary>
    public FightRecordsCreatureBean GetRecordsForCreatureData(string creatureId, bool isAdd)
    {
        if (dicRecordsCreatureData.TryGetValue(creatureId, out var targetData))
        {
            return targetData;
        }
        else
        {
            if (isAdd)
            {
                var addData = new FightRecordsCreatureBean(creatureId);
                dicRecordsCreatureData.Add(creatureId, addData);
                return addData;
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 添加经验
    /// </summary>
    public void AddCreatureExp(string creatureId, int addValue)
    {
        var recordsData = GetRecordsForCreatureData(creatureId, true);
        recordsData.AddExp(addValue);
        totalAddExp += addValue;
    }

    /// <summary>
    /// 添加恢复HP的量
    /// </summary>
    public void AddCreatureRegainHP(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddRegainHP(addValue);
            totalRegainHPForDef += addValue;
        }
        else
        {
            totalRegainHPForAtk += addValue;
        }
    }

    /// <summary>
    /// 接受恢复HP的量
    /// </summary>
    public void AddCreatureRegainHPReceived(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddRegainHPReceived(addValue);
        }
    }

    /// <summary>
    /// 添加恢复DR的量
    /// </summary>
    public void AddCreatureRegainDR(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddRegainDR(addValue);
            totalRegainDRForDef += addValue;
        }
        else
        {
            totalRegainDRForAtk += addValue;
        }
    }

    /// <summary>
    /// 接受恢复DR的量
    /// </summary>
    public void AddCreatureRegainDRReceived(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddRegainDRReceived(addValue);
        }
    }

    /// <summary>
    /// 添加伤害
    /// </summary>
    public void AddCreatureDamage(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddDamage(addValue);
            totalDamageForDef += addValue;
        }
        else
        {
            totalDamageForAtk += addValue;
        }
    }

    /// <summary>
    /// 添加受到的伤害
    /// </summary>
    public void AddCreatureDamageReceived(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddDamageReceived(addValue);
            totalDamageReceivedForDef += addValue;
        }
        else
        {
            totalDamageReceivedForAtk += addValue;
        }
    }

    /// <summary>
    /// 添加杀敌数
    /// </summary>
    public void AddCreatureKillNum(string creatureId, int addValue)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //是否是防守生物
        if (gameLogic.fightData.dlDefenseCreatureData.ContainsKey(creatureId))
        {
            var recordsData = GetRecordsForCreatureData(creatureId, true);
            recordsData.AddKillNum(addValue);
            totalKillNumForDef += addValue;
        }
        else
        {
            totalKillNumForAtk += addValue;
        }
    }
}

public class FightRecordsCreatureBean
{
    public FightRecordsCreatureBean(string creatureId)
    {
        this.creatureId = creatureId;
    }

    public string creatureId;//生物ID；
    public int damage;//造成的伤害
    public int killNum;//杀敌数
    public int damageReceived;//受到的伤害
    public int exp;//添加的经验
    public int regainHP;//给别人加血
    public int regainHPReceived;//接受别人加血

    public int regainDR;//给别人加护甲
    public int regainDRReceived;//接受别人护甲

    public void AddRegainHP(int addValue)
    {
        regainHP += addValue;
    }
    public void AddRegainDR(int addValue)
    {
        regainHP += addValue;
    }
    public void AddRegainHPReceived(int addValue)
    {
        regainHPReceived += addValue;
    }
    public void AddRegainDRReceived(int addValue)
    {
        regainDRReceived += addValue;
    }

    /// <summary>
    /// 添加经验
    /// </summary>
    public void AddExp(int addValue)
    {
        exp += addValue;
    }

    /// <summary>
    /// 添加伤害
    /// </summary>
    public void AddDamage(int addValue)
    {
        damage += addValue;
    }

    /// <summary>
    /// 添加受到的伤害
    /// </summary>
    public void AddDamageReceived(int addValue)
    {
        damageReceived += addValue;
    }

    /// <summary>
    /// 添加杀敌数
    /// </summary>
    public void AddKillNum(int addValue)
    {
        killNum += addValue;
    }
}