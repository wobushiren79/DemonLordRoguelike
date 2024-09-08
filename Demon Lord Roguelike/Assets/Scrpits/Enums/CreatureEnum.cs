
using DG.Tweening;

public enum CreatureTypeEnum
{
    FightDef = 1,//战斗防守方
    FightAtt = 2,//战斗进攻方
    FightDefCore = 99,//防守方核心
}

public enum CreatureStateEnum
{
    None = 0,
    Live = 1,//存活
    Dead = 2,//死亡
}

public enum CreatureSkinTypeEnum
{
    Base = 0,
    Head = 1,//头部
    Hat = 2,//帽子
    Hair = 3,//发型
    Body = 4,//身体
    Eye = 5,//眼睛
    Mouth = 6,//嘴巴
    Clothes = 10,//衣服
    Pants = 12,//裤子

    Armor_Arm_Up_L = 20,//左肩上
    Armor_Arm_Down_L = 21,//左肩上
    Armor_Palm_Up_L = 22,//手套左
    Armor_Arm_Up_R = 25,//左肩上
    Armor_Arm_Down_R = 26,//左肩上
    Armor_Palm_Up_R = 27,//手套右

    Armor_Thigh_L = 30,//大腿
    Armor_Calf_L = 31,//小腿
    Shoe_L = 32,//鞋子
    Armor_Thigh_R = 35,
    Armor_Calf_R = 36,
    Shoe_R = 37,

    Weapon_L = 90,//武器左
    Weapon_R = 91//武器右
}

public class CreatureEnum
{
    public static string GetCreatureSkinTypeEnumName(CreatureSkinTypeEnum creatureSkinType)
    {
        switch (creatureSkinType)
        {
            case CreatureSkinTypeEnum.Base:
                return "";
            case CreatureSkinTypeEnum.Head:
                return TextHandler.Instance.GetTextById(1001);
            case CreatureSkinTypeEnum.Hat:
                return TextHandler.Instance.GetTextById(1002);
            case CreatureSkinTypeEnum.Hair:
                return TextHandler.Instance.GetTextById(1003);
            case CreatureSkinTypeEnum.Body:
                return TextHandler.Instance.GetTextById(1004);
            case CreatureSkinTypeEnum.Eye:
                return TextHandler.Instance.GetTextById(1005);
            case CreatureSkinTypeEnum.Mouth:
                return TextHandler.Instance.GetTextById(1006);
        }
        return "???";
    }

}