using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public enum Direction : int
{
    NONE = -1,
    UP,
    DOWN,
    LEFT,
    RIGHT
}

public class Player : MonoBehaviour
{
    int id;

    bool[] isMoveKeyPressed;
    Vector3 position;

    public int Id
    {
        get { return id; }
        set { id = value; }
    }


    void Start()
    {
        isMoveKeyPressed = new bool[4];
        for (int i = 0; i < isMoveKeyPressed.Length; i++)
            isMoveKeyPressed[i] = false;

        //StartCoroutine(SendData());
    }

    private void Update()
    {
        CheckInput();
    }

    void CheckInput()
    {
        // 이동
        SetInput(KeyCode.W, Direction.UP);
        SetInput(KeyCode.S, Direction.DOWN);
        SetInput(KeyCode.A, Direction.LEFT);
        SetInput(KeyCode.D, Direction.RIGHT);

        // 공격

        // 무기 전환


    }


    void SetInput(KeyCode keycode, Direction dir)
    {
        if (Input.GetKeyDown(keycode))
        {
            isMoveKeyPressed[(int)dir] = true;
        }

        if (Input.GetKeyUp(keycode))
            isMoveKeyPressed[(int)dir] = false;
    }

    IEnumerator SendData()
    {
        while (true)
        {
            for (int i = 0; i < isMoveKeyPressed.Length; i++)
            {
                if (!isMoveKeyPressed[i])
                    continue;

                Debug.Log("이벤트 등록 : PlayerMovement");
                EventManager.Instance.PostNotification(EVENT_TYPE.SEND_PLAYER_MOVEMENT, this, isMoveKeyPressed);
                break;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }
}
