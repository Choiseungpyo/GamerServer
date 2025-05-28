using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 로비에서의 방 정보 UI
/// </summary>
public class LobbyRoomItemUI : PoolableObject
{
    enum Type
    {
        NO = 0,
        ROOMNAME = 1,
        PEOPLENUM = 2,
        STATE = 3
    }

    TMP_Text[] roomDataTxts;

    private void Awake()
    {
        roomDataTxts = new TMP_Text[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            roomDataTxts[i] = transform.GetChild(i).GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 방 생성시 호출하는 함수
    /// </summary>
    /// <param name="packet"></param>
    public void InitRoomUIData(PACKET_S_C_CREATE_ROOM packet)
    {
        roomDataTxts[(int)Type.NO].text = packet.RoomNo.ToString();
        roomDataTxts[(int)Type.ROOMNAME].text = packet.RoomName.ToString();
        roomDataTxts[(int)Type.PEOPLENUM].text = "1/" + ((int)packet.MatchType * 2).ToString();
        roomDataTxts[(int)Type.STATE].text = RoomState.WATING.ToString();
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
        for (int i = 0; i < transform.childCount; i++)
            roomDataTxts[i].text = "";
    }

    protected override void OnSpawn()
    {
        InitRoomUIData();
    }

    protected override void OnDespawn()
    {

    }


}