using UnityEngine;
using UnityEngine.UI;

public partial class AudioHandler
{
    public override void Awake()
    {
        base.Awake();
        UIHandler.Instance.AddOnClickAction(ActionForUIOnClick);
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
