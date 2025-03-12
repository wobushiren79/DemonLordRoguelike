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
        ui_Content.DestroyAllChild();
    }

    /// <summary>
    /// 初始化地图
    /// </summary>
    public void InitMap()
    {
        //获取用户数据
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlockData = userData.GetUserUnlockData();
        //所有已解锁的世界
        long[] keys = userUnlockData.unlockWorldData.Keys.ToArray();

        List<Vector2> listOldPos = new List<Vector2>();
        for (int i = 0; i < userUnlockData.unlockWorldMapRefreshNum; i++)
        {
            int randomWorldKey = UnityEngine.Random.Range(0, keys.Length);
            long randomWorldId = keys[randomWorldKey];
            //获取解锁世界数据
            UserUnlockWorldBean userUnlockWorldData = userUnlockData.GetUnlockWorldData(randomWorldId);
            //获取世界数据
            var worldInfo = GameWorldInfoCfg.GetItemData(randomWorldId);
            GameObject objItem = Instantiate(ui_Content.gameObject, ui_UIViewBasePortalItem.gameObject);
            objItem.ShowObj(true);
            UIViewBasePortalItem itemView = objItem.GetComponent<UIViewBasePortalItem>();
            //随机难度
            int randomDifficultyLevel = UnityEngine.Random.Range(1, userUnlockWorldData.difficultyLevel + 1);
            //随机地图位置
            Vector2 randomMapPos = GetRandomMapPos(listOldPos);
            listOldPos.Add(randomMapPos);
            //随机关卡长度
            var difficultyData = worldInfo.GetDifficultyData(randomDifficultyLevel);
            int randomLevelMax = UnityEngine.Random.Range(difficultyData.minLevelNum, difficultyData.maxLevelNum + 1);

            //设置数据
            itemView.SetData(worldInfo, randomDifficultyLevel, randomLevelMax, randomMapPos);
        }
    }

    /// <summary>
    /// 随机获取地图上的点位
    /// </summary>
    protected Vector2 GetRandomMapPos(List<Vector2> listOldPos)
    {
        float itemWidth = ui_UIViewBasePortalItem.rectTransform.rect.width / 2f;
        float itemHeight = ui_UIViewBasePortalItem.rectTransform.rect.height / 2f;

        float width = (ui_Content.rect.width / 2f) - itemWidth;
        float height = (ui_Content.rect.height / 2f) - itemHeight;

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
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

}
