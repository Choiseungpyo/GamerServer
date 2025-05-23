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

    // 랜덤 방 입장
    public void EntryRandomRoom()
    {
        List<Room> joinableRooms =  GetJoinableRooms();
        
        // 입장할 수 없는 경우
        if(joinableRooms.Count == 0)
        {
            // 모든 방이 꽉 찼습니다! 라고 문구 띄우기
            return;
        }

        // 입장할 수 있는 경우
        int randomRoomNo = Random.Range(0, joinableRooms.Count);

        rooms[randomRoomNo].CurrPeopleNum++;

        RoomUIManager.Instance.SetRoom(rooms[randomRoomNo]);
        // RoomUI 띄우기
    }

    public List<Room> GetJoinableRooms()
    {
        return rooms.Values
            .Where(room => room.GetRoomState() == RoomState.WATING && room.CurrPeopleNum < room.MaxPeopleNum)
            .ToList();
    }

    /// <summary>
    /// 방 생성
    /// </summary>
    /// <param name="roomOption"></param>
    public void CreateRoom(RoomOption roomOption)
    {
        if (rooms.Count >= maxRoomNum)
        {
            // 최대 방 개수를 도달해서 못만든다고 UI 띄우기
            return;
        }


        int no = rooms.Count + 1;
        Player player = EntityManager.Instance.GetPlayer(roomOption.playerId);
        if (!player)
            Debug.LogWarning(roomOption.playerId);

        var newRoom = new Room(roomOption.matchType, no, roomOption.roomName, (int)roomOption.matchType * 2, player);
        rooms.Add(no, newRoom);

        LobbyUIManager.Instance.CreateRoom(newRoom);

        // 호출한 객체가 현재 플레이어라면
        //if (EntityManager.Instance.IsCurrPlayer(roomOption.playerId))
        //    PanelManager.Instance.ActivateOnly(PanelType.ROOM);
    }

    private void DeleteRoom(int roomNo)
    {
        rooms.Remove(roomNo);
        // UI 재활성화
    }

    private void MoveToRoom()
    {
        SceneManager.LoadScene("Game");

    }
}