using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameStateEnum
{
    None = 0,
    Pre,//׼����
    Gaming,//��Ϸ��
    End,//��Ϸ����
}

//��Ƭ״̬
public enum CardStateEnum
{
    None = 0,//��
    FightIdle = 101,//����
    FightSelect = 102,//ѡ��
    Fighting = 103,//�ϳ�ս��
    FightRest = 104,//��Ϣ
}

//����-����
public enum AnimationCreatureStateEnum
{
    Idle,
    Walk,
    Attack,
    Dead,
}