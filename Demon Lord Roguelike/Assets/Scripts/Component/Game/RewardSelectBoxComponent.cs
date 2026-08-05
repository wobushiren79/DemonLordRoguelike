using System.Collections.Generic;
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

    #region 道具闪耀粒子(Effect_Sparkle_1)
    //道具下 Effect_Sparkle_1 的粒子列表(含其所有子粒子)
    protected List<ParticleSystem> listSparklePS = new List<ParticleSystem>();
    //道具数据(开箱时取稀有度给粒子上色)
    protected ItemBean itemData;
    #endregion

    public void Awake()
    {
        boxTF = transform.Find("RewardBox_1");
        boxAnim = boxTF.GetComponent<Animator>();

        itemTF = transform.Find("RewardSelectBoxItem");
        itemRenderer = itemTF.Find("Renderer").GetComponent<SpriteRenderer>();
        itemNumText = itemTF.Find("RewardNum").GetComponent<TextMeshPro>();

        //运行时读取从天而降(Show)动画的实际时长 避免写死(动画时长变更后自动跟随)
        timeBoxShowAnim = AnimUtil.GetAnimClipLength(boxAnim, boxAnimShowName, timeBoxShowAnim);

        //缓存道具下的闪耀粒子(Effect_Sparkle_1 自身及其所有子粒子)
        var sparkleTF = itemTF.Find("Effect_Sparkle_1");
        if (sparkleTF != null)
            listSparklePS.AddRange(sparkleTF.GetComponentsInChildren<ParticleSystem>(true));
    }

    /// <summary>
    /// 初始化(仅数据初始化 不播落地动画;落地动画由 PlayShowAnim 统一触发)
    /// </summary>
    /// <param name="itemData"></param>
    public void InitData(ItemBean itemData)
    {
        //缓存道具数据
        this.itemData = itemData;
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
        //先隐藏箱子 落地动画由 PlayShowAnim 触发
        boxTF.gameObject.SetActive(false);
    }

    /// <summary>
    /// 预热显隐开关:预热时激活箱子(Animator暂停在Show第0帧)与道具用于渲染预热(shader编译/灯光/粒子),预热结束藏回道具
    /// </summary>
    /// <param name="isPrewarm">true进入预热显隐 false结束预热(藏回道具 箱子保持激活待播动画)</param>
    public void SetPrewarmActive(bool isPrewarm)
    {
        if (isPrewarm)
        {
            boxTF.gameObject.SetActive(true);
            //暂停动画 停在Show第0帧 仅做渲染预热
            boxAnim.speed = 0;
            //道具与闪耀粒子一并预热
            itemTF.gameObject.SetActive(true);
        }
        else
        {
            //道具重新藏起 开箱时再显示
            itemTF.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 播放箱子从天而降(Show)动画
    /// </summary>
    /// <param name="timeShowDelay">箱子延迟出现时间(多个箱子错开落地节奏)</param>
    public async Task PlayShowAnim(float timeShowDelay)
    {
        //延迟出现
        await new WaitForSeconds(timeShowDelay);
        //确保箱子激活(预热流程已激活时幂等) 并恢复动画播放 从Show第0帧开始落地
        boxTF.gameObject.SetActive(true);
        boxAnim.speed = 1;
        //落地后进入待机状态
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
        //按道具稀有度给闪耀粒子上色
        SetSparkleColorByRarity();
        //播放道具显示动画
        itemTF.DOLocalMove(new Vector3(0, 1.5f, 0), 0.5f);
        //等待道具动画播放完
        await new WaitForSeconds(timeOpen);
        return timeOpen;
    }

    #region 道具闪耀粒子(Effect_Sparkle_1)
    /// <summary>
    /// 按道具稀有度设置闪耀粒子(Effect_Sparkle_1)颜色(与 UIViewItem 道具背景同口径:RarityInfo.ui_board_color_item,缺配置回退白色)
    /// </summary>
    protected void SetSparkleColorByRarity()
    {
        if (listSparklePS.IsNull())
            return;
        Color rarityColor = Color.white;
        var rarityInfo = itemData != null ? RarityInfoCfg.GetItemData(itemData.rarity) : null;
        if (rarityInfo != null && !string.IsNullOrEmpty(rarityInfo.ui_board_color_item))
            rarityColor = ColorUtil.ParseHtmlString(rarityInfo.ui_board_color_item);
        //逐个粒子系统改 startColor(与 EffectHandler.ShowCreatureAscendCompleteEffect 同写法)
        for (int i = 0; i < listSparklePS.Count; i++)
        {
            var mainModule = listSparklePS[i].main;
            mainModule.startColor = rarityColor;
        }
    }
    #endregion
}
