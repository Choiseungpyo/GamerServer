using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : Singleton<LobbyManager>
{
    Dictionary<int, Room> rooms;

    RoomUI roomUI;

    private void Start()
    {
        rooms = new Dictionary<int, Room>();

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            RoomOption roomOption = new RoomOption("Come On Bro", MatchType.Solo, 0);
            CreateRoom(roomOption);
            Debug.Log(rooms.Count);
        }
    }

    // ���� �� ����
    public void EntryRandomRoom()
    {
        /*
         * PeopleNum++
         * �� UI Ȱ��ȭ
        */
        int randomRoomNo = Random.Range(0, rooms.Count);
        rooms[randomRoomNo].CurrPeopleNum++;

        RoomUIManager.Instance.SetVoidRoom(rooms[randomRoomNo]);
       
    }

    //public List<Room> GetJoinableRooms()
    //{
    //    return rooms.Values
    //        .Where(room => room.state == RoomState.Waiting && room.currentPlayers < room.maxPlayers)
    //        .ToList();
    //}

    /// <summary>
    /// �� ����
    /// </summary>
    /// <param name="roomOption"></param>
    public void CreateRoom(RoomOption roomOption)
    {
        int no = rooms.Count + 1;
        Player player = EntityManager.Instance.GetPlayer(roomOption.playerId);
        if (!player)
            Debug.LogWarning(roomOption.playerId);

        var newRoom = new Room(roomOption.matchType, no, roomOption.roomName, (int)roomOption.matchType * 2, player);
        rooms.Add(no, newRoom);

        LobbyUIManager.Instance.CreateRoom(newRoom);

        // ȣ���� ��ü�� ���� �÷��̾���
        if (EntityManager.Instance.IsCurrPlayer(roomOption.playerId))
            PanelManager.Instance.ActivateOnly(PanelType.ROOM);
    }

    private void DeleteRoom(int roomNo)
    {
        rooms.Remove(roomNo);
        // UI ��Ȱ��ȭ
    }

    private void MoveToRoom()
    {
        SceneManager.LoadScene("Game");

    }
}
