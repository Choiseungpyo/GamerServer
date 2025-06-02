using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum TeamType : int
{
    RED,
    BLUE
}

public enum RoomState : int
{
    WAITING,
    INGAME
}

public enum MatchType : int
{
    SOLO = 1,
    DUO = 2,
    SQUAD = 4
}



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
    [SerializeField] private TMP_Text inRoomUserStateTxt;
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

    private int hostId;

    #endregion
    #endregion


    void Awake()
    {
        hostId = -1;
        InitBtns();
        InitRoomInfo();
    }

    #region Functions
    public void SetInRoomUI(int currClientId, PACKET_S_C_INROOM_INFO_HEADER pack, List<PACKET_S_C_ROOM_USER_INFO> roomUsersInfo)
    {
        hostId = pack.HostId;

        // 방 정보
        SetRoomOption(pack);

        SetUserInfo(roomUsersInfo);

        SetInRoomUserStateTxt(currClientId, roomUsersInfo);

        SetBtns(hostId == currClientId);
    }

    private void SetBtns(bool isHost)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            btnDict[BtnType.OPTION].gameObject.SetActive(isHost);
        });
    }

    void SetInRoomUserStateTxt(int currClientId, List<PACKET_S_C_ROOM_USER_INFO> roomUsersInfo)
    {
        foreach(var roomUserInfo in roomUsersInfo)
        {
            if (currClientId != roomUserInfo.UserId)
                continue;

            TcpManager.Instance.RegisterJop(() =>
            {
                inRoomUserStateTxt.text = roomUserInfo.InRoomUserState.ToString();
            });
            
            return;
        }
    }

    /// <summary>
    /// 다른 유저들의 UI 정보 설정
    /// </summary>
    /// <param name="roomUsersInfo"></param>
    public void SetUserInfo(List<PACKET_S_C_ROOM_USER_INFO> roomUsersInfo)
    {
        foreach (var userInfo in roomUsersInfo)
        { 
            if (userInfo.teamType == TeamType.RED)
            {
                // 이미 추가해놓은 유저인 경우
                if (redUsersUIDict.ContainsKey(userInfo.userOrderOfTeam))
                {
                    redUsersUIDict[userInfo.userOrderOfTeam].SetInRoomUserStateTxt(userInfo.InRoomUserState);
                    continue;
                }
                   

                // 유저 정보 UI 설정
                var newInRoomUserUI = inRoomUserUIPool.Get(userInfo.UserId);
                newInRoomUserUI.SetInRoomUserUI(userInfo.UserName, userInfo.InRoomUserState);


                redUsersUIDict.Add(userInfo.userOrderOfTeam, newInRoomUserUI);
                TcpManager.Instance.RegisterJop(() =>
                {
                    newInRoomUserUI.transform.SetParent(redTeamTrDict[userInfo.userOrderOfTeam]);
                    newInRoomUserUI.GetRectTr().localPosition = Vector3.zero;
                });
               
            }
            else
            {
                // 이미 추가해놓은 유저인 경우
                if (blueUsersUIDict.ContainsKey(userInfo.userOrderOfTeam))
                {
                    blueUsersUIDict[userInfo.userOrderOfTeam].SetInRoomUserStateTxt(userInfo.InRoomUserState);
                    continue;
                }


                // 유저 정보 UI 설정
                var newInRoomUserUI = inRoomUserUIPool.Get(userInfo.UserId);
                newInRoomUserUI.SetInRoomUserUI(userInfo.UserName, userInfo.InRoomUserState);


                blueUsersUIDict.Add(userInfo.userOrderOfTeam, newInRoomUserUI);
                TcpManager.Instance.RegisterJop(() =>
                {
                    newInRoomUserUI.transform.SetParent(blueTeamTrDict[userInfo.userOrderOfTeam]);
                    newInRoomUserUI.GetRectTr().localPosition = Vector3.zero;
                });
               
            }
        }
    }

    public void SetRoomOption(PACKET_S_C_INROOM_INFO_HEADER pack)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            roomInfoDict[RoomInfoType.NO].text = pack.RoomNo.ToString();
            roomInfoDict[RoomInfoType.NAME].text = pack.RoomName.ToString();
            roomInfoDict[RoomInfoType.MATCHTYPE].text = pack.MatchType.ToString();
        });
      
    }


    #region Team

    #endregion

    #region Btns
    private void InitBtns()
    {
        foreach (var v in btnDatas)
            btnDict[v.type] = v.button;

        inRoomUserStateTxt.text = "";

        // 버튼 이벤트 연결 예시
        btnDict[BtnType.READY].onClick.AddListener(ChangeInRoomUserState);
        btnDict[BtnType.TEAM_CHANGE].onClick.AddListener(TeamChange);
        btnDict[BtnType.OPTION].onClick.AddListener(OnClickSetRoomOptions);
        btnDict[BtnType.EXIT].onClick.AddListener(Exit);
    }

    /// <summary>
    /// 클라 : 레디 상태 변경
    /// </summary>
    private void ChangeInRoomUserState()
    {
        switch (inRoomUserStateTxt.text)
        {
            case "READY":
            case "UNREADY":
                TcpManager.Instance.SendToServer(PTYPE.C_S_INROOM_USERSTATE);
                break;

            case "IDLE":
                TcpManager.Instance.SendToServer(PTYPE.C_S_GAMETSTART_BTN);
                break;

            default:
                Debug.LogWarning($"Invalid inRoomUserStateTxt.text : {inRoomUserStateTxt.text}");
                break;
        }
    }

    /// <summary>
    /// 서버 : 레디 상태 변경
    /// </summary>
    /// <param name="pack"></param>
    public void ChangeInRoomUserState(PACKET_S_C_CHANGE_INROOM_USERSTATE pack)
    {
        if (!redUsersUIDict.TryGetValue(pack.OrderOfTeam, out InRoomUserUI inRoomUserUI))
        {
            Debug.LogWarning(pack.OrderOfTeam);
            return;
        }

        inRoomUserUI.SetInRoomUserStateTxt(pack.InRoomUserState);

        TcpManager.Instance.RegisterJop(() =>
        {
            inRoomUserStateTxt.text = pack.InRoomUserState.ToString();
        });

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
            TcpManager.Instance.RegisterJop(() =>
            {
                inRoomUserUI.transform.SetParent(redTeamTrDict[pack.CurrOrderOfTeam]);
                inRoomUserUI.transform.localPosition = Vector3.zero;
            });
           
        }
        // 바꿀 팀이 블루인 경우
        else if (pack.CurrTeamType == TeamType.BLUE)
        {
            if (!redUsersUIDict.TryGetValue(pack.PrvOrderOfTeam, out InRoomUserUI inRoomUserUI))
            {
                Debug.LogWarning(pack.PrvOrderOfTeam);
                return;
            }

            redUsersUIDict.Remove(pack.PrvOrderOfTeam);
            blueUsersUIDict[pack.CurrOrderOfTeam] = inRoomUserUI;
            TcpManager.Instance.RegisterJop(() =>
            {
                inRoomUserUI.transform.SetParent(blueTeamTrDict[pack.CurrOrderOfTeam]);
                inRoomUserUI.transform.localPosition = Vector3.zero;
            });
          
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
        TcpManager.Instance.SendToServer(PTYPE.C_S_EXIT_ROOM);
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
