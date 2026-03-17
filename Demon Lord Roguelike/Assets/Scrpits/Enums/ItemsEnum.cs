/// <summary>
/// 道具id枚举
/// </summary>
public enum ItemIdEnum
{
    Crystal = 1//魔晶
}


/// <summary>
/// 道具类型枚举
/// </summary>
public enum ItemTypeEnum
{
    Hat = 1,
    Clothes = 2,
    Pants = 3,
    Shoe = 4,
    NoseRing = 5,
    Weapon = 10,
    Crystal = 1000//魔晶
}


public enum ItemInfoAttackModeDataEnum
{
    ShowSprite,//展示的精灵图片
    VertexRotateAxis,//模型旋转角度0,0,-1
    VertexRotateSpeed,//模型旋转速度 10
    UVRotateSpeed,//UV旋转速度
    StartPosition,//开始位置
    StartSize,//开始大小
}