using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightAttCreateBean
{
    public Dictionary<int, FightAttCreateDetailsBean> dicDetailsData;
}

/// <summary>
/// ��ͨ��������
/// </summary>
public class FightAttCreateDetailsBean
{
    public int stage;//�׶�
    public float timeDuration;//����ʱ��

    public float createNum;//һ�����ɵ�����
    public float createNumLerpData;//ÿ���������������Ա仯

    public float createDelay;//һ�����ɼ��
    public float createDelayLerpData;//һ�����ɼ��

    public Dictionary<float, List<int>> creatureIds;//��ͬʱ��׶� ��Ҫ���ɵ����keyΪ0-1�ٷֱȣ�
    public Dictionary<int, int> creatureEndIds;//���һ����Ҫ���ɵ�����
}

