using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InRoomUserState : int
{
    UNREADY,
    READY,
    IDLE,
    START
}

public class InRoomUserUI : PoolableObject
{
    [SerializeField] private Image iconImg;
    [SerializeField] private TMP_Text userNameTxt;
    [SerializeField] private TMP_Text inRoomUserStateTxt;
    private RectTransform rectTr;

    private void Awake()
    {
        rectTr = GetComponent<RectTransform>();
    }

    public void SetInRoomUserUI(string userName, InRoomUserState InRoomUserState)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            iconImg.sprite = UserIcon.Instance.GetSprite(userName);
            userNameTxt.text = userName;
        });

        SetInRoomUserStateTxt(InRoomUserState);
    }

    public void SetInRoomUserStateTxt(InRoomUserState inRoomUserState)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            inRoomUserStateTxt.text = inRoomUserState.ToString();
        });
    }
    
    protected override void OnSpawn()
    {
        TcpManager.Instance.RegisterJop(()=>
        {
            iconImg.sprite = null;
            userNameTxt.text = "";
            inRoomUserStateTxt.text = "";
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
