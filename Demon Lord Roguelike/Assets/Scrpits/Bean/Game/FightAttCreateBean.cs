using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightAttCreateBean
{
    public Dictionary<int, FightAttCreateDetailsBean> dicDetailsData;

    public FightAttCreateDetailsBean GetDetailData(int stage)
    {
        if (dicDetailsData.TryGetValue(stage, out FightAttCreateDetailsBean fightAttCreateDetails))
        {
            return fightAttCreateDetails;
        }
        return null;
    }
}

/// <summary>
/// ��ͨ��������
/// </summary>
public class FightAttCreateDetailsBean
{
    public int stage;//�׶�
    public float timeDuration;//����ʱ��

    public int createNum;//һ�����ɵ�����

    public float createDelay;//һ�����ɼ��
    public float createDelayLerpData;//һ�����ɼ��

    public List<FightAttCreateDetailsTimePointBean> timePointForCreatures;//��ͬʱ��׶� ��Ҫ���ɵ����keyΪ0-1�ٷֱȣ�
    public Dictionary<int, int> creatureEndIds;//���һ����Ҫ���ɵ�����
}

public class FightAttCreateDetailsTimePointBean
{
    public float startTimeProgress;
    public float endTimeProgress;
    public List<int> creatureIds;

    public FightAttCreateDetailsTimePointBean(float startTimeProgress,float endTimeProgress,List<int> creatureIds)
    {
        this.startTimeProgress = startTimeProgress;
        this.endTimeProgress = endTimeProgress;
        this.creatureIds = creatureIds;
    }
}

