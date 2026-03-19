using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class UIDialogBossShow : DialogView
{
    protected DialogBossShowBean dialogBossShowData;
    
    protected List<UIViewDialogBossShowItem> listBossItemShow = new List<UIViewDialogBossShowItem>();
    
    protected float timeScaleOrigin = 1f;
    protected float timeScaleSlow = 0.01f;
    /// <summary>
    /// 设置对话框数据并初始化 Boss 列表和动画
    /// </summary>
    /// <param name="dialogData">对话框基础数据，会被转换为 DialogBossShowBean 类型</param>
    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        dialogBossShowData = dialogData as DialogBossShowBean;
        InitListBoss();
        AnimForShow();
        timeScaleOrigin = Time.timeScale;
        Time.timeScale = timeScaleSlow;
    }

    /// <summary>
    /// 初始化 Boss 列表，根据配置数据动态创建 Boss 展示项
    /// 遍历 dialogBossShowData 中的 npcIds，为每个 Boss ID 实例化对应的 UI 项
    /// </summary>
    public void InitListBoss()
    {
        for (int i = 0; i < dialogBossShowData.npcIds.Count; i++)
        {
            long npcId = dialogBossShowData.npcIds[i];
            GameObject itemBossShow = Instantiate(ui_ContentList.gameObject, ui_UIViewDialogBossShowItem.gameObject);
            itemBossShow.gameObject.SetActive(true);
            var itemView = itemBossShow.GetComponent<UIViewDialogBossShowItem>();
            itemView.SetData(npcId);
            listBossItemShow.Add(itemView);
        }
    }

    /// <summary>
    /// 播放 Boss 展示的完整动画序列
    /// 流程：所有 Boss 项和标题淡入显示 -> 等待 2 秒 -> 所有 Boss 项和标题淡出隐藏 -> 销毁对话框
    /// 使用异步方式实现动画间的等待延迟
    /// </summary>
    public async void AnimForShow()
    {
        float timeForAnim = 1f;  // 动画持续时间
        float timeForWait = 2f;  // Boss 显示后的等待时间
        
        // 第一阶段：所有 Boss 项和标题淡入显示
        for (int i = 0; i < listBossItemShow.Count; i++)
        {
            var itemView = listBossItemShow[i];
            itemView.AnimForShow(timeForAnim, 1);  // state=1 表示显示
        }
        AnimForTitle(1, timeForAnim);

        await new WaitForSecondsRealtime(timeForWait);

        // 第二阶段：所有 Boss 项和标题淡出隐藏
        for (int i = 0; i < listBossItemShow.Count; i++)
        {
            var itemView = listBossItemShow[i];
            itemView.AnimForShow(timeForAnim, 0);  // state=0 表示隐藏
        }
        AnimForTitle(0, timeForAnim);
        await new WaitForSecondsRealtime(timeForAnim);   
        Time.timeScale = timeScaleOrigin;
        DestroyDialog();
    }

    /// <summary>
    /// 控制标题的显示/隐藏动画（淡入淡出 + 位移动画）
    /// </summary>
    /// <param name="state">动画状态：1=显示（从右侧淡入），0=隐藏（向左侧淡出）</param>
    /// <param name="timeForAnim">动画持续时间</param>
    public void AnimForTitle(int state, float timeForAnim)
    {
        float startAlpha;       // 起始透明度
        float endAlpha;         // 结束透明度
        Vector2 startPos;       // 起始位置
        Vector2 endPos;         // 结束位置
        Ease easeType;          // 缓动函数类型
        
        if (state == 1)
        {
            // 显示状态：从透明到不透明，从右侧滑入
            startAlpha = 0;
            endAlpha = 1;
            startPos = new Vector2(1000, 0);
            endPos = Vector2.zero;
            easeType = Ease.OutExpo;  // 指数缓出，快速进入后减速
        }
        else
        {
            // 隐藏状态：从不透明到透明，向左滑出
            startAlpha = 1;
            endAlpha = 0;
            startPos = Vector2.zero;
            endPos = new Vector2(-1000, 0);
            easeType = Ease.InExpo;   // 指数缓入，慢速启动后加速
        }
        
        // 设置初始状态
        ui_TitlePro.alpha = startAlpha;
        ui_TitlePro.rectTransform.anchoredPosition = startPos;

        // 透明度渐变动画
        ui_TitlePro
            .DOFade(endAlpha, timeForAnim)
            .SetEase(easeType)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);

        // 位移动画
        ui_TitlePro.rectTransform
            .DOAnchorPos(endPos, timeForAnim)
            .SetEase(easeType)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true);
    }
}