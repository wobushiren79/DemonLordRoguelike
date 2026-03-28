using System;

public static class CreatureUtil
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
