using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class RewardSelectBoxComponent : BaseMonoBehaviour
{
    protected Transform itemTF;
    protected SpriteRenderer itemRenderer = null;
    protected TextMeshPro itemNumText = null;

    protected Transform boxTF;
    protected Animator boxAnim;
    protected string boxAnimState = "state";
    //箱子从天而降(Show)动画状态名 用于运行时读取该动画实际时长
    protected string boxAnimShowName = "Show";
    //箱子从天而降(Show)动画时长 Awake 时从 Animator 动态读取 此处仅作读取失败时的兜底默认值
    protected float timeBoxShowAnim = 0.5f;

    public RewardSelectBoxStateEnum rewardSelectBoxState;

    public void Awake()
    {
        boxTF = transform.Find("RewardBox_1");
        boxAnim = boxTF.GetComponent<Animator>();

        itemTF = transform.Find("RewardSelectBoxItem");
        itemRenderer = itemTF.Find("Renderer").GetComponent<SpriteRenderer>();
        itemNumText = itemTF.Find("RewardNum").GetComponent<TextMeshPro>();

        //运行时读取从天而降(Show)动画的实际时长 避免写死(动画时长变更后自动跟随)
        timeBoxShowAnim = AnimUtil.GetAnimClipLength(boxAnim, boxAnimShowName, timeBoxShowAnim);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="tiemShowDelay">箱子延迟出现时间</param>
    public async Task InitData(ItemBean itemData, float tiemShowDelay)
    {
        //设置箱子状态
        rewardSelectBoxState = RewardSelectBoxStateEnum.Idle;
        //设置道具图标
        IconHandler.Instance.SetItemIcon(itemData.itemsInfo.icon_res, itemData.itemsInfo.icon_rotate_z, itemRenderer);
        //设置道具数量 数量大于1才显示
        if (itemData.itemNum > 1)
        {
            itemNumText.gameObject.SetActive(true);
            itemNumText.text = itemData.itemNum.ToString();
        }
        else
        {
            itemNumText.gameObject.SetActive(false);
        }
        //先隐藏道具 点选之后再显示
        itemTF.gameObject.SetActive(false);
        //延迟出现箱子
        boxTF.gameObject.SetActive(false);
        await new WaitForSeconds(tiemShowDelay);
        boxTF.gameObject.SetActive(true);
        //箱子进入待机状态
        boxAnim.SetInteger(boxAnimState, 1);
        //等待从天而降(Show)动画播放完毕 播放落地音效
        await new WaitForSeconds(timeBoxShowAnim);
        AudioHandler.Instance.PlaySound(AudioEnum.sound_hit_6);
    }

    /// <summary>
    /// 打开箱子
    /// </summary>
    public async Task<float> OpenBox()
    {
        //设置箱子状态
        rewardSelectBoxState = RewardSelectBoxStateEnum.Open;
        var timeOpen = await OpenBoxBase();
        return timeOpen;
    }

    /// <summary>
    /// 打开箱子-最终展示
    /// </summary>
    public async Task<float> OpenBoxForPreview()
    {
        //如果已经打开则不处理
        if (rewardSelectBoxState == RewardSelectBoxStateEnum.Open)
        {
            return 0;
        }
        //设置箱子状态
        rewardSelectBoxState = RewardSelectBoxStateEnum.OpenPreview;
        var timeOpen = await OpenBoxBase();
        return timeOpen;
    }

    /// <summary>
    /// 打开箱子-基础
    /// </summary>
    protected async Task<float> OpenBoxBase()
    {
        float timeOpen = 1f;
        //箱子播放打开动画
        boxAnim.SetInteger(boxAnimState, 2);
        //播放开箱音效(音量倍率由配置表 AudioInfo 的 volume_scale 控制)
        AudioHandler.Instance.PlaySound(AudioEnum.sound_set_1);
        //显示道具
        itemTF.gameObject.SetActive(true);
        //播放道具显示动画
        itemTF.DOLocalMove(new Vector3(0, 1.5f, 0), 0.5f);
        //等待道具动画播放完
        await new WaitForSeconds(timeOpen);
        return timeOpen;
    }
}
