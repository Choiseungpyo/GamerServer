using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 积己等 规 郴何 UI甫 包府窍绰 努贰胶
/// </summary>
public class RoomUIManager : Singleton<RoomUIManager>
{
    Room room;


    public void SetRoom(Room room)
    {
        this.room = room;
    }

}
