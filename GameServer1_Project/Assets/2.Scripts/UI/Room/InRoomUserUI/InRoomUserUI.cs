using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ReadyState : int
{
    WAITING,
    READY
}

public class InRoomUserUI : PoolableObject
{
    [SerializeField] private Image iconImg;
    [SerializeField] private TMP_Text userNameTxt;
    [SerializeField] private TMP_Text readyStateTxt;
    private RectTransform rectTr;

    private void Awake()
    {
        rectTr = GetComponent<RectTransform>();
    }

    public void SetInRoomUserUI(string userName, ReadyState readyState)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            iconImg.sprite = UserIcon.Instance.GetSprite(userName);
            userNameTxt.text = userName;
        });
        
        SetUserReadyState(readyState);
    }

    public void SetUserReadyState(ReadyState readyState)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            readyStateTxt.text = readyState.ToString();
        });
    }
    
    protected override void OnSpawn()
    {
        TcpManager.Instance.RegisterJop(()=>
        {
            iconImg.sprite = null;
            userNameTxt.text = "";
            readyStateTxt.text = "";
        });
  
    }
    protected override void OnDespawn()
    {

    }

    public RectTransform GetRectTr()
    {
        return rectTr;
    }

}
