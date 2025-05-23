using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class LobbyManager : Singleton<LobbyManager>
{
    #region Variables
    Dictionary<int, Room> rooms;
    [SerializeField] int maxRoomNum;
    #endregion

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
            

        }
    }

    // ���� �� ����
    public void EntryRandomRoom()
    {
        List<Room> joinableRooms =  GetJoinableRooms();
        
        // ������ �� ���� ���
        if(joinableRooms.Count == 0)
        {
            // ��� ���� �� á���ϴ�! ��� ���� ����
            return;
        }

        // ������ �� �ִ� ���
        int randomRoomNo = Random.Range(0, joinableRooms.Count);

        rooms[randomRoomNo].CurrPeopleNum++;

        RoomUIManager.Instance.SetRoom(rooms[randomRoomNo]);
        // RoomUI ����
    }

    public List<Room> GetJoinableRooms()
    {
        return rooms.Values
            .Where(room => room.GetRoomState() == RoomState.WATING && room.CurrPeopleNum < room.MaxPeopleNum)
            .ToList();
    }

    /// <summary>
    /// �� ����
    /// </summary>
    /// <param name="roomOption"></param>
    public void CreateRoom(RoomOption roomOption)
    {
        if (rooms.Count >= maxRoomNum)
        {
            // �ִ� �� ������ �����ؼ� ������ٰ� UI ����
            return;
        }


        int no = rooms.Count + 1;
        Player player = EntityManager.Instance.GetPlayer(roomOption.playerId);
        if (!player)
            Debug.LogWarning(roomOption.playerId);

        var newRoom = new Room(roomOption.matchType, no, roomOption.roomName, (int)roomOption.matchType * 2, player);
        rooms.Add(no, newRoom);

        LobbyUIManager.Instance.CreateRoom(newRoom);

        // ȣ���� ��ü�� ���� �÷��̾���
        //if (EntityManager.Instance.IsCurrPlayer(roomOption.playerId))
        //    PanelManager.Instance.ActivateOnly(PanelType.ROOM);
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