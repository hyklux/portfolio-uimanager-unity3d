﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupMgr : SingletonBehaviour<PopupMgr>
{
    [SerializeField]
    private Image m_FadeImg = null;

    [SerializeField]
    private GameObject m_Indicator = null;

    [SerializeField]
    private TextMeshProUGUI m_IndicatorMsg = null;

    [SerializeField]
    private Animation[] m_IndicatorAnimArry = null;

    [SerializeField]
    private Transform m_PopupCanvasTrs = null;
    public Transform PopupCanvasTrs { get { return m_PopupCanvasTrs; } }
    public GameObject Stats = null;
    public GoldMgr GoldMgr = null;
    public GemMgr GemMgr = null;

    public enum openMode
    {
        Overlay,  //다른 팝업과 함께 최상위에 표시
        Single,   //활성화된 모든 팝업을 hide하고, 혼자만 노출. 팝업이 닫히면 hide되었던 팝업들이 다시 활성화
        Override, //기존 팝업을 모두 닫고 단독으로 표시
        Reserve,  //대기 목록(reservedList)에 추가, 다른 팝업이 모두 닫힌 후 표시함.
        Mono,     //기존 팝업이 떠 있으면 이후 초기화 메세지를 무시함. 서버에서 메세지를 받아서 팝업을 띄울때는 오류를 줄이기 위해 이걸 사용할 것.  
    }

    public enum gnbMode
    {
        All,
        HideResources,
    }

    private Transform m_Container = null;
    private PopupBase m_FrontPopup = null;
    private Dictionary<PopupType, GameObject> m_OpenPopupPool = new Dictionary<PopupType, GameObject>();
    private Dictionary<PopupType, GameObject> m_PopupPool = new Dictionary<PopupType, GameObject>();
    private Stack<List<GameObject>> m_HiddenPopupListStack = new Stack<List<GameObject>>();
    private int m_OverlayPopupCount = 0;

    protected override void Init()
    {
        base.Init();

        m_Container = transform.Find("Container");
        SetStatsActive(false);
        m_FadeImg.transform.localScale = Vector3.zero;
        m_Indicator.transform.localScale = Vector3.zero;
        m_IndicatorMsg.text = string.Empty;
    }

    public bool IsIndicatorOn()
    {
        return m_Indicator.gameObject.activeSelf;
    }

    public bool ExistsOpenPopup()
    {
        return m_FrontPopup != null;
    }

    public bool IsPopupOpen(PopupType popupType)
    {
        return m_OpenPopupPool.ContainsKey(popupType);
    }

    public void OpenPopup(PopupDataBase popupData, Action onShow = null, Action onClose = null)
    {
        Logger.Log(string.Format("Open {0}.", popupData.popupType.ToString()));

        if (m_Indicator.transform.position.x == 1)
        {
            Logger.LogWarning(string.Format("Indicator is on. Popup open canceled.", popupData.popupType.ToString()));
            return;
        }

        bool isAlreadyOpen = false;
        var popup = GetPopup(popupData.popupType, out isAlreadyOpen);
        if (!popup)
        {
            Logger.LogError(string.Format("{0} does not exist.", popupData.popupType.ToString()));
            return;
        }

        var siblingIdx = m_PopupCanvasTrs.childCount - 3 - m_OverlayPopupCount;
        popup.Init(m_PopupCanvasTrs);
        if(!isAlreadyOpen)
        {
            popup.transform.SetSiblingIndex(siblingIdx);
        }
        popup.gameObject.SetActive(true);
        popup.SetInfo(popupData, onShow, onClose);

        switch (popupData.openMode)
        {
            case openMode.Overlay:
                m_OverlayPopupCount++;
                popup.ShowPopup();
                break;
            case openMode.Reserve:
            case openMode.Mono:
                popup.ShowPopup();
                break;
            case openMode.Override:
                CloseAllOpenPopups();
                popup.ShowPopup();
                break;
            case openMode.Single:
                HideOpenPopups();
                popup.ShowPopup();
                break;
            default:
                break;
        }

        m_FrontPopup = popup;
        m_OpenPopupPool[popupData.popupType] = popup.gameObject;
    }

    private PopupBase GetPopup(PopupType popupType, out bool isAlreadyOpen)
    {
        PopupBase popup = null;
        isAlreadyOpen = false;

        if (m_OpenPopupPool.ContainsKey(popupType))
        {
            popup = m_OpenPopupPool[popupType].GetComponent<PopupBase>();
            isAlreadyOpen = true;
        }
        else if (m_PopupPool.ContainsKey(popupType))
        {
            popup = m_PopupPool[popupType].GetComponent<PopupBase>();
            m_PopupPool.Remove(popupType);
        }
        else
        {
            var popupObj = Instantiate(Resources.Load("Prefabs/Popups/" + popupType.ToString(), typeof(GameObject))) as GameObject;
            popup = popupObj.GetComponent<PopupBase>();
        }

        return popup;
    }

    public PopupBase GetActivePopup(PopupType popupType)
    {
        if (m_OpenPopupPool.ContainsKey(popupType))
        {
            return m_OpenPopupPool[popupType].GetComponent<PopupBase>();
        }
        else
        {
            return null;
        }
    }

    public void ClosePopup(PopupBase popup)
    {
        Logger.Log("ClosePopup popup::" + popup.popupType.ToString());

        if (popup.openMode == openMode.Single)
        {
            ShowHiddenPopups();
        }

        if (popup.openMode == openMode.Overlay)
        {
            m_OverlayPopupCount--;
        }

        popup.gameObject.SetActive(false);
        m_OpenPopupPool.Remove(popup.popupType);
        m_PopupPool[popup.popupType] = popup.gameObject;
        popup.transform.SetParent(m_Container);

        m_FrontPopup = null;
        foreach (var item in m_OpenPopupPool)
        {
            var openPopup = item.Value.GetComponent<PopupBase>();
            m_FrontPopup = openPopup;
        }
    }

    private void HideOpenPopups()
    {
        var list = new List<GameObject>();

        foreach (var item in m_OpenPopupPool)
        {
            var popupObj = item.Value;
            if (popupObj != null && !popupObj.GetComponent<PopupBase>().isHidden)
            {
                popupObj.transform.localScale = Vector3.zero;
                popupObj.GetComponent<PopupBase>().isHidden = true;
                list.Add(popupObj);
            }
        }

        m_HiddenPopupListStack.Push(list);
    }

    private void ShowHiddenPopups()
    {
        var list = m_HiddenPopupListStack.Pop();
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].transform.localScale = Vector3.one;
                list[i].GetComponent<PopupBase>().isHidden = false;
            }
        }
        list = null;
    }

    public PopupBase GetCurrentFrontPopup()
    {
        return m_FrontPopup;
    }

    public void CloseCurrFrontPopup()
    {
        m_FrontPopup.ClosePopup();
    }

    public void CloseAllOpenPopups()
    {
        while (m_FrontPopup)
        {
            m_FrontPopup.ClosePopup(true);
        }
    }

    public void ShowNetworkErrorPopup()
    {
        var popupData = new ConfirmPopupData();
        popupData.popupType = PopupType.ConfirmPopup;
        popupData.ConfirmType = ConfirmType.OK;
        popupData.TitleTxt = Localization.Get("network_error");
        popupData.TitleTxt = Localization.Get("network_error_desc");
        popupData.SubBtnTxt = Localization.Get("ok");
        popupData.OnClickSubBtn = () =>
        {
            Application.Quit();
        };
    }

    public void ShowGooglePlayLoginFailPopup()
    {
        var popupData = new ConfirmPopupData();
        popupData.popupType = PopupType.ConfirmPopup;
        popupData.ConfirmType = ConfirmType.OK;
        popupData.TitleTxt = Localization.Get("network_error");
        popupData.TitleTxt = Localization.Get("network_error_desc");
        popupData.SubBtnTxt = Localization.Get("ok");
    }

    public void ShowAppStoreLoginFailPopup()
    {
        var popupData = new ConfirmPopupData();
        popupData.popupType = PopupType.ConfirmPopup;
        popupData.ConfirmType = ConfirmType.OK;
        popupData.TitleTxt = Localization.Get("network_error");
        popupData.TitleTxt = Localization.Get("network_error_desc");
        popupData.SubBtnTxt = Localization.Get("ok");
    }

    public void Fade(Color color, float startAlpha, float endAlpha, float duration, float startDelay, bool deactiveOnFinish, Action onFinish = null)
    {
        StartCoroutine(DoFade(color, startAlpha, endAlpha, duration, startDelay, deactiveOnFinish, onFinish));
    }

    private IEnumerator DoFade(Color color, float startAlpha, float endAlpha, float duration, float startDelay, bool deactiveOnFinish, Action onFinish)
    {
        yield return new WaitForSeconds(startDelay);

        m_FadeImg.transform.localScale = Vector3.one;
        m_FadeImg.color = new Color(color.r, color.g, color.b, startAlpha);

        var startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < duration)
        {
            m_FadeImg.color = new Color(color.r, color.g, color.b, Mathf.Lerp(startAlpha, endAlpha, (Time.realtimeSinceStartup - startTime) / duration));
            yield return null;
        }

        m_FadeImg.color = new Color(color.r, color.g, color.b, endAlpha);

        if (deactiveOnFinish)
        {
            m_FadeImg.transform.localScale = Vector3.zero;
        }

        onFinish?.Invoke();
    }

    public void CancelFade()
    {
        m_FadeImg.transform.localScale = Vector3.zero;
    }

    public void EnableIndicator(bool value, string msg = "")
    {
        m_IndicatorMsg.text = msg;
        m_Indicator.transform.localScale = value ? Vector3.one : Vector3.zero;

        if (value)
        {
            for (int i = 0; i < m_IndicatorAnimArry.Length; i++)
            {
                m_IndicatorAnimArry[i].Stop();
                m_IndicatorAnimArry[i].Play();
            }
        }
    }

    public void OnClickCancelIndicator()
    {
        EnableIndicator(false);
    }

    public void SetStatsActive(bool value, bool enableInput = true)
    {
        Stats.SetActive(value);
        GoldMgr.SetInputActive(enableInput);
        GemMgr.SetInputActive(enableInput);
    }
}