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
    FingerRing = 6,
    Weapon = 10,
    Portrait = 101,
}

public enum ItemTypeWeaponEnum
{
    Staff = 1,       // 法杖
    OneHanded = 2,  // 单手剑
    TwoHanded = 3,  // 双手剑
    SwordAndShield = 4,  // 刀盾
    GreatSword = 5,      // 大剑
    GreatShield = 6,     // 大盾
    Bow = 7,             // 弓
    Thrown = 8,          // 投掷物
}


/// <summary>
/// 道具使用者类型枚举
/// </summary>
public enum ItemUserTypeEnum
{
    Default = 0,//默认 所有生物可用
    DemonLord = 1,//魔王专属
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

