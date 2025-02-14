using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventsInfo
{
    public static string Coin_Change = "Coin_Change";
    public static string Magic_Change = "Magic_Change";

    public static string UIViewCreatureCardItem_SelectKeep = "UIViewCreatureCardItem_SelectKeep";//��Ƭ����
    public static string UIViewCreatureCardItem_ShowDetails = "UIViewCreatureCardItem_ShowDetails";//չʾ��Ƭ����
    public static string UIViewCreatureCardItem_HideDetails = "UIViewCreatureCardItem_HideDetails";//���ؿ�Ƭ����

    public static string UIViewCreatureCardItem_OnPointerEnter = "UIViewCreatureCardItem_OnPointerEnter";//��Ƭ����
    public static string UIViewCreatureCardItem_OnPointerExit = "UIViewCreatureCardItem_OnPointerExit";//��Ƭ�뿪

    public static string UIViewCreatureCardItem_OnClickSelect = "UIViewCreatureCardItem_OnClickSelect";//���ѡ��

    public static string GameFightLogic_SelectCard = "GameFightLogic_SelectCard";//��Ƭѡ��
    public static string GameFightLogic_UnSelectCard = "GameFightLogic_UnSelectCard";//ȡ����Ƭѡ��
    public static string GameFightLogic_PutCard = "GameFightLogic_PutCard";//���ÿ�Ƭѡ��    
    public static string GameFightLogic_RefreshCard = "GameFightLogic_RefreshCard";//ˢ�¿�Ƭ  

    public static string GameFightLogic_CreatureDeadStart = "GameFightLogic_CreatureDeadStart";//���￪ʼ����  
    public static string GameFightLogic_CreatureDeadEnd = "GameFightLogic_CreatureDeadEnd";//�����������

    public static string Toast_NoEnoughCreateMagic = "Toast_NoEnoughCreateMagic";//û���㹻�Ĵ���ħ��

    #region Ť����
    public static string GashaponMachine_ClickBreak = "GashaponMachine_ClickBreak";//������� 
    public static string GashaponMachine_ClickNext = "GashaponMachine_ClickNext";//�����һ��
    public static string GashaponMachine_ClickReset = "GashaponMachine_ClickReset";//�������
    public static string GashaponMachine_ClickEnd = "GashaponMachine_ClickEnd";//�������
    #endregion
}
