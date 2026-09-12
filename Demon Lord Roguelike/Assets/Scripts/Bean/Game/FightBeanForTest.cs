using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForTest : FightBean
{
    //进攻数据
    public FightAttackBean fightAttackDataRemark;

    //测试深渊馈赠目标行ID列表(已按"族根+等级"解析好的具体馈赠行id；由 GameFightLogicTest 在防守核心创建后统一添加)
    public List<long> testAbyssalBlessingIds = new List<long>();

    //测试魔王蓝量(战斗开始时魔王当前魔力值,应用时同时把魔力上限提升到不低于该值;由 GameFightLogicTest 在防守核心创建后统一应用)
    public float testDemonLordMP = 9999;

    //测试防守方固定属性(属性类型→固定值; 由 GameTestEditor「防守方固定属性」配置, 对防守方全部生物=卡片魔物+魔王核心生效:
    //作为基础值替换——加点/装备/BUFF/深渊馈赠等修正仍在固定值上叠加; MP 被固定时 testDemonLordMP 蓝量设置不生效)
    public Dictionary<CreatureAttributeTypeEnum, float> dicTestDefenseFixedAttribute = new Dictionary<CreatureAttributeTypeEnum, float>();


    public FightBeanForTest() : base()
    {

    }
}