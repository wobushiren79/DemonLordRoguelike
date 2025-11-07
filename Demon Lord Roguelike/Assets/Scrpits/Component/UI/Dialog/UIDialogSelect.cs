

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIDialogSelect : DialogView
{
    protected DialogSelectBean dialogSelectData;
    public List<Button> listButtonSelect = new List<Button>();

    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        dialogSelectData = (DialogSelectBean)dialogData;
        //先隐藏已有的按钮
        for (int i = 0; i < listButtonSelect.Count; i++)
        {
            var itemView = listButtonSelect[i];
            itemView.onClick.RemoveAllListeners();
            itemView.gameObject.SetActive(false);
        }

        for (int i = 0; i < dialogSelectData.listSelectContent.Count; i++)
        {
            var selectContent = dialogSelectData.listSelectContent[i];
            var selectAction = dialogSelectData.listActions[i];
            Button itemView = null;
            if (i >= listButtonSelect.Count)
            {
                GameObject newObj = Instantiate(ui_DialogContent.gameObject, ui_SelectBtn.gameObject);
                itemView = newObj.GetComponent<Button>();
                listButtonSelect.Add(itemView);
            }
            else
            {
                itemView = listButtonSelect[i];
            }
            itemView.gameObject.SetActive(true);
            itemView.onClick.RemoveAllListeners();
            itemView.onClick.AddListener(() =>
            {
                actionCancel?.Invoke(this, dialogData);
                DestroyDialog();
                selectAction?.Invoke();
            });
            var tvContent = itemView.GetComponentInChildren<TextMeshProUGUI>();
            tvContent.text = selectContent;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_SelectBtn.transform as RectTransform);

    }

    public override void SetContent(string content)
    {
        base.SetContent(content);
        if(content.IsNull())
        {
            ui_ContentShow.gameObject.SetActive(false);
        }
        else
        {
            ui_ContentShow.gameObject.SetActive(true);
        }
    }
}