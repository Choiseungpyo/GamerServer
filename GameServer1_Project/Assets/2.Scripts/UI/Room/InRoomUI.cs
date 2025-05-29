using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InRoomUI : MonoBehaviour
{
    #region Variables
    #region Team

    [System.Serializable]
    public struct UserInfo
    {
        public int soltNum;
        public InRoomUserUI inRoomUserUI;
    }

    // <SlotNum, INRoomUserUI>
    Dictionary<int, InRoomUserUI> redUsersUIDict;
    Dictionary<int, InRoomUserUI> blueUsersUIDict;

    private InRoomUserUIPool inRoomUserUIPool;


    #endregion

    #region Btns
    public enum BtnType
    {
        READY,
        TEAM_CHANGE,
        OPTION,
        EXIT
    }

    [System.Serializable]
    public struct ButtonData
    {
        public BtnType type;
        public Button button;
    }

    [SerializeField] List<ButtonData> btnDatas;
    Dictionary<BtnType, Button> btnDict = new();

    // 준비완료 버튼 텍스트
    [SerializeField] private TMP_Text readyStateBtnTxt;
    #endregion

    #region RoomInfo
    public enum RoomInfoType
    {
        NO,
        NAME,
        MATCHTYPE
    }

    [System.Serializable]
    public struct RoomInfoData
    {
        public RoomInfoType type;
        public TMP_Text txt;
    }

    [SerializeField] List<RoomInfoData> roomInfoDatas;
    Dictionary<RoomInfoType, TMP_Text> roomInfoDict = new();

    #endregion
    #endregion


    void Awake()
    {
        InitBtns();
        InitRoomInfo();
    }

    #region Functions
    public void SetInRoomUI(Packet_S_C_ROOM_USERS_INFO_HEADER pack, List<PACKET_S_C_ROOM_USER_INFO> roomUsersInfo)
    {
        // 방 정보
        roomInfoDict[RoomInfoType.NO].text = pack.RoomNo.ToString();
        roomInfoDict[RoomInfoType.NAME].text = pack.RoomName.ToString();
        roomInfoDict[RoomInfoType.MATCHTYPE].text = pack.MatchType.ToString();

        SetUsersInfo(roomUsersInfo);

        SetReadyBtnText(pack.HostId);
    }

    public void SetUserReadyState(PACKET_S_C_READY_BTN pack)
    {
        if (redUsersUIDict.TryGetValue(pack.OrderOfTeam, out InRoomUserUI inRoomUserUI))
        {
            inRoomUserUI.SetUserState(pack.readyState);
        }
    }

    public void SetUsersInfo(List<PACKET_S_C_ROOM_USER_INFO> roomUsersInfo)
    {
        foreach (var userInfo in roomUsersInfo)
        { 
            // 유저 정보 UI 설정
            var newInRoomUserUI = inRoomUserUIPool.Get();
            newInRoomUserUI.SetInRoomUserUI(userInfo.UserName, userInfo.readyState);

            // Dict에 저장
            if (userInfo.teamType == TeamType.RED)
            {
                redUsersUIDict.Add(redUsersUIDict.Count, newInRoomUserUI);
            }
            else
            {
                blueUsersUIDict.Add(blueUsersUIDict.Count, newInRoomUserUI);
            }
        }
    }

    private void SetReadyBtnText(int hostId)
    {
        // 현재 클라이언트가 호스트인 경우
        if (hostId == TcpManager.Instance.Id)
            readyStateBtnTxt.text = "Ready";
        else
            readyStateBtnTxt.text = "Start";
    }


    #region Team

    #endregion

    #region Btns
    private void InitBtns()
    {
        foreach (var v in btnDatas)
            btnDict[v.type] = v.button;

        readyStateBtnTxt.text = "Ready";

        // 버튼 이벤트 연결 예시
        btnDict[BtnType.READY].onClick.AddListener(Ready);
        btnDict[BtnType.TEAM_CHANGE].onClick.AddListener(TeamChange);
        btnDict[BtnType.OPTION].onClick.AddListener(Option);
        btnDict[BtnType.EXIT].onClick.AddListener(Exit);
    }

    private void Ready()
    {
        // 현재 클라이언트가 호스트가 아닌 경우
        if(readyStateBtnTxt.text.Equals("Ready"))
            TcpManager.Instance.SendToServer(PTYPE.C_S_READY_BTN);
        else
            TcpManager.Instance.SendToServer(PTYPE.C_S_GAMETSTART_BTN);
    }

    private void TeamChange()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_TEAM_CHANGE);
    }

    private void Option()
    {
        //TcpManager.Instance.SendToServer();
    }

    private void Exit()
    {
        //TcpManager.Instance.SendToServer();
    }
    #endregion

    #region RoomInfo
    private void InitRoomInfo()
    {
        foreach (var v in roomInfoDatas)
            roomInfoDict[v.type] = v.txt;
    }


    #endregion
    #endregion
}
