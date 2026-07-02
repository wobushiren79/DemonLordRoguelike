using System.Collections.Generic;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 孵化缸进阶详情-单条BUFF增益item:展示可能生成的BUFF名字与概率,带出现/消失/移动 DOTween 动画。
/// </summary>
public partial class UIViewCreatureVatAscendBuffItem : BaseUIView
{
    #region 字段
    //淡入淡出用的CanvasGroup(运行时按需补挂)
    protected CanvasGroup canvasGroup;
    //当前出现/消失序列(显式持有以便整体 Kill,避免 Sequence(target=null) 被 DOKill 漏杀致 OnComplete 误触发)
    protected Sequence currentSeq;
    //移动到目标位置的时长
    protected const float MoveAnimTime = 0.25f;
    //出现动画时长
    protected const float AppearAnimTime = 0.35f;
    //消失动画时长
    protected const float DisappearAnimTime = 0.2f;
    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        //淡入淡出需要CanvasGroup,模板未挂则运行时补一个
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置数据:BUFF名字 + 生成概率 + 稀有度配色(名字字体/BG背景) + BG悬浮提示(BUFF内容)。
    /// </summary>
    /// <param name="chance">该BUFF命中概率数据(名字/概率/buffId)</param>
    /// <param name="rarity">该BUFF所属(升阶后)稀有度,用于名字字体与BG背景配色</param>
    public void SetData(CreatureAscendBuffChanceStruct chance, int rarity)
    {
        ui_AscendBuffItemName.text = chance.name;
        ui_AscendBuffItemRate.text = $"{Mathf.RoundToInt(chance.rate)}%";
        //概率文本按自身百分比分段配色(复用献祭成功率进度条同口径:ColorUtil.GetProgressColor,rate为0~100需归一化)
        ui_AscendBuffItemRate.color = ColorUtil.GetProgressColor(chance.rate / 100f);

        //名字字体 + BG背景统一用稀有度配色(与 UIViewBuffShowItem 同口径:RarityInfo.buff_color)
        Color rarityColor = GetRarityColor(rarity);
        ui_AscendBuffItemName.color = rarityColor;
        if (ui_BG_Image != null)
            ui_BG_Image.color = rarityColor;

        //BG悬浮提示:展示该BUFF内容。未解锁「进阶增益范围预览」研究时数值用???替代,解锁后显示 min~max 范围
        if (ui_BG_PopupButtonCommonView != null)
            ui_BG_PopupButtonCommonView.SetData(GetBuffContentForPreview(chance), PopupEnum.Text);
    }

    /// <summary>
    /// 取稀有度配色(RarityInfo.buff_color,与 UIViewBuffShowItem 同口径;解析失败回退白色)。
    /// </summary>
    /// <param name="rarity">稀有度值</param>
    /// <returns>稀有度颜色</returns>
    protected Color GetRarityColor(int rarity)
    {
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        //颜色解析复用 ColorUtil.ParseHtmlString(解析失败回退白色)
        return rarityInfo != null ? ColorUtil.ParseHtmlString(rarityInfo.buff_color) : Color.white;
    }

    /// <summary>
    /// 预览用BUFF内容文案:随机增益兜底项(buffId≤0,无具体BUFF)给通用说明;具体BUFF按「进阶增益范围预览」研究是否解锁分档:
    /// <para>未解锁:仅展示效果描述,把 {Percentage}/{Time_S}/{KillNum} 等占位参数统一替成???(数值保密)。</para>
    /// <para>已解锁:随机增益属性({Percentage})显示 min~max 范围(素材命中时下限抬高),其余固定占位参数显示实际值。</para>
    /// </summary>
    /// <param name="chance">该BUFF命中概率数据(含 buffId 与素材抬高的数值下限)</param>
    /// <returns>悬浮提示内容文案</returns>
    protected string GetBuffContentForPreview(CreatureAscendBuffChanceStruct chance)
    {
        if (chance.buffId <= 0)
            return "随机生成一条该品质增益";
        var buffInfo = BuffInfoCfg.GetItemData(chance.buffId);
        if (buffInfo == null)
            return "???";
        //未解锁「进阶增益范围预览」研究:占位参数统一替成???
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        if (!userUnlock.CheckIsUnlock(UnlockEnum.CreatureVatBuffPreview))
            return Regex.Replace(buffInfo.content_language, @"\{[^}]+\}", "???");
        //已解锁:数值范围+固定条件实际值
        return BuildUnlockedRangeContent(buffInfo, chance);
    }

    /// <summary>
    /// 已解锁「进阶增益范围预览」时的内容文案:随机增益属性({Percentage})显示 min~max 范围,
    /// 触发时间/前置条件(击杀数/承伤/造伤/血量阈值)等固定占位参数显示实际值(与 UIViewBuffShowItem 同口径)。
    /// </summary>
    /// <param name="buffInfo">BUFF配置</param>
    /// <param name="chance">命中概率数据(取素材抬高的百分比下限)</param>
    /// <returns>带数值范围的内容文案</returns>
    protected string BuildUnlockedRangeContent(BuffInfoBean buffInfo, CreatureAscendBuffChanceStruct chance)
    {
        Dictionary<TextReplaceEnum, string> dicReplace = new Dictionary<TextReplaceEnum, string>()
        {
            //唯一随机值:进阶增益属性百分比,按整数百分点闭区间 [min,max] 显示
            {TextReplaceEnum.Percentage, GetPercentageRangeStr(buffInfo, chance.floorValueRate)},
            //触发时间为固定配置值(不随机),直接显示
            {TextReplaceEnum.Time_S, $"{Mathf.FloorToInt(buffInfo.trigger_time)}"},
        };
        //前置条件数值(击杀数/承伤/造伤/血量阈值)均为固定值,按前置实例类型逐项映射(与 UIViewBuffShowItem 同口径)
        var preInfo = buffInfo.GetPreInfo();
        if (!preInfo.IsNull())
        {
            foreach (var itemData in preInfo)
            {
                var buffPreInfo = BuffPreInfoCfg.GetItemData(itemData.Key);
                var buffPreEntity = BuffHandler.Instance.manager.GetBuffPreEntity(buffPreInfo);
                if (buffPreEntity is BuffPreEntityForKillNum)
                    dicReplace[TextReplaceEnum.KillNum] = $"{Mathf.FloorToInt(itemData.Value)}";
                else if (buffPreEntity is BuffPreEntityForUnderAttackDamage)
                    dicReplace[TextReplaceEnum.UnderAttackDamage] = $"{Mathf.FloorToInt(itemData.Value)}";
                else if (buffPreEntity is BuffPreEntityForAttackDamage)
                    dicReplace[TextReplaceEnum.AttackDamage] = $"{Mathf.FloorToInt(itemData.Value)}";
                else if (buffPreEntity is BuffPreEntityForHPRateLess)
                    dicReplace[TextReplaceEnum.HPRateLess] = $"{MathUtil.GetPercentage(itemData.Value, 2)}";
            }
        }
        return TextHandler.Instance.GetTextReplace(buffInfo.content_language, dicReplace);
    }

    /// <summary>
    /// 计算进阶增益属性百分比的展示范围文案(与 BuffBean.CreateRandomWithFloor 同口径):
    /// 按整数百分点随机,下限抬到 max(配置min, 素材下限);上下限相同显示单值,否则显示 min~max。
    /// </summary>
    /// <param name="buffInfo">BUFF配置(取 trigger_value_rate_min~trigger_value_rate)</param>
    /// <param name="floorValueRate">素材命中抬高的百分比下限(无素材命中为0)</param>
    /// <returns>形如 "10~20" 或 "20" 的范围文案(不含百分号,百分号在描述模板里)</returns>
    protected string GetPercentageRangeStr(BuffInfoBean buffInfo, float floorValueRate)
    {
        int rateFloor = Mathf.RoundToInt(Mathf.Max(buffInfo.trigger_value_rate_min, floorValueRate) * 100);
        int rateMax = Mathf.Max(rateFloor, Mathf.RoundToInt(buffInfo.trigger_value_rate * 100));
        return rateFloor == rateMax ? $"{rateFloor}" : $"{rateFloor}~{rateMax}";
    }
    #endregion

    #region 动画相关
    /// <summary>
    /// 出现动画:从上方掉落落入 + BackOut 缩放弹出 + 淡入(以当前 anchoredPosition 作为落点)。
    /// </summary>
    public void PlayAppear()
    {
        KillAnim();
        gameObject.SetActive(true);
        Vector2 endPos = rectTransform.anchoredPosition;
        //起点:落点上方 + 缩小 + 透明
        rectTransform.anchoredPosition = endPos + new Vector2(0, 36);
        transform.localScale = Vector3.one * 0.3f;
        canvasGroup.alpha = 0f;

        currentSeq = DOTween.Sequence();
        currentSeq.Append(rectTransform.DOAnchorPos(endPos, AppearAnimTime).SetEase(Ease.OutBack));
        currentSeq.Join(transform.DOScale(Vector3.one, AppearAnimTime).SetEase(Ease.OutBack));
        currentSeq.Join(canvasGroup.DOFade(1f, AppearAnimTime * 0.7f));
    }

    /// <summary>
    /// 移动到目标位置(复用已显示item时平滑滑动,同时确保缩放/透明已复位)。
    /// </summary>
    /// <param name="targetPos">目标 anchoredPosition</param>
    public void MoveTo(Vector2 targetPos)
    {
        KillAnim();
        gameObject.SetActive(true);
        transform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
        rectTransform.DOAnchorPos(targetPos, MoveAnimTime).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 消失动画:BackIn 缩小 + 淡出,结束后隐藏自身。
    /// </summary>
    public void PlayDisappear()
    {
        if (!gameObject.activeSelf)
            return;
        KillAnim();
        currentSeq = DOTween.Sequence();
        currentSeq.Append(transform.DOScale(Vector3.one * 0.3f, DisappearAnimTime).SetEase(Ease.InBack));
        currentSeq.Join(canvasGroup.DOFade(0f, DisappearAnimTime * 0.9f));
        currentSeq.OnComplete(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// 立即隐藏(无动画,用于关闭界面/批量清理)
    /// </summary>
    public void HideImmediately()
    {
        KillAnim();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 杀掉当前序列与位置/缩放/透明上的所有补间,避免动画叠加冲突及消失序列 OnComplete 误触发。
    /// </summary>
    protected void KillAnim()
    {
        //先整体杀掉序列(序列 target 为 null,单纯 DOKill(transform) 杀不掉它的 OnComplete)
        if (currentSeq != null && currentSeq.IsActive())
            currentSeq.Kill();
        currentSeq = null;
        rectTransform.DOKill();
        transform.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
    #endregion
}
