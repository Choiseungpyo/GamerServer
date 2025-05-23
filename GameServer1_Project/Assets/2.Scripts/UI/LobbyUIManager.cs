using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// �κ� UI�� ����ϴ� Ŭ����
/// </summary>
public class LobbyUIManager : Singleton<LobbyUIManager>
{
    #region Variables
    #region Room
    [SerializeField] Transform roomContents;
    [SerializeField] GameObject roomUI_Prefab;

    Dictionary<int, RoomInfoUI> roomUIs;

    [SerializeField] Button EntryRandomBtn;
    [SerializeField] Button CreateRoomBtn;

    // Variables
    [SerializeField] Button exitBtn;
    #endregion

    #region Chat
    //UserNamePool userNamePool;
    [SerializeField] TxtPool chatPool;
    [SerializeField] Transform chatContent_Tr;
    #endregion

    #region UserNames
    [SerializeField] UserProfilePool userProfilePool;
    [SerializeField] Transform userProfileContent_Tr;
    #endregion

    #endregion


    private void Start()
    {
        EntryRandomBtn.onClick.AddListener(EntryRandomRoom);
        CreateRoomBtn.onClick.AddListener(ShowCreateRoomUI);

        roomUIs = new Dictionary<int, RoomInfoUI>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            //AddItem<Txt>(chatContent_Tr, chatPool.Get, (chat, msg) => chat.SetText(msg), "User1 : Hi");

            AddItem<UserProfile>(0, userProfileContent_Tr, userProfilePool.Get, (profile, name) => profile.SetUserProfile(name), "Cat");
        }
        if (Input.GetKeyDown(KeyCode.D))
            DeleteUserProtile(0);

    }

    #region Room
    public void CreateRoom(Room roomData)
    {
        GameObject newRoomInfoUI_Obj = Instantiate(roomUI_Prefab, roomContents.transform);
        RoomInfoUI newRoomUIFo = newRoomInfoUI_Obj.GetComponent<RoomInfoUI>();
        if (!newRoomUIFo)
        {
            Debug.LogWarning(newRoomUIFo);
            return;
        }

        newRoomUIFo.SetRoomUIData(roomData);
        roomUIs.Add(roomUIs.Count + 1, newRoomUIFo);
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

    private void Exit()
    {
        // Ÿ��Ʋ�� �̵�
        SceneManager.LoadScene("Title");
    }
    #endregion

    // userId�� �ʿ��� ���
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

    // userId�� �ʿ� ���� ���
    private void AddItem<T>(Transform parent, Func<T> getter, Action<T, string> setter, string data)
        where T : MonoBehaviour
    {
        T item = getter();
        item.transform.SetParent(parent);
        setter(item, data);
    }

    private void DeleteUserProtile(int userId)
    {
        userProfilePool.Release(userId);
    }
}
