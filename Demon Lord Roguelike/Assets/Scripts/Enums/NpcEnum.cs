public enum NpcTypeEnum
{
    None = 0,
    Councilor = 2,//议会固定NPC(使用固定的装备和样貌, 拥有独立持久化的好感系统)
    CouncilorRandom = 3,//议会随机NPC(使用随机的装备和样貌, 每场议会临时随机生成)
}

public enum NpcRelationshipEnum
{
    Hatred = 1,//仇恨
    Neutral = 2,//冷淡
    Acquaintance = 3,//中立
    FriendShip = 4,//友好
    Infatuation = 5,//迷恋
}

public enum NpcVoteTypeEnum
{
    None = 0,
    Aye = 1,//赞成
    Nay = 2,//反对
    Sleep = 3,//睡觉
}