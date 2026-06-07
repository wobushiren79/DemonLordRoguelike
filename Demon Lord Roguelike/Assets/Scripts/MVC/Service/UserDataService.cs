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
    /// 保存用户数据（主存档带自动备份最多3份；解锁/成就拆分为同槽目录下的独立文件一并保存）
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

        // 备份主存档，最多保留3份，循环覆盖
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

        // 写入主存档
        base.Save(data);

        // 拆分存档：解锁/成就独立文件（同槽目录，复用 BaseDataService 泛型读写，不做备份）
        GetSplitService<UserUnlockBean>($"UserUnlock_{slotIndex}").Save(data.GetUserUnlockData());
        GetSplitService<UserAchievementBean>($"UserAchievement_{slotIndex}").Save(data.GetUserAchievementData());
    }

    /// <summary>
    /// 读取用户数据（主存档 + 注入拆分的解锁/成就数据）
    /// 拆分文件不存在时（全新槽位或旧存档）注入空数据，不读取旧版内嵌字段
    /// </summary>
    public override UserDataBean Load(bool isShowLog = true)
    {
        UserDataBean data = base.Load(isShowLog);
        if (data == null)
            return null;
        data.userUnlockData = GetSplitService<UserUnlockBean>($"UserUnlock_{slotIndex}").Load(false) ?? new UserUnlockBean();
        data.userAchievementData = GetSplitService<UserAchievementBean>($"UserAchievement_{slotIndex}").Load(false) ?? new UserAchievementBean();
        return data;
    }

    /// <summary>
    /// 删除用户数据（主存档 + 拆分的解锁/成就文件）
    /// </summary>
    public override void Delete()
    {
        base.Delete();
        FileUtil.DeleteFile($"{StoragePath}/UserUnlock_{slotIndex}");
        FileUtil.DeleteFile($"{StoragePath}/UserAchievement_{slotIndex}");
    }

    /// <summary>
    /// 构造一个指向当前槽目录的拆分存档服务（按类型与文件名即用即建，复用泛型 Load/Save）
    /// </summary>
    /// <typeparam name="T">拆分数据类型</typeparam>
    /// <param name="fileName">拆分文件名</param>
    private BaseDataService<T> GetSplitService<T>(string fileName) where T : class, new()
    {
        return new BaseDataService<T>(fileName) { StoragePath = this.StoragePath };
    }
}
