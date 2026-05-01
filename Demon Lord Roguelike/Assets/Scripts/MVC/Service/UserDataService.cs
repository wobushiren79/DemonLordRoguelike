/*
* FileName: UserDataService
* Author: AppleCoffee
* CreateTime: 2024-07-16-17:44:25
*/

using UnityEngine;
using System;

public class UserDataService : BaseDataService<UserDataBean>
{
    private int slotIndex;

    public UserDataService(int slotIndex = 0) : base($"UserData_{slotIndex}")
    {
        this.slotIndex = slotIndex;
        StoragePath = $"{Application.persistentDataPath}/UserData_{slotIndex}";
    }

    /// <summary>
    /// 切换存档槽位
    /// </summary>
    public void ChangeSlot(int slotIndex)
    {
        this.slotIndex = slotIndex;
        FileName = $"UserData_{slotIndex}";
        StoragePath = $"{Application.persistentDataPath}/UserData_{slotIndex}";
    }

    /// <summary>
    /// 保存用户数据（带自动备份，最多保留3份）
    /// </summary>
    public override void Save(UserDataBean data)
    {
        if (data == null)
        {
            LogUtil.Log("保存文件失败-没有数据");
            return;
        }

        // 创建目录
        FileUtil.CreateDirectory(StoragePath);

        // 备份数据，最多保留3份，循环覆盖
        if (data.saveRemarkIndex >= 3)
        {
            data.saveRemarkIndex = 0;
        }

        string sourcePath = $"{StoragePath}/{FileName}";
        string backupPath = $"{StoragePath}/{FileName}_Backups_{data.saveRemarkIndex}";
        bool isRemarkSuccess = FileUtil.CopyFile(sourcePath, backupPath, true);
        if (isRemarkSuccess)
        {
            data.saveRemarkIndex++;
        }

        // 写入新数据
        base.Save(data);
    }
}
