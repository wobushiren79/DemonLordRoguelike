using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameDataManager : BaseManager
{
    public UserDataBean userData;
    private UserDataService userDataService;

    /// <summary>
    /// 测试模拟模式(通用测试标记):为 true 时 SaveUserData 一律不落盘,全程只在内存中生效。
    /// 由各测试入口(如献祭升级测试、魔物进阶测试)在读取真实存档并 SetUserData 后置位;
    /// 正式游戏流程永不置位,故不影响真实存档。作为"测试不落盘"的单一真实源,任何存档路径都自动跳过写盘。
    /// </summary>
    public bool isTestSimulation;

    public void Awake()
    {
    }

    /// <summary>
    /// 获取用户数据
    /// </summary>
    public UserDataBean GetUserData()
    {
        if (userData == null)
        {
            userData = new UserDataBean();
        }
        return userData;
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    public void SetUserData(UserDataBean targetUserData)
    {
        userData = targetUserData;
    }

    /// <summary>
    /// 保存用户数据
    /// </summary>
    public UserDataBean SaveUserData()
    {
        if (userData != null)
        {
            SaveUserData(userData);
        }
        return userData;
    }

    public void SaveUserData(UserDataBean targetUserData)
    {
        if (targetUserData == null)
            return;
        //测试模拟模式:一律不落盘,所有存档路径在此统一跳过写盘(全程内存模拟)
        if (isTestSimulation)
            return;
        userDataService ??= new UserDataService();
        userDataService.ChangeSlot(targetUserData.saveIndex);
        //UserDataService.Save 内部会一并保存拆分的解锁/成就独立文件
        userDataService.Save(targetUserData);
    }

    /// <summary>
    /// 删除用户数据
    /// </summary>
    public void DeleteUserData(UserDataBean targetUserData)
    {
        if (targetUserData == null)
            return;
        userDataService ??= new UserDataService();
        userDataService.ChangeSlot(targetUserData.saveIndex);
        //UserDataService.Delete 内部会一并删除拆分的解锁/成就独立文件
        userDataService.Delete();
    }

    /// <summary>
    /// 读取用户数据
    /// </summary>
    public void LoadUserData(int index, Action<int, UserDataBean> actionForComplete)
    {
        try
        {
            userDataService ??= new UserDataService();
            userDataService.ChangeSlot(index);
            //UserDataService.Load 内部会加载并注入拆分的解锁/成就数据
            UserDataBean data = userDataService.Load();
            if (data == null)
                data = new UserDataBean();
            actionForComplete?.Invoke(index, data);
        }
        catch (Exception e)
        {
            LogUtil.LogError($"读取用户数据失败:{e.Message}");
            UserDataBean errorData = new UserDataBean();
            errorData.isErrorData = true;
            actionForComplete?.Invoke(index, errorData);
        }
    }
}
