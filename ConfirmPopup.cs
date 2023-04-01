using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ConfirmType
{
    OK,
    OK_CANCEL
}

public class ConfirmPopupData : PopupDataBase
{
    public ConfirmType ConfirmType;
    public string TitleTxt;
    public string DescTxt;
    public string MainBtnTxt;
    public Action OnClickMainBtn;
    public string SubBtnTxt;
    public Action OnClickSubBtn;
}

public class ConfirmPopup : PopupBase
{
    public TextMeshProUGUI TitleTxt = null;
    public TextMeshProUGUI DescTxt = null;
    public Button MainBtn = null;
    public Button SubBtn = null;
    public TextMeshProUGUI MainBtnTxt = null;
    public TextMeshProUGUI SubBtnTxt = null;

    private ConfirmPopupData m_ConfirmPopupData = null;
    private Action m_OnClickMainBtn = null;
    private Action m_OnClickSubBtn = null;

    public override void SetInfo(PopupDataBase popupData, Action onShow, Action onClose)
    {
        base.SetInfo(popupData, onShow, onClose);

        m_ConfirmPopupData = popupData as ConfirmPopupData;

        TitleTxt.text = m_ConfirmPopupData.TitleTxt;
        DescTxt.text = m_ConfirmPopupData.DescTxt;
        MainBtnTxt.text = m_ConfirmPopupData.MainBtnTxt;
        m_OnClickMainBtn = m_ConfirmPopupData.OnClickMainBtn;
        SubBtnTxt.text = m_ConfirmPopupData.SubBtnTxt;
        m_OnClickSubBtn = m_ConfirmPopupData.OnClickSubBtn;

        MainBtn.gameObject.SetActive(m_ConfirmPopupData.ConfirmType == ConfirmType.OK_CANCEL);
        SubBtn.gameObject.SetActive(true);
    }

    public void OnClickMainBtn()
    {
        m_OnClickMainBtn?.Invoke();
        ClosePopup();
    }

    public void OnClickSubBtn()
    {
        m_OnClickSubBtn?.Invoke();
        ClosePopup();
    }
}
