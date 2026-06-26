using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIBasePortal : BaseUIComponent
{

    public override void OpenUI()
    {
        base.OpenUI();
        //开启控制
        GameControlHandler.Instance.SetBaseControl(false);
        //开启摄像头
        CameraHandler.Instance.SetBasePortalCamera(int.MaxValue, true);
        //关闭远景
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        //初始化地图
        InitMap();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        //关闭远景
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
        ClearMap();
    }

    /// <summary>
    /// 清理地图
    /// </summary>
    public void ClearMap()
    {
        ui_Content.DestroyAllChild();
    }

    /// <summary>
    /// 初始化地图
    /// 已缓存的传送门世界保持不变直接显示; 当解锁研究使显示数量(showCount)增大时, 仅按缺口补足新世界,
    /// 既不重置已有世界(避免每次打开重新洗牌), 又能让新解锁的数量立即生效(避免忽略 showCount).
    /// </summary>
    public void InitMap()
    {
        //获取用户数据
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlockData = userData.GetUserUnlockData();
        UserTempBean userTempData = userData.GetUserTempData();
        //获取显示数量(基础值 portalShowMax + 传送门数量研究等级)
        int showCount = userUnlockData.GetUnlockPortalShowCount();

        List<Vector2> listOldPos = new List<Vector2>();
        //先显示已缓存世界, 并记录其位置避免后续补足的世界与之重叠
        List<GameWorldInfoRandomBean> listRandomData = userTempData.listPortalWorldInfoRandomData;
        int cachedCount = listRandomData == null ? 0 : listRandomData.Count;
        for (int i = 0; i < cachedCount; i++)
        {
            listOldPos.Add(listRandomData[i].uiPosition.GetVector());
            SetItemMapData(listRandomData[i]);
        }
        //按缺口补足世界(解锁研究提升数量后, 旧世界不变, 仅新增差额部分)
        for (int i = cachedCount; i < showCount; i++)
        {
            GameWorldInfoRandomBean gameWorldInfoRandomData = CreateRandomPortalWorld(userUnlockData, listOldPos);
            SetItemMapData(gameWorldInfoRandomData);
            userTempData.AddPortalWorldInfoRandomData(gameWorldInfoRandomData);
        }
    }

    /// <summary>
    /// 随机创建一个传送门世界数据(随机世界id/游戏类型/地图位置/图标种子)
    /// </summary>
    /// <param name="userUnlockData">用户解锁数据(取已解锁世界id池)</param>
    /// <param name="listOldPos">已占用的地图位置, 用于避免新世界与其重叠(会把新位置加入)</param>
    /// <returns>新的传送门世界随机数据</returns>
    protected GameWorldInfoRandomBean CreateRandomPortalWorld(UserUnlockBean userUnlockData, List<Vector2> listOldPos)
    {
        //所有已解锁的世界
        List<long> unlockWorldIds = userUnlockData.GetUnlockGameWorldIds();
        //随机一个世界
        int randomWorldKey = UnityEngine.Random.Range(0, unlockWorldIds.Count);
        long randomWorldId = unlockWorldIds[randomWorldKey];
        GameWorldInfoRandomBean gameWorldInfoRandomData = new GameWorldInfoRandomBean();
        //设置游戏类型随机
        gameWorldInfoRandomData.SetGameFightTypeRandom(randomWorldId);
        //随机地图位置
        Vector2 randomMapPos = GetRandomMapPos(listOldPos);
        listOldPos.Add(randomMapPos);
        gameWorldInfoRandomData.uiPosition = new Vector2Bean(randomMapPos);
        //设置地图icon种子
        gameWorldInfoRandomData.iconSeed = UnityEngine.Random.Range(0, int.MaxValue);
        return gameWorldInfoRandomData;
    }

    /// <summary>
    /// 设置地图数据
    /// </summary>
    /// <param name="gameWorldInfoRandomData"></param>
    public void SetItemMapData(GameWorldInfoRandomBean gameWorldInfoRandomData)
    {
        //获取世界数据
        var worldInfo = GameWorldInfoCfg.GetItemData(gameWorldInfoRandomData.worldId);
        GameObject objItem = Instantiate(ui_Content.gameObject, ui_UIViewBasePortalItem.gameObject);
        objItem.ShowObj(true);
        UIViewBasePortalItem itemView = objItem.GetComponent<UIViewBasePortalItem>();
        //设置数据
        itemView.SetData(worldInfo, gameWorldInfoRandomData);
    }

    /// <summary>
    /// 随机获取地图上的点位
    /// </summary>
    protected Vector2 GetRandomMapPos(List<Vector2> listOldPos)
    {
        float itemWidth = ui_UIViewBasePortalItem.rectTransform.rect.width / 2f;
        float itemHeight = ui_UIViewBasePortalItem.rectTransform.rect.height / 2f;

        float width = (ui_Content.rect.width / 2f * 0.9f) - itemWidth;
        float height = (ui_Content.rect.height / 2f * 0.9f) - itemHeight;

        float xRandom = UnityEngine.Random.Range(-width, width);
        float yRandom = UnityEngine.Random.Range(-height, height);

        for (int i = 0; i < listOldPos.Count; i++)
        {
            var itemOldPos = listOldPos[i];
            if ((xRandom > itemOldPos.x - itemWidth)
                && (xRandom < itemOldPos.x + itemWidth)
                && (yRandom > itemOldPos.y - itemHeight)
                && (yRandom < itemOldPos.y + itemHeight))
            {
                return GetRandomMapPos(listOldPos);
            }
        }
        return new Vector2(xRandom, yRandom);
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnRefresh)
        {
            OnClickForRefresh();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    /// <summary>
    /// 点击刷新
    /// </summary>
    public void OnClickForRefresh()
    {
        //获取用户数据
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserTempBean userTempData = userData.GetUserTempData();
        userTempData.ClearPortalWorldInfoRandomData();
        ClearMap();
        InitMap();
    }
}
