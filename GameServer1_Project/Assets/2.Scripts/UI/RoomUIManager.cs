using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ������ �� ���� UI�� �����ϴ� Ŭ����
/// </summary>
public class RoomUIManager : Singleton<RoomUIManager>
{
    Room room;


    public void SetVoidRoom(Room room)
    {
        this.room = room;
    }

}
