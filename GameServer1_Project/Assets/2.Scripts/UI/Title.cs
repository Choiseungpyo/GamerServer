using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    public enum BtnType : int
    {
        LOBBY = 0,
        LOGOUT
    }

    [SerializeField] Button[] btns;

    // Start is called before the first frame update
    void Start()
    {
        btns[(int)BtnType.LOBBY].onClick.AddListener(MoveLobby);
        btns[(int)BtnType.LOGOUT].onClick.AddListener(Logout);
    }

    private void MoveLobby()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_ENTRY_LOBBY);
    }

    private void Logout()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_LOGOUT);
    }

}
