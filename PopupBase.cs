using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PopupType
{
    None = 0,
    SettingsPopup,
    ShopPopup,
    ContinuePopup,
    PausePopup,
    QuitPopup,
    HintConfirmPopup,
    ResultPopup,
    TutorialPopup,
    ConfirmPopup,
    LanguagePopup,
    StageSelectPopup,
    ChapterClearPopup,
    BoosterConfirmPopup,
    RoulettePopup,
    CharacterPopup,
    PackPurchasePopup,
}

public class PopupDataBase
{
    public PopupType popupType = PopupType.None;
    public PopupMgr.openMode openMode = PopupMgr.openMode.Overlay;
    public PopupMgr.gnbMode gnbMode = PopupMgr.gnbMode.All;
}

public class PopupBase : MonoBehaviour
{
    public PopupType popupType { get; private set; }
    public PopupMgr.openMode openMode { get; private set; }
    public PopupMgr.gnbMode gnbMode { get; private set; }
    protected bool isInputBlocked = false;
    private Action m_OnShow;
    private Action m_OnClose;
    public bool isHidden { get; set; } = false;

    public virtual void Init(Transform anchor)
    {
        Logger.Log(popupType + " Init");

        popupType = PopupType.None;
        isInputBlocked = false;
        m_OnShow = null;
        m_OnClose = null;

        transform.SetParent(anchor);

        var rectTransform = GetComponent<RectTransform>();
        if(!rectTransform)
        {
            Logger.LogError("Popup does not have rectransform.");
            return;
        }

        rectTransform.localPosition = new Vector3(0f, 0f, 0f);
        rectTransform.localScale = new Vector3(1f, 1f, 1f);
        rectTransform.offsetMin = new Vector2(0, 0);
        rectTransform.offsetMax = new Vector2(0, 0);
    }

    public virtual void SetInfo(PopupDataBase popupData, Action onShow, Action onClose)
    {
        Logger.Log(popupType + " SetInfo");

        popupType = popupData.popupType;
        openMode = popupData.openMode;
        gnbMode = popupData.gnbMode;
        m_OnShow = onShow;
        m_OnClose = onClose;
    }

    public virtual void ShowPopup()
    {
        m_OnShow?.Invoke();
        m_OnShow = null;
    }

    public virtual void ClosePopup(bool isCloseAll = false)
    {
        if (!isCloseAll)
        {
            m_OnClose?.Invoke();
        }
        m_OnClose = null;

        PopupMgr.Instance.ClosePopup(this);
    }

    public virtual void EnableInput()
    {
        isInputBlocked = false;
    }

    public virtual void OnClickCloseButton()
    {
        SoundMgr.Instance.PlaySFX(SFX.ui_menu_button_click_19);

        ClosePopup();
    }
}