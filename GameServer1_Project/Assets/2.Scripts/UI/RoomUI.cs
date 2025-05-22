using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomUI : MonoBehaviour
{
    enum Type
    {
        NO = 0,
        ROOMNAME =1,
        PEOPLENUM=2,
        STATE=3
    }

    [SerializeField] TMP_Text[] roomDataTxts;
    
    public void SetRoomUIData(Room room)
    {
        roomDataTxts[(int)Type.NO].text = room.No.ToString();
        roomDataTxts[(int)Type.ROOMNAME].text = room.RoomName.ToString();
        roomDataTxts[(int)Type.PEOPLENUM].text = room.CurrPeopleNum.ToString() + "/" + room.MaxPeopleNum.ToString();
        roomDataTxts[(int)Type.STATE].text = room.GetRoomState().ToString();
    }
}
