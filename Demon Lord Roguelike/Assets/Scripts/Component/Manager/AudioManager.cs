using System.Collections.Generic;
using UnityEngine;

public partial class AudioManager
{
    #region 通用点击音效排除名单（默认全部按钮响，命中以下名单的按钮保持静音）
    /// <summary>
    /// 通用点击音效——按 GameObject 名的排除名单（当前暂时清空）。
    /// 命中的按钮不播放 sound_btn_1；用于按钮 Image 共用通用枠 sprite、无法用 sprite 名区分时按 GameObject 名排除。
    /// 待实机点击后观察 ActionForUIOnClick 打印的真实 obj 名，再按需补入需要静音的频繁/连续操作类按钮。
    /// </summary>
    public HashSet<string> listExcludeUIClickByName = new HashSet<string>
    {
        //暂时清空：待实机点按钮观察 ActionForUIOnClick 打印的真实 obj 名后再按需补入
    };

    /// <summary>
    /// 通用点击音效——按 sprite 名的排除名单（当前暂时清空）。
    /// 命中的按钮不播放 sound_btn_1；仅适用于某 sprite 被独占用途(如页签/单选专用枠)、按 sprite 名排除不会误伤通用按钮的情况。
    /// 待实机点击后观察 ActionForUIOnClick 打印的真实 sprite 名，再按需补入需要静音的按钮。
    /// </summary>
    public HashSet<string> listExcludeUIClickBySprite = new HashSet<string>
    {
        //暂时清空：待实机点按钮观察 ActionForUIOnClick 打印的真实 sprite 名后再按需补入
    };
    #endregion
}
