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
    private string userName;
    private TeamType teamType;

    private bool[] isMoveKeyPressed = new bool[4];
    private Vector3 position;
    private Vector3 rotation;

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
    private Rigidbody rb;


    private void Awake()
    {
        isMoveKeyPressed = new bool[4];
        for (int i = 0; i < isMoveKeyPressed.Length; i++)
            isMoveKeyPressed[i] = false;

        //StartCoroutine(SendData());
        rb = GetComponent<Rigidbody>();
    }


    private void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0, v).normalized;
        rb.MovePosition(rb.position + move * 2 * Time.fixedDeltaTime);
    }

    void CheckInput()
    {
        // 이동
        SetMoveInput(KeyCode.W, Direction.UP);
        SetMoveInput(KeyCode.S, Direction.DOWN);
        SetMoveInput(KeyCode.A, Direction.LEFT);
        SetMoveInput(KeyCode.D, Direction.RIGHT);

        // 총
        Shoot();
        Reload();

    }



    // Functions
    private void Move()
    {

    }


    private void Shoot()
    {
        // 총 이펙트 효과 
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

                Debug.Log("이벤트 등록 : PlayerMovement");
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
        userName = data.Name;
        
        teamType = data.TeamType;
        currHp = data.CurrHp;
        state = data.State;

        Vector3 tmpVector = Vector3.zero;
        Vector3 dir = Vector3.zero; // 오른쪽으로 1만큼 이동

        TcpManager.Instance.RegisterJop(() =>
        {
            SetUnityVector3(ref tmpVector, data.Position);
            dir = tmpVector - position;
            position = tmpVector;

            
            rb.MovePosition(rb.position + dir);
            transform.position = rb.position;

            SetUnityVector3(ref tmpVector, data.Rotation);
            rotation = tmpVector;
            rb.MoveRotation(Quaternion.Euler(rotation));
            transform.rotation = rb.rotation;
        }); 
    }

    private void SetUnityVector3(ref Vector3 tmpVector , System.Numerics.Vector3 v)
    {
        tmpVector.x = v.X;
        tmpVector.y = v.Y;
        tmpVector.z = v.Z;
    }
    

    protected override void OnSpawn()
    {

    }

    protected override void OnDespawn()
    {

    }

    

}
