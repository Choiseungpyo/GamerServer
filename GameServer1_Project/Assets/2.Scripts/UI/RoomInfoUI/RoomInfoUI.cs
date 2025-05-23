using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomInfoUI : PoolableObject
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

    public void SetRoomUIData(Room room)
    {
        roomDataTxts[(int)Type.NO].text = room.No.ToString();
        roomDataTxts[(int)Type.ROOMNAME].text = room.RoomName.ToString();
        roomDataTxts[(int)Type.PEOPLENUM].text = room.CurrPeopleNum.ToString() + "/" + room.MaxPeopleNum.ToString();
        roomDataTxts[(int)Type.STATE].text = room.GetRoomState().ToString();
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