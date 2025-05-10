
public enum CreatureSearchType{
    Ray = 0,//默认射线

    AreaSphere = 11,//范围Sphere
    AreaSphereHPNoMax = 12,//范围内hp不满
    AreaSphereDRNoMax = 13,//范围内dr不满
    AreaSphereFront = 14,//范围Sphere前方

    AreaBox = 21,//范围Box
    AreaBoxHPNoMax = 22,//范围内hp不满
    AreaBoxDRNoMax = 23,//范围内dr不满
    AreaBoxFront = 24,//范围Box前方

    DisMinByAll = 30,//遍历距离最近的生物
    DisMinByRoad = 31,//遍历同一路线所有生物
    DisMinByRoadAdjacentUpDown = 32,//遍历相邻2条路
}

public enum CreatureStateEnum
{
    Idle = 1,//闲置
    Fight = 2,//战斗中
    Rest = 3,//战斗休息
}

public enum CreatureTypeEnum
{
    None = 0,
    FightDefense = 1,//战斗防守方
    FightAttack = 2,//战斗进攻方
    FightDefenseCore = 99,//防守方核心
}

public enum CreatureFightStateEnum
{
    None = 0,
    Live = 1,//存活
    Dead = 2,//死亡
}

public enum CreatureSkinTypeEnum
{
    //---------------------身体
    Base = 0,//基础
    Head = 1,//头部
    Hair = 3,//发型
    Body = 4,//身体
    Eye = 5,//眼睛
    Mouth = 6,//嘴巴
    Horn = 7,//角
    Wing = 8,//翅膀
    NoseRing = 9,//鼻环
    //---------------------穿戴
    Hat = 50,//帽子
    Clothes = 51,//衣服
    Pants = 52,//裤子
    Shoe = 53,//鞋子
    Belt = 54,//腰带
    Gloves = 55,//手套
    //---------------------其他
    Weapon_Line = 80,//武器线
    //---------------------武器
    Weapon_L = 90,//武器左手
    Weapon_R = 91//武器右手
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