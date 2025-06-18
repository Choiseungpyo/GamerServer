using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    public enum BtnType
    {
        LOBBY,
        LOGOUT
    }

    [System.Serializable]
    public struct BtnData
    {
        public BtnType type;
        public Button btn;
    }

    [SerializeField] private List<BtnData> btnDatas;

    private Dictionary<BtnType, Button> btnDict = new();


    private void Awake()
    {
        foreach (var btn in btnDatas)
        {
            btnDict[btn.type] = btn.btn;
        }

        btnDict[BtnType.LOBBY].onClick.AddListener(MoveLobby);
        btnDict[BtnType.LOGOUT].onClick.AddListener(Logout);
    }

    private void MoveLobby()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_ENTRY_LOBBY);
    }

    private void Logout()
    {
        Application.Quit();
        //TcpManager.Instance.SendToServer(PTYPE.C_S_LOGOUT);
    }

}
