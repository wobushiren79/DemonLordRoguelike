using System;
using UnityEngine;

[Serializable]
public class CreatureCardItemBean
{
    public CreatureBean creatureData;//卡片数据
    public CardUseStateEnum cardUseState;//卡片用途
    public CardStateEnum cardState;//卡片状态(用于UI展示)

    public Vector2 originalCardPos;//卡片的起始位置
    public int originalSibling;//卡片的原始层级

    public int indexList = 0;//序号 用于多卡片
}