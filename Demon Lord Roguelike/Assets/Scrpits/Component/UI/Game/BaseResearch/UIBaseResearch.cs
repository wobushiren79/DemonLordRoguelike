﻿

using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIBaseResearch : BaseUIComponent
{
    protected List<UIViewBaseResearchItem> listResearchItemView = new List<UIViewBaseResearchItem>();
    protected List<GameObject> listLineObj = new List<GameObject>();
    public float SpeedForChangeContentSize = 10;
    protected Tween animForShowUnlockEffect;
    protected ResearchInfoTypeEnum researchInfoType;
    public override void CloseUI()
    {
        base.CloseUI();
        ClearData(true);
        ClearAnim();
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_EffectUnlock.gameObject.SetActive(false);
        this.researchInfoType = ResearchInfoTypeEnum.Strengthen;
        InitResearchItems(researchInfoType);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if(viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
        else if(inputType == InputActionUIEnum.ScrollWheel)
        {
            Vector2 wheelData = callback.ReadValue<Vector2>();
            ChangeContentSize(wheelData.y);
        }
    }

    public void ChangeContentSize(float changeSize)
    {
        ui_Content.transform.localScale +=  Vector3.one * (changeSize * Time.deltaTime * SpeedForChangeContentSize);
        Vector3 clampedPosition = ui_Content.transform.localScale;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, 0.5f, 1f);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, 0.5f, 1f);
        ui_Content.transform.localScale = clampedPosition;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void InitResearchItems()
    {        
        InitResearchItems(researchInfoType);
    }

    public void InitResearchItems(ResearchInfoTypeEnum researchInfoType)
    {        
        ClearData(false);
        List<ResearchInfoBean> listData = ResearchInfoCfg.GetResearchInfoByType(researchInfoType);
        //创建item
        listData.ForEach((index, itemData) =>
        {
            if (CheckPreIsUnlock(itemData))
            {
                //创建研究item
                CreateResearchItem(index, itemData);
                //创建连线
                CreateLine(itemData);
            }
        });
    }


    /// <summary>
    /// 检测前置是否解锁
    /// </summary>
    public bool CheckPreIsUnlock(ResearchInfoBean researchInfo)
    {
        long[] preIds =  researchInfo.GetPreResearchIds();
        if(preIds.IsNull())
        {
            return true;
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlockData = userData.GetUserUnlockData();
        for (int i = 0; i < preIds.Length; i++)
        {
            long preId = preIds[i];
            var preResearchInfo = ResearchInfoCfg.GetItemData(preId);
            bool isUnlock = userUnlockData.CheckIsUnlock(preResearchInfo.unlock_id);
            if (isUnlock == false)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 创建研究item
    /// </summary>
    public void CreateResearchItem(int index,ResearchInfoBean researchInfo)
    {
            UIViewBaseResearchItem itemView;
            if (index < listResearchItemView.Count)
            {
                 itemView = listResearchItemView[index];
            }
            else
            {
                var newResearchItemObj = Instantiate(ui_Content.gameObject, ui_UIViewBaseResearchItem.gameObject);
                itemView =  newResearchItemObj.GetComponent<UIViewBaseResearchItem>();
                listResearchItemView.Add(itemView);
            }
            itemView.gameObject.SetActive(true);
            itemView.SetData(researchInfo);
    }

    /// <summary>
    /// 设置连线
    /// </summary>
    public void CreateLine(ResearchInfoBean researchInfo)
    {                   
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlockData = userData.GetUserUnlockData();
        Vector2 itemPosition = new Vector2(researchInfo.position_x, researchInfo.position_y);
        long[] preResearchIds = researchInfo.GetPreResearchIds();
        if (preResearchIds.IsNull())
            return;
        bool isUnlockSelf = userUnlockData.CheckIsUnlock(researchInfo.unlock_id);
        ColorUtility.TryParseHtmlString("#382087",out Color color1);
        ColorUtility.TryParseHtmlString("#3C0031",out Color color2Unlock);
        ColorUtility.TryParseHtmlString("#3C003100",out Color color2Lock);

        preResearchIds.ForEach((index, value) =>
        {
            var researchInfo = ResearchInfoCfg.GetItemData(value);
            GameObject objItemLine = null;
            //查询所有缓存的线
            for (int i = 0; i < listLineObj.Count; i++)
            {
                var itemLine = listLineObj[i];
                if (itemLine.activeSelf == false)
                {
                    objItemLine = itemLine;
                    break;
                }
            }
            //如果没有缓存的线 则创建一个新的
            if (objItemLine == null)
            {
                objItemLine = Instantiate(ui_Line.gameObject, ui_Line_Item.gameObject);
            }
            objItemLine.SetActive(true);      
            listLineObj.Add(objItemLine);
            RectTransform rtfLine = (RectTransform)objItemLine.transform;
            Image ivLine = objItemLine.GetComponent<Image>();
            Vector2 originalPosition = new Vector2(researchInfo.position_x, researchInfo.position_y);
            //设置线段位置
            Vector2 targetPosition = UGUIUtil.GetRootPosForUI(originalPosition, ui_Content, ui_Line);
            rtfLine.anchoredPosition = targetPosition;
            //设置线段长度
            float lineLength = Vector2.Distance(itemPosition, originalPosition);
            rtfLine.sizeDelta = new Vector2(lineLength, 100);
            //设置线段角度
            float angle = VectorUtil.GetAngleForXLine(originalPosition, itemPosition);
            rtfLine.transform.eulerAngles = new Vector3(0, 0, angle);
            //设置线段颜色
            Material uniqueMat = new Material(ivLine.material);

            uniqueMat.SetColor("_Color1", color1);
            uniqueMat.SetFloat("_Segments", 2);
            if (isUnlockSelf)
            {
                uniqueMat.SetColor("_Color2", color2Unlock);
                uniqueMat.SetFloat("_WaveSpeed", 1);
                uniqueMat.SetFloat("_Amplitude", 0.05f);
            }
            else
            {
                uniqueMat.SetColor("_Color2", color2Lock);
                uniqueMat.SetFloat("_WaveSpeed", 10);
                uniqueMat.SetFloat("_Amplitude", 0.2f);      
            }
            ivLine.material = uniqueMat;
        });
    }

    /// <summary>
    /// 清理动画
    /// </summary>
    public void ClearAnim()
    {
        if (animForShowUnlockEffect != null && animForShowUnlockEffect.IsPlaying())
        {
            animForShowUnlockEffect.Kill();
        }
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    /// <param name="isDestory"></param>
    public void ClearData(bool isDestory)
    {
        listResearchItemView.ForEach((index, itemView) =>
        {
            if (isDestory)
            {
                DestroyImmediate(itemView.gameObject);
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        });
        listLineObj.ForEach((index, itemView) =>
        {
            if (isDestory)
            {
                DestroyImmediate(itemView.gameObject);
            }
            else
            {
                itemView.gameObject.SetActive(false);
            }
        });
        if (isDestory)
        {
            listResearchItemView.Clear();
            listLineObj.Clear();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 动画-播放解锁粒子动画
    /// </summary>
    public void AnimForShowUnlockEffect(Vector2 position)
    {       
        ClearAnim();
        ui_EffectUnlock.transform.position = position;
        ui_EffectUnlock.gameObject.SetActive(true);
        ui_EffectUnlock.Clear(); 
        ui_EffectUnlock.Play();
        animForShowUnlockEffect =  DOVirtual.DelayedCall(1.5f,()=>
        { 
            ui_EffectUnlock.Stop(); 
            ui_EffectUnlock.gameObject.SetActive(false);
        });
    }
}