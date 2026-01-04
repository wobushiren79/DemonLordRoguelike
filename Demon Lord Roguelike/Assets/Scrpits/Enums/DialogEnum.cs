using UnityEngine;
using UnityEditor;

public enum DialogEnum
{
    Normal = 0,
    Select,//选项
    SelectItem,//道具选择
    SelectColor,//选择颜色
    SelectCreature,//选择生物
    PortalDetails,//传送门详情确认
    Rename,//重命名
}