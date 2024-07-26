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
    Attack2,
    Dead,
}


//��Ϸս��Ԥ��״̬
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//ʰȡ����У�
    Droping = 2,//ʰȡ��
}

public enum TestSceneTypeEnum
{
    None = 0,
    FightSceneTest = 1,//ս����������
    CardTest = 2,//��ƬЧ������
}