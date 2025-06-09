using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AudioHandler
{
    public override void Awake()
    {
        base.Awake();
        UIHandler.Instance.AddOnClickAction(ActionForUIOnClick);
    }

    public void PlayMusicForMain()
    {
        List<int> listMusicId = new List<int>
        {
            1200001
        };
        PlayMusicListForLoop(listMusicId);
    }

    public void PlayMusicForGaming()
    {
        // List<int> listMusicId = new List<int>
        // {
        //     1100001,1100002,1100003,1100004,1100005,1100006,1100007
        // };
        // PlayMusicListForLoop(listMusicId);
    }

    public void PlayMusicForFight()
    {
        List<int> listMusicId = new List<int>
        {
            1000001,1000002,1000003,1000004,1000005,1000006
        };
        PlayMusicListForLoop(listMusicId);
    }

    /// <summary>
    /// UI点击回调
    /// </summary>
    public void ActionForUIOnClick(GameObject targetObj)
    {
        Button tagetButton = targetObj.GetComponent<Button>();
        if (tagetButton != null)
        {
            Image targetImage = tagetButton.image;
            if (targetImage.sprite == null)
            {
                return;
            }
            LogUtil.Log($"ActionForUIOnClick {targetImage.sprite.name}");
            //通用点击
            //如果是退出点击
            if (targetImage.name.Equals("ViewExit"))
            {
                PlaySound(4);
                return;
            }
            if (manager.listCommonUIClick.Contains(targetImage.sprite.name))
            {
                PlaySound(3);
            }
        }
    }
}
