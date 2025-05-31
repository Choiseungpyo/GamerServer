using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 UI를 담당하는 클래스
/// </summary>
public class LobbyUIManager : Singleton<LobbyUIManager>
{
    #region Variables
    #region Room
    [SerializeField] Transform roomContents;
    [SerializeField] GameObject roomUI_Prefab;

    // <RoomNo, LobbyRoomItemUI>
    Dictionary<int, LobbyRoomItemUI> lobbyRoomItemUIDict = new();

    [SerializeField] Button EntryRandomBtn;
    [SerializeField] Button CreateRoomBtn;

    // Variables
    [SerializeField] Button exitBtn;
    #endregion

    #region Chat
    //UserNamePool userNamePool;
    [SerializeField] private TxtPool chatPool;
    [SerializeField] private Transform chatContent_Tr;
    #endregion

    #region UserNames
    [SerializeField] UserProfilePool userProfilePool;
    [SerializeField] Transform userProfileContent_Tr;

    #endregion

    #endregion


    protected override void Awake()
    {
        base.Awake();

        EntryRandomBtn.onClick.AddListener(EntryRandomRoom);
        CreateRoomBtn.onClick.AddListener(ShowCreateRoomUI);
        exitBtn.onClick.AddListener(Exit);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            //AddItem<Txt>(chatContent_Tr, chatPool.Get, (chat, msg) => chat.SetText(msg), "User1 : Hi");


        }
        if (Input.GetKeyDown(KeyCode.D))
            DeleteUserProtile(0);

    }

    #region Room
    public void CreateRoom(PACKET_S_C_CREATE_ROOM packet)
    {
        GameObject newRoomInfoUI_Obj = Instantiate(roomUI_Prefab, roomContents.transform);
        LobbyRoomItemUI newRoomUIFo = newRoomInfoUI_Obj.GetComponent<LobbyRoomItemUI>();
        if (!newRoomUIFo)
        {
            Debug.LogWarning(newRoomUIFo);
            return;
        }

        newRoomUIFo.InitRoomUIData(packet);
        lobbyRoomItemUIDict.Add(packet.RoomNo, newRoomUIFo);
    }

    public void UpdateRoomInfo(PACKET_S_C_UPDATE_LOBBY_ROOM_INFO packet)
    {
        // 기존에 있는 방 정보일 경우
        if (lobbyRoomItemUIDict.TryGetValue(packet.RoomNo, out LobbyRoomItemUI lobbyRoomItemUI))
        {
            lobbyRoomItemUI.ChangeRoomUIData(packet);
        }
        // 새로운 방 정보일 경우
        else
        {
            PACKET_S_C_CREATE_ROOM tmpPack = new PACKET_S_C_CREATE_ROOM();
            tmpPack.RoomNo = packet.RoomNo;
            tmpPack.RoomName = packet.RoomName;
            tmpPack.MatchType = (MatchType)packet.MaxNumOfPeople;
            CreateRoom(tmpPack);
        }
    }

    private void DeleteRoom(int roomNo)
    {
        lobbyRoomItemUIDict.Remove(roomNo);
    }

    private void EntryRandomRoom()
    {
        TcpManager.Instance.SendToServer(PTYPE.C_S_ENTRY_RANDOMROOM);
    }

    /// <summary>
    /// 방 생성 UI를 활성화하기
    /// </summary>
    private void ShowCreateRoomUI()
    {
        PanelManager.Instance.Activate(PanelType.LOBBY, PanelType.ROOMOPTION);
    }

    /// <summary>
    /// 타이틀로 이동버튼 클릭시
    /// </summary>
    private void Exit()
    {
        // 타이틀로 이동
        TcpManager.Instance.SendToServer(PTYPE.C_S_MOVE_TITLE);
    }
    #endregion


    public void SetRoomOption(PACKET_CHANGE_ROOM_OPTION pack)
    {
        if (!lobbyRoomItemUIDict.TryGetValue(pack.RoomNo, out LobbyRoomItemUI value))
        {
            Debug.LogWarning(pack.RoomNo);
            return;
        }

        value.ChangeRoomUIData(pack);
    }

    public void AddUserProfile(List<PACKET_S_C_LOBBY_USERS_INFO> userInfo)
    {
        foreach (var user in userInfo)
        {
           AddItem<UserProfile>(userProfileContent_Tr,
           () => userProfilePool.Get(user.UserId),
           (userProfile, userName) => userProfile.SetUserProfile(userName),
           user.UserName);
        }
    }

    // userId가 필요한 경우
    private void AddItem<T>(int userId, Transform parent, Func<int, T> getter, Action<T, string> setter, string data)
        where T : MonoBehaviour
    {
        T item = getter(userId);
        if (!item)
        {
            Debug.LogWarning(userId);
            return;
        }
        item.transform.SetParent(parent);
        setter(item, data);
    }

    // userId가 필요 없는 경우
    private void AddItem<T>(Transform parent, Func<T> getter, Action<T, string> setter, string data)
        where T : MonoBehaviour
    {
        T item = getter();
        item.transform.SetParent(parent);
        setter(item, data);
    }

    /// <summary>
    /// 유저가 로그아웃하면 호출
    /// </summary>
    /// <param name="userId"></param>
    private void DeleteUserProtile(int userId)
    {
        userProfilePool.Release(userId);
    }
}
