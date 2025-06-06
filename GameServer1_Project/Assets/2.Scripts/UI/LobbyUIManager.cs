using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 UI를 담당하는 클래스
/// </summary>
public class LobbyUIManager : ChatUI
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


    #region Room
    public void CreateRoom(PACKET_S_C_LOBBY_ROOM_INFO packet)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            GameObject newRoomInfoUI_Obj = Instantiate(roomUI_Prefab, roomContents.transform);
            LobbyRoomItemUI newRoomUIFo = newRoomInfoUI_Obj.GetComponent<LobbyRoomItemUI>();
            if (!newRoomUIFo)
            {
                Debug.LogWarning(newRoomUIFo);
                return;
            }

            newRoomUIFo.ChangeRoomUIData(packet);
            lobbyRoomItemUIDict.Add(packet.RoomNo, newRoomUIFo);
        });
    }

    public void UpdateRoomInfo(PACKET_S_C_LOBBY_ROOM_INFO packet)
    {

        // 기존에 있는 방 정보일 경우
        if (lobbyRoomItemUIDict.TryGetValue(packet.RoomNo, out LobbyRoomItemUI lobbyRoomItemUI))
        {
            lobbyRoomItemUI.ChangeRoomUIData(packet);
        }
        // 새로운 방 정보일 경우
        else
        {
            CreateRoom(packet);
        }
    }

    public void UpdateAllRoomInfo(List<PACKET_S_C_LOBBY_ROOM_INFO> packets)
    {
        foreach(var pack in packets)
        {
            UpdateRoomInfo(pack);
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
        TcpManager.Instance.SendToServer(PTYPE.C_S_EXIT_LOBBY);
    }
    #endregion


    public void SetRoomOption(PACKET_CHANGE_ROOM_OPTION pack)
    {
        if (!lobbyRoomItemUIDict.TryGetValue(pack.RoomNo, out LobbyRoomItemUI value))
        {
            Debug.LogWarning(pack.RoomNo);
            return;
        }

        value.ChangeRoomOptionUI(pack);
    }

    public void AddUserProfile(List<PACKET_S_C_LOBBY_USERS_INFO> userInfo)
    {
        foreach (var user in userInfo)
        {
            AddItem<UserProfile>(
                      user.UserId,                       // userId
                      userProfileContent_Tr,             // parent
                      (id) => userProfilePool.Get(id),  // getter: id로 프로필 얻기
                      (profile, userName) =>
                      {
                          if (profile != null)
                              profile.SetUserProfile(userName);
                      },
                      user.UserName                      // data
                  );
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
        TcpManager.Instance.RegisterJop(() =>
        {
            item.transform.SetParent(parent);
        });
       
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
