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

        //��������
        GameControlHandler.Instance.SetBaseControl(false);
        //��������ͷ
        CameraHandler.Instance.SetBasePortalCamera(int.MaxValue, true);
        //�ر�Զ��
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        //��ʼ����ͼ
        InitMap();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        //�ر�Զ��
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
        ui_Content.DestroyAllChild();
    }

    /// <summary>
    /// ��ʼ����ͼ
    /// </summary>
    public void InitMap()
    {
        //��ȡ�û�����
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlockData = userData.GetUserUnlockData();
        //�����ѽ���������
        long[] keys = userUnlockData.unlockWorldData.Keys.ToArray();

        List<Vector2> listOldPos = new List<Vector2>();
        for (int i = 0; i < userUnlockData.unlockWorldMapRefreshNum; i++)
        {
            int randomWorldKey = UnityEngine.Random.Range(0, keys.Length);
            long randomWorldId = keys[randomWorldKey];
            //��ȡ������������
            UserUnlockWorldBean userUnlockWorldData = userUnlockData.GetUnlockWorldData(randomWorldId);
            //��ȡ��������
            var worldInfo = GameWorldInfoCfg.GetItemData(randomWorldId);
            GameObject objItem = Instantiate(ui_Content.gameObject, ui_UIViewBasePortalItem.gameObject);
            objItem.ShowObj(true);
            UIViewBasePortalItem itemView = objItem.GetComponent<UIViewBasePortalItem>();
            //����Ѷ�
            int randomDifficultyLevel = UnityEngine.Random.Range(1, userUnlockWorldData.difficultyLevel + 1);
            //�����ͼλ��
            Vector2 randomMapPos = GetRandomMapPos(listOldPos);
            listOldPos.Add(randomMapPos);

            //��������
            itemView.SetData(worldInfo, randomDifficultyLevel, randomMapPos);
        }
    }

    /// <summary>
    /// �����ȡ��ͼ�ϵĵ�λ
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
            if((xRandom > itemOldPos.x - itemWidth)
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
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

}
