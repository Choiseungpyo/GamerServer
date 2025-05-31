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
    Dictionary<int, InRoomUserUI> redUsersUIDict = new();
    Dictionary<int, InRoomUserUI> blueUsersUIDict = new();

    [SerializeField] private InRoomUserUIPool inRoomUserUIPool;

    [System.Serializable]
    public struct OrderTransform
    {
        public int order;
        public Transform transform;
    }
    [SerializeField] private List<OrderTransform> redTeamTrDatas;
    [SerializeField] private List<OrderTransform> blueTeamTrDatas;
    Dictionary<int, Transform> redTeamTrDict = new();
    Dictionary<int, Transform> blueTeamTrDict = new();

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
        SetRoomOption(pack);

        SetUserInfo(roomUsersInfo);

        SetReadyBtnText(pack.HostId);
    }

    /// <summary>
    /// 유저가 방 입장시 다른 유저들의 UI 정보 설정
    /// </summary>
    /// <param name="roomUsersInfo"></param>
    public void SetUserInfo(List<PACKET_S_C_ROOM_USER_INFO> roomUsersInfo)
    {
        foreach (var userInfo in roomUsersInfo)
        { 
            // 유저 정보 UI 설정
            var newInRoomUserUI = inRoomUserUIPool.Get();
            newInRoomUserUI.SetInRoomUserUI(userInfo.UserName, userInfo.readyState);

            // Dict 저장
            if (userInfo.teamType == TeamType.RED)
            {
                redUsersUIDict.Add(userInfo.userOrderOfTeam, newInRoomUserUI);
                TcpManager.Instance.RegisterJop(() =>
                {
                    newInRoomUserUI.transform.SetParent(redTeamTrDict[userInfo.userOrderOfTeam]);
                    newInRoomUserUI.GetRectTr().localPosition = Vector3.zero;
                });
               
            }
            else
            {
                blueUsersUIDict.Add(userInfo.userOrderOfTeam, newInRoomUserUI);
                TcpManager.Instance.RegisterJop(() =>
                {
                    newInRoomUserUI.transform.SetParent(blueTeamTrDict[userInfo.userOrderOfTeam]);
                    newInRoomUserUI.GetRectTr().localPosition = Vector3.zero;
                });
               
            }
        }
    }

    public void SetRoomOption(Packet_S_C_ROOM_USERS_INFO_HEADER pack)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            roomInfoDict[RoomInfoType.NO].text = pack.RoomNo.ToString();
            roomInfoDict[RoomInfoType.NAME].text = pack.RoomName.ToString();
            roomInfoDict[RoomInfoType.MATCHTYPE].text = pack.MatchType.ToString();
        });
      
    }

    private void SetReadyBtnText(int hostId)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            // 현재 클라이언트가 호스트인 경우
            if (hostId == TcpManager.Instance.Id)
                readyStateBtnTxt.text = "Ready";
            else
                readyStateBtnTxt.text = "Start";
        });
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
        btnDict[BtnType.OPTION].onClick.AddListener(OnClickSetRoomOptions);
        btnDict[BtnType.EXIT].onClick.AddListener(Exit);
    }

    /// <summary>
    /// 클라 : 레디 상태 변경
    /// </summary>
    private void Ready()
    {
        // 현재 클라이언트가 호스트가 아닌 경우
        if(readyStateBtnTxt.text.Equals("Ready"))
            TcpManager.Instance.SendToServer(PTYPE.C_S_READY_BTN);
        else
            TcpManager.Instance.SendToServer(PTYPE.C_S_GAMETSTART_BTN);
    }

    /// <summary>
    /// 서버 : 레디 상태 변경
    /// </summary>
    /// <param name="pack"></param>
    public void Ready(PACKET_S_C_READY_BTN pack)
    {
        if (!redUsersUIDict.TryGetValue(pack.OrderOfTeam, out InRoomUserUI inRoomUserUI))
        {
            Debug.LogWarning(pack.OrderOfTeam);
            return;
        }

        inRoomUserUI.SetUserReadyState(pack.readyState);
    }

    /// <summary>
    /// 클라 : 유저의 팀 변경 
    /// </summary>
    private void TeamChange()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_TEAM_CHANGE);
    }

    /// <summary>
    /// 서버 : 유저의 팀 변경
    /// </summary>
    /// <param name="pack"></param>
    public void TeamChange(PACKET_S_C_TEAM_CHANGE pack)
    {
        // 바꿀 팀이 레드인 경우
        if (pack.CurrTeamType == TeamType.RED)
        {
            if (!blueUsersUIDict.TryGetValue(pack.PrvOrderOfTeam, out InRoomUserUI inRoomUserUI))
            {
                Debug.LogWarning(pack.PrvOrderOfTeam);
                return;
            }

            blueUsersUIDict.Remove(pack.PrvOrderOfTeam);
            redUsersUIDict[pack.CurrOrderOfTeam] = inRoomUserUI;
            inRoomUserUI.transform.SetParent(redTeamTrDict[pack.CurrOrderOfTeam]);
        }
        // 바꿀 팀이 블루인 경우
        else if (pack.CurrTeamType == TeamType.RED)
        {
            if (!redUsersUIDict.TryGetValue(pack.PrvOrderOfTeam, out InRoomUserUI inRoomUserUI))
            {
                Debug.LogWarning(pack.PrvOrderOfTeam);
                return;
            }

            redUsersUIDict.Remove(pack.PrvOrderOfTeam);
            blueUsersUIDict[pack.CurrOrderOfTeam] = inRoomUserUI;
            inRoomUserUI.transform.SetParent(blueTeamTrDict[pack.CurrOrderOfTeam]);
        }
    }

    /// <summary>
    /// 방 옵션 변경 UI 활성화 버튼을 눌렀을 경우
    /// </summary>
    private void OnClickSetRoomOptions()
    {
        PanelManager.Instance.Activate(PanelType.ROOM, PanelType.ROOMOPTION);
    }

    /// <summary>
    /// 클라 : 방 옵션 변경 완료 버튼을 눌렀을 경우
    /// </summary>
    private void RoomOption()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_CHANGE_ROOM_OPTION);
    }

    /// <summary>
    /// 서버 : 방 옵션 변경
    /// </summary>
    /// <param name="pack"></param>
    public void RoomOption(PACKET_CHANGE_ROOM_OPTION pack)
    {
        roomInfoDict[RoomInfoType.NO].text = pack.RoomNo.ToString();
        roomInfoDict[RoomInfoType.NAME].text = pack.RoomName;
        roomInfoDict[RoomInfoType.MATCHTYPE].text = pack.MatchType.ToString();
    }

    /// <summary>
    /// 클라 : 로비로 나가기
    /// </summary>
    private void Exit()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_MOVE_LOBBY);
    }

    #endregion

    #region RoomInfo
    private void InitRoomInfo()
    {
        foreach (var v in roomInfoDatas)
            roomInfoDict[v.type] = v.txt;

        foreach (var redTeamTr in redTeamTrDatas)
            redTeamTrDict[redTeamTr.order] = redTeamTr.transform;

        foreach (var blueTeamTr in blueTeamTrDatas)
            blueTeamTrDict[blueTeamTr.order] = blueTeamTr.transform;

    }


    #endregion
    #endregion
}
