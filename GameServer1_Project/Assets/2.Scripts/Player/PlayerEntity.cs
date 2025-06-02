using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;

public enum Direction : int
{
    NONE = -1,
    UP,
    DOWN,
    LEFT,
    RIGHT
}

public enum PlayerState : int
{
    IDLE,
    MOVE,
    SHOOT,
    RELOAD,
    DEAD
}

public struct PlayerEntityData
{

}

public class PlayerEntity : PoolableObject
{
    private int id;
    private string Name;
    private TeamType teamType;

    private bool[] isMoveKeyPressed = new bool[4];
    private Vector3 position;

    // Variables
    [SerializeField] GameObject hitEffect_Prefab;
    private PlayerState state;

    private const int maxHp = 100;
    private int currHp;

    // Property
    public int Id
    {
        get { return id; }
        private set { id = value; }
    }

    public TeamType TeamType
    {
        get { return teamType; }
        private set { }
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
        // ÀÌµ¿
        SetMoveInput(KeyCode.W, Direction.UP);
        SetMoveInput(KeyCode.S, Direction.DOWN);
        SetMoveInput(KeyCode.A, Direction.LEFT);
        SetMoveInput(KeyCode.D, Direction.RIGHT);

        // ÃÑ
        Shoot();
        Reload();

    }



    // Functions
    private void Move()
    {

    }


    private void Shoot()
    {
        // ÃÑ ÀÌÆåÆ® È¿°ú 
    }

    private void Reload()
    {

    }

    private void Ani()
    {

    }

    private void OnTriggerEnter(Collider other)
    {

    }


    private void SetMoveInput(KeyCode keycode, Direction dir)
    {
        if (Input.GetKeyDown(keycode))
        {
            isMoveKeyPressed[(int)dir] = true;
        }

        if (Input.GetKeyUp(keycode))
            isMoveKeyPressed[(int)dir] = false;
    }

    private IEnumerator SendData()
    {
        while (true)
        {
            for (int i = 0; i < isMoveKeyPressed.Length; i++)
            {
                if (!isMoveKeyPressed[i])
                    continue;

                Debug.Log("ÀÌº¥Æ® µî·Ï : PlayerMovement");
                TcpManager.Instance.SendToServer(PTYPE.C_S_MOVE_PLAYER, isMoveKeyPressed);
                break;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void UpdataeData(PACKET_S_C_PLAYERENTITY_DATA data)
    {
        id = data.Id;

        //for(int i=0; i< isMoveKeyPressed.Length; i++)
        //    isMoveKeyPressed = data.isMoveKeyPressed;

        // position = data.Position;
        // TcpManager.Instance.RegisterJop(() =>
        //{
        //    transform.rotation = Quaternion.Euler(data.rotation);
        //});

        state = data.State;
        currHp = data.CurrHp;
    }

    protected override void OnSpawn()
    {

    }

    protected override void OnDespawn()
    {

    }


}
