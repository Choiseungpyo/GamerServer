using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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



    private void Awake()
    {
        foreach (var v in roomTxtDatas)
            roomTxtDict[v.type] = v.txt;
    }

    /// <summary>
    /// 방 생성시 호출하는 함수
    /// </summary>
    /// <param name="packet"></param>
    public void InitRoomUIData(PACKET_S_C_CREATE_ROOM packet)
    {
        roomTxtDict[RoomInfoType.NO].text = packet.RoomNo.ToString();
        roomTxtDict[RoomInfoType.ROOMNAME].text = packet.RoomName.ToString();
        roomTxtDict[RoomInfoType.PEOPLENUM].text = "1/" + ((int)packet.MatchType * 2).ToString();
        roomTxtDict[RoomInfoType.STATE].text = RoomState.WATING.ToString();
    }

    /// <summary>
    /// 방 정보를 변경시 호출하는 함수
    /// </summary>
    /// <param name="packet"></param>
    public void SetRoomUIData()
    {
        //roomDataTxts[(int)Type.NO].text = packet.RoomNo.ToString();
        //roomDataTxts[(int)Type.ROOMNAME].text = packet.RoomName.ToString();
        //roomDataTxts[(int)Type.PEOPLENUM].text = packet.CurrPeopleNum.ToString() + "/" + room.MaxPeopleNum.ToString();
        //roomDataTxts[(int)Type.STATE].text = room.GetRoomState().ToString();
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
        InitRoomUIData();
    }


}