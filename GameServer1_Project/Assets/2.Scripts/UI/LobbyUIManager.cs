using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 UI를 담당하는 클래스
/// </summary>
public class LobbyUIManager : Singleton<LobbyUIManager>
{
    [SerializeField] Transform roomContents;
    [SerializeField] GameObject roomUI_Prefab;

    Dictionary<int, RoomUI> roomUIs;

    [SerializeField] Button EntryRandomBtn;
    [SerializeField] Button CreateRoomBtn;

    private void Start()
    {
        EntryRandomBtn.onClick.AddListener(EntryRandomRoom);
        CreateRoomBtn.onClick.AddListener(ShowCreateRoomUI);

        roomUIs = new Dictionary<int, RoomUI>();
    }

    public void CreateRoom(Room roomData)
    {
        GameObject newRoomUI_Obj = Instantiate(roomUI_Prefab, roomContents.transform);
        RoomUI newRoomUI = newRoomUI_Obj.GetComponent<RoomUI>();
        if (!newRoomUI)
        {
            Debug.LogWarning(newRoomUI);
            return;
        }

        newRoomUI.SetRoomUIData(roomData);
        roomUIs.Add(roomUIs.Count + 1, newRoomUI);
    }

    private void DeleteRoom(int roomNo)
    {
        roomUIs.Remove(roomNo);
    }

    private void EntryRandomRoom()
    {
        LobbyManager.Instance.EntryRandomRoom();
    }

    private void ShowCreateRoomUI()
    {
        PanelManager.Instance.SetPanelState(PanelType.ROOMOPTION, true);
    }


    // Variables
    [SerializeField] Button exitBtn;


    #region Functions
    private void Exit()
    {

    }
    #endregion


    #region Chat
    void AddMsg()
    {
        // 오브젝트 풀링으로 최대 20개 저장
    }
    #endregion

    #region Users
    void AddUser()
    {

    }

    void DeleteUser()
    {

    }
    #endregion
}
