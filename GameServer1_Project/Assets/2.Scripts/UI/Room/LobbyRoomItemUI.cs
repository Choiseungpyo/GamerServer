using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LobbyRoomItemUI;

/// <summary>
/// 로비에서의 방 정보 UI
/// </summary>
public class LobbyRoomItemUI : PoolableObject
{
    public enum RoomInfoType
    {
        NO = 0,
        ROOMNAME = 1,
        PEOPLENUM = 2,
        STATE = 3
    }

    [System.Serializable]
    public struct RoomTxtData
    {
        public RoomInfoType type;
        public TMP_Text txt;
    }

    [SerializeField] List<RoomTxtData> roomTxtDatas;
    Dictionary<RoomInfoType, TMP_Text> roomTxtDict = new();

    [SerializeField ]private Button btn;

    private void Awake()
    {
        btn.onClick.AddListener(EntryRoom);
        InitRoomTxtDict();
    }

    void InitRoomTxtDict()
    {
        if (roomTxtDict.Count > 0)
            return;

        foreach (var v in roomTxtDatas)
            roomTxtDict[v.type] = v.txt;
    }

    /// <summary>
    /// 방 정보를 변경시 호출하는 함수
    /// </summary>
    /// <param name="packet"></param>
    public void ChangeRoomOptionUI(PACKET_CHANGE_ROOM_OPTION pack)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            roomTxtDict[RoomInfoType.NO].text = pack.RoomNo.ToString();
            roomTxtDict[RoomInfoType.ROOMNAME].text = pack.RoomName.ToString();
        });
    }

    public void ChangeRoomUIData(PACKET_S_C_LOBBY_ROOM_INFO pack)
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            roomTxtDict[RoomInfoType.NO].text = pack.RoomNo.ToString();
            roomTxtDict[RoomInfoType.ROOMNAME].text = pack.RoomName.ToString();
            roomTxtDict[RoomInfoType.PEOPLENUM].text = pack.CurrNumOfPeople + "/" + pack.MaxNumOfPeople;
            roomTxtDict[RoomInfoType.STATE].text = pack.RoomState.ToString();
        });
    }

    private void InitRoomUIData()
    {
        foreach (var v in roomTxtDatas)
            v.txt.text = "";
    }

    protected override void OnSpawn()
    {

    }

    protected override void OnDespawn()
    {
        TcpManager.Instance.RegisterJop(() =>
        {
            InitRoomUIData();
        });
    }

    private void EntryRoom()
    {
        string[] peopleNum = roomTxtDict[RoomInfoType.PEOPLENUM].text.Split('/');

        // 인게임중일 경우
        if ((RoomState)Enum.Parse(typeof(RoomState), roomTxtDict[RoomInfoType.STATE].text) == RoomState.INGAME)
            return;

        // 방이 전부 찼을 경우
        if (int.Parse(peopleNum[0]) >= int.Parse(peopleNum[1]))
            return;

        int roomNo = int.Parse(roomTxtDict[RoomInfoType.NO].text);
        TcpManager.Instance.SendToServer(PTYPE.C_S_ENTRY_ROOM, roomNo);
    }
}