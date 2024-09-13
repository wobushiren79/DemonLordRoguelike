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

public enum Rarity
{
    N = 1,
    R = 2,
    SR = 3,
    SSR = 4,
    UR = 5,
    L = 6
}

//��Ƭ��;
public enum CardUseState
{
    Show,//չʾ
    Fight,//ս��
    Lineup,//����
    LineupBackpack,//���ݱ���
}

//��Ƭ״̬
public enum CardStateEnum
{
    None = 0,//��
    FightIdle = 101,//����
    FightSelect = 102,//ѡ��
    Fighting = 103,//�ϳ�ս��
    FightRest = 104,//��Ϣ

    LineupNoSelect = 201,//����δѡ��
    LineupSelect = 202,//����ѡ��
}

//��Ϸս��Ԥ��״̬
public enum GameFightPrefabStateEnum
{
    None = 0,
    DropCheck = 1,//ʰȡ����У�
    Droping = 2,//ʰȡ��
}

//��Ϸ����
public enum GameSceneTypeEnum
{
    None = 0,
    Base = 1,//����
    Fight = 2,//ս��
}

public enum TestSceneTypeEnum
{
    None = 0,
    FightSceneTest = 1,//ս����������
    CardTest = 2,//��ƬЧ������
    Base = 3,//���ز���
}

public enum CinemachineCameraEnum
{
    None,
    Base,
    Fight,
}