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
    /// 保存用户数据（主存档带自动备份最多3份；解锁/成就/背包道具/背包生物/好感/故事拆分为同槽目录下的独立文件一并保存）
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

        // 拆分存档：解锁/成就/背包道具/背包生物/好感/故事独立文件（同槽目录，复用 BaseDataService 泛型读写，不做备份）
        GetSplitService<UserUnlockBean>($"UserUnlock_{slotIndex}").Save(data.GetUserUnlockData());
        GetSplitService<UserAchievementBean>($"UserAchievement_{slotIndex}").Save(data.GetUserAchievementData());
        GetSplitService<UserBackpackItemsBean>($"UserBackpackItem_{slotIndex}").Save(data.GetUserBackpackItemsData());
        GetSplitService<UserBackpackCreatureBean>($"UserBackpackCreature_{slotIndex}").Save(data.GetUserBackpackCreatureData());
        GetSplitService<UserRelationshipBean>($"UserRelationship_{slotIndex}").Save(data.GetUserRelationshipData());
        GetSplitService<UserStoryBean>($"UserStory_{slotIndex}").Save(data.GetUserStoryData());
    }

    /// <summary>
    /// 读取用户数据（主存档 + 注入拆分的解锁/成就/背包道具/背包生物/好感/故事数据）
    /// 拆分文件不存在时（全新槽位或旧存档）注入空数据，不读取旧版内嵌字段
    /// </summary>
    public override UserDataBean Load(bool isShowLog = true)
    {
        UserDataBean data = base.Load(isShowLog);
        if (data == null)
            return null;
        data.userUnlockData = GetSplitService<UserUnlockBean>($"UserUnlock_{slotIndex}").Load(false) ?? new UserUnlockBean();
        data.userAchievementData = GetSplitService<UserAchievementBean>($"UserAchievement_{slotIndex}").Load(false) ?? new UserAchievementBean();
        data.userBackpackItemsData = GetSplitService<UserBackpackItemsBean>($"UserBackpackItem_{slotIndex}").Load(false) ?? new UserBackpackItemsBean();
        data.userBackpackCreatureData = GetSplitService<UserBackpackCreatureBean>($"UserBackpackCreature_{slotIndex}").Load(false) ?? new UserBackpackCreatureBean();
        data.userRelationshipData = GetSplitService<UserRelationshipBean>($"UserRelationship_{slotIndex}").Load(false) ?? new UserRelationshipBean();
        data.userStoryData = GetSplitService<UserStoryBean>($"UserStory_{slotIndex}").Load(false) ?? new UserStoryBean();
        return data;
    }

    /// <summary>
    /// 删除用户数据（主存档 + 拆分的解锁/成就/背包道具/背包生物/好感/故事文件）
    /// </summary>
    public override void Delete()
    {
        base.Delete();
        FileUtil.DeleteFile($"{StoragePath}/UserUnlock_{slotIndex}");
        FileUtil.DeleteFile($"{StoragePath}/UserAchievement_{slotIndex}");
        FileUtil.DeleteFile($"{StoragePath}/UserBackpackItem_{slotIndex}");
        FileUtil.DeleteFile($"{StoragePath}/UserBackpackCreature_{slotIndex}");
        FileUtil.DeleteFile($"{StoragePath}/UserRelationship_{slotIndex}");
        FileUtil.DeleteFile($"{StoragePath}/UserStory_{slotIndex}");
    }

    /// <summary>
    /// 仅读取当前槽位的故事演出拆分存档（UserStory_{slot}，文件不存在返回 null，不读主档与其它拆分档）
    /// 供测试工具查询故事已播记录等轻量场景使用
    /// </summary>
    public UserStoryBean LoadStoryData()
    {
        return GetSplitService<UserStoryBean>($"UserStory_{slotIndex}").Load(false);
    }

    /// <summary>
    /// 仅删除当前槽位的故事演出拆分存档（UserStory_{slot}），主档与其它拆分档不动
    /// 供测试工具（故事演出测试-清除存档故事数据）使用
    /// </summary>
    public void DeleteStoryData()
    {
        FileUtil.DeleteFile($"{StoragePath}/UserStory_{slotIndex}");
    }

    /// <summary>
    /// 仅移除当前槽位的单个故事已播记录（读拆分存档→移除→写回；文件不存在或未播该故事时不动）
    /// 供测试工具（故事演出测试-删除指定故事数据）使用
    /// </summary>
    /// <param name="storyId">故事ID（StoryInfo.id）</param>
    public void RemoveStoryData(long storyId)
    {
        var storyData = LoadStoryData();
        if (storyData == null)
            return;
        if (!storyData.GetDicPlayedStory().Remove(storyId))
            return;
        //拆分存档目录可能不存在(空槽),先建目录再写回
        FileUtil.CreateDirectory(StoragePath);
        GetSplitService<UserStoryBean>($"UserStory_{slotIndex}").Save(storyData);
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
