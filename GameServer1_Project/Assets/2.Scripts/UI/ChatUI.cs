using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private TxtPool chatPool;
    [SerializeField] private Transform chatContent_Tr;

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendBtn;

    protected virtual void Awake()
    {
        sendBtn.onClick.AddListener(Send);
    }

    public void AddMsg(string msg)
    {
        var chat = chatPool.Get();
        TcpManager.Instance.RegisterJop(() =>
        {
            chat.transform.SetParent(chatContent_Tr);
            chat.SetText(msg);
        });
    }

    private void Send()
    {
        string inputTxt = inputField.text;
        inputField.text = "";
        if (inputTxt.Equals(""))
            return;
        
        string msg = $"{TcpManager.Instance.UserName} : {inputTxt}";
        SendToSever(msg);
    }

    protected virtual void SendToSever(string msg)
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_CHAT_LOBBY, msg);
    }
}
