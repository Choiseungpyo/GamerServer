using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ReadyState
{
    WAITING,
    READY
}

public class InRoomUserUI : PoolableObject
{
    [SerializeField] private Image iconImg;
    [SerializeField] private TMP_Text userNameTxt;
    [SerializeField] private TMP_Text readyStateTxt;

    public void SetInRoomUserUI(string userName, ReadyState readyState)
    {
        iconImg.sprite = UserIcon.Instance.GetSprite(userName);
        userNameTxt.text = userName;
        SetUserState(readyState);
    }

    public void SetUserState(ReadyState readyState)
    {
        readyStateTxt.text = readyState.ToString();
    }

    protected override void OnSpawn()
    {
        iconImg.sprite = null;
        userNameTxt.text = "";
        readyStateTxt.text = "";
    }
    protected override void OnDespawn()
    {

    }

}
