using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    private float yaw;
    private float pitch;

    private int tick;
    private float tickAccum;

    [Header("Net")]
    [SerializeField] private float sendHz = 30f;
    [SerializeField] private float mouseSensitivity = 0.15f;


    private void Awake()
    {
        TcpManagerMarshal.Instance.OnGameStart += EnableInputReader;
        TcpManagerMarshal.Instance.OnGameOver += DisableInputReader;
    }

    private void Start()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        tick = 0;
        tickAccum = 0f;

        yaw = 0f;
        pitch = 0f;
    }

    private void OnDestroy()
    {
        var tcpManager = TcpManagerMarshal.Instance;

        if (tcpManager == null) return;

        tcpManager.OnGameStart -= EnableInputReader;
        tcpManager.OnGameOver -= DisableInputReader;
    }

    private void EnableInputReader(GameStartPacket gameStartPacket)
    {
        enabled = true;
    }

    private void DisableInputReader(GameOverPacket gameOverPacket)
    {
        enabled = false;
    }

    private void Update()
    {
        if (GameSessionManager.Instance.GameFlowState == GameFlowState.MultiGame_Spectator)
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                GameSessionManager.Instance.SetSpectateTarget();
            return;
        }

        float dt = Time.deltaTime;

        ReadLook();
        ReadFire();

        tickAccum += dt;
        float interval = 1f / sendHz;

        while (tickAccum >= interval)
        {
            tickAccum -= interval;
            SendInputTick();
            tick++;
        }
    }

    private void ReadLook()
    {
        Vector2 md = Mouse.current.delta.ReadValue();
        yaw += md.x * mouseSensitivity;
        pitch -= md.y * mouseSensitivity;

        if (pitch > 89f) pitch = 89f;
        if (pitch < -89f) pitch = -89f;

        var player = GameSessionManager.Instance.GetLocalPlayer();
        player.SetLook(yaw, pitch);
    }

    private void SendInputTick()
    {
        Vector2 move = ReadMove();
        uint buttons = 0;

        int weaponId = 0;

        var player = GameSessionManager.Instance.GetLocalPlayer();
        weaponId = player.WeaponId;

        if (!player.CanMove)
        {
            move = Vector2.zero;
        }

        player.SetMoveInput(move.x, move.y);

        TcpManagerMarshal.Instance.SendInput(
            GameSessionManager.Instance.GameId,
            tick,
            move.x,
            move.y,
            yaw,
            pitch,
            buttons,
            weaponId
        );
    }


    private void ReadFire()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var player = GameSessionManager.Instance.GetLocalPlayer();

        int weaponId = player.WeaponId;

        player.PlayShoot();
        player.PlayMuzzleFlash();

        TcpManagerMarshal.Instance.SendFire(GameSessionManager.Instance.GameId, tick, weaponId);

        SoundManager.Instance.PlaySfx(SfxType.Player_Shoot);
    }

    private Vector2 ReadMove()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;

        Vector2 v = new Vector2(x, z);
        if (v.sqrMagnitude > 1f) v.Normalize();
        return v;
    }
}
