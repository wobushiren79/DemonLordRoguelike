
using DG.Tweening;

public enum CreatureTypeEnum
{
    FightDef = 1,//ս�����ط�
    FightAtt = 2,//ս��������
    FightDefCore = 99,//���ط�����
}

public enum CreatureStateEnum
{
    None = 0,
    Live = 1,//���
    Dead = 2,//����
}

public enum CreatureSkinTypeEnum
{
    Base = 0,
    Head = 1,//ͷ��
    Hat = 2,//ñ��
    Hair = 3,//����
    Body = 4,//����
    Eye = 5,//�۾�
    Mouth = 6,//���
    Clothes = 10,//�·�
    Pants = 12,//����

    Armor_Arm_Up_L = 20,//�����
    Armor_Arm_Down_L = 21,//�����
    Armor_Palm_Up_L = 22,//������
    Armor_Arm_Up_R = 25,//�����
    Armor_Arm_Down_R = 26,//�����
    Armor_Palm_Up_R = 27,//������

    Armor_Thigh_L = 30,//����
    Armor_Calf_L = 31,//С��
    Shoe_L = 32,//Ь��
    Armor_Thigh_R = 35,
    Armor_Calf_R = 36,
    Shoe_R = 37,

    Weapon_L = 90,//������
    Weapon_R = 91//������
}

public class CreatureEnum
{
    public static string GetCreatureSkinTypeEnumName(CreatureSkinTypeEnum creatureSkinType)
    {
        switch (creatureSkinType)
        {
            case CreatureSkinTypeEnum.Base:
                return "";
            case CreatureSkinTypeEnum.Head:
                return TextHandler.Instance.GetTextById(1001);
            case CreatureSkinTypeEnum.Hat:
                return TextHandler.Instance.GetTextById(1002);
            case CreatureSkinTypeEnum.Hair:
                return TextHandler.Instance.GetTextById(1003);
            case CreatureSkinTypeEnum.Body:
                return TextHandler.Instance.GetTextById(1004);
            case CreatureSkinTypeEnum.Eye:
                return TextHandler.Instance.GetTextById(1005);
            case CreatureSkinTypeEnum.Mouth:
                return TextHandler.Instance.GetTextById(1006);
        }
        return "???";
    }

}