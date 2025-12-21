using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSessionManager : Singleton<GameSessionManager>
{
    [Header("Scene Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerPoolComponent playerPool;

    [Header("Visual DB (local assets)")]
    [SerializeField] private CharacterVisualDatabaseSO visualDb;

    [Header("Runtime DB (from server)")]
    [SerializeField] private CharacterDatabaseSO characterStatDb;
    [SerializeField] private WeaponRuntimeDatabaseSO weaponStatDb;

    [Header("Net")]
    [SerializeField] private float sendHz = 30f;
    [SerializeField] private float mouseSensitivity = 0.15f;

    [Header("UI")]
    [SerializeField] private LocalHpBarUI localHpBar;
    [SerializeField] private DamageOverlayUI damageOverlay;

    [Header("Spectate")]
    [SerializeField] private float spectateDistance = 3.0f;
    [SerializeField] private float spectateHeight = 1.6f;
    [SerializeField] private float spectateLerp = 12.0f;

    [SerializeField] private VictoryPodiumManager victoryPodium;

    private bool isSpectating;
    private ulong spectateTargetSid;

    private readonly Dictionary<ulong, int> sidToCharacterId = new Dictionary<ulong, int>(8);
    private readonly Dictionary<ulong, Player> players = new Dictionary<ulong, Player>(8);

    private GameStartPacket pendingStart;
    private bool hasPendingStart;
    private int selectedCharacterId = -1;

    private int gameId;
    private int selfIndex;
    private int playerCount;
    private ulong mySessionId;

    private int tick;
    private float tickAccum;
    private int shotSeq;

    private float yaw;
    private float pitch;

    private bool inGame;

    protected override void Awake()
    {
        base.Awake();

        inGame = false;
        hasPendingStart = false;
        selectedCharacterId = -1;

        isSpectating = false;
        spectateTargetSid = 0;

        if (visualDb != null)
            visualDb.Build();
    }

    private void OnEnable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        tcp.OnGameStart += HandleGameStart;
        tcp.OnGameState += HandleGameState;
        tcp.OnShotResult += HandleShotResult;
        tcp.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        tcp.OnGameStart -= HandleGameStart;
        tcp.OnGameState -= HandleGameState;
        tcp.OnShotResult -= HandleShotResult;
        tcp.OnGameOver -= HandleGameOver;
    }

    private void HandleGameStart(GameStartPacket pkt)
    {
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.MultiGame_Playing });

        if (selectedCharacterId >= 0)
            EnterGame(pkt);
        else
            SetPendingStart(pkt);
    }

    public void SetPendingStart(GameStartPacket pkt)
    {
        pendingStart = pkt;
        hasPendingStart = true;
    }

    public void SetSelectedCharacter(int characterId)
    {
        selectedCharacterId = characterId;
    }

    public void StartPendingGame()
    {
        if (!hasPendingStart) return;

        hasPendingStart = false;
        EnterGame(pendingStart);
    }

    public void EnterGame(GameStartPacket pkt)
    {
        if (visualDb != null)
            visualDb.Build();

        ClearPlayers();

        isSpectating = false;
        spectateTargetSid = 0;

        if (localHpBar != null)
            localHpBar.gameObject.SetActive(true);

        if (damageOverlay != null)
            damageOverlay.gameObject.SetActive(true);

        gameId = pkt.gameId;
        selfIndex = pkt.selfIndex;
        playerCount = pkt.playerCount;

        mySessionId = pkt.sessionIds[selfIndex];

        tick = 0;
        tickAccum = 0f;
        shotSeq = 0;

        yaw = 0f;
        pitch = 0f;

        sidToCharacterId.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            ulong sid = pkt.sessionIds[i];
            Vector3 pos = new Vector3(pkt.spawnX[i], pkt.spawnY[i], pkt.spawnZ[i]);

            Player p = playerPool.Get();
            p.OnDespawnRequested = OnPlayerDespawnRequested;

            bool isMe = (sid == mySessionId);
            p.Spawn(sid, isMe, pos);

            int characterId = 0;
            if (pkt.characterIds != null && pkt.characterIds.Length >= playerCount)
                characterId = pkt.characterIds[i];

            if (characterId < 0)
                characterId = 0;

            sidToCharacterId[sid] = characterId;

            CharacterVisualRow entry = null;
            bool hasEntry = (visualDb != null) && visualDb.TryGet(characterId, out entry);
            if (hasEntry && entry != null && entry.modelPrefab != null)
            {
                p.SetCharacterModel(characterId, entry.modelPrefab);

                int wid = 0;
                if (pkt.weaponIds != null && pkt.weaponIds.Length >= playerCount)
                    wid = pkt.weaponIds[i];

                p.SetDefaultWeapon(wid);
            }
            else
            {
                Debug.LogError("[EnterGame] CharacterVisual missing. characterId=" + characterId);
            }

            if (isMe)
            {
                BindCameraToLocalPlayer(p);

                if (localHpBar != null)
                    localHpBar.Bind(p);

                if (damageOverlay != null)
                    damageOverlay.Bind(p);

                p.OnHpChanged += (hp, maxHp) =>
                {
                    if (hp <= 0)
                    {
                        if (!isSpectating)
                            EnterSpectate();
                    }
                };
            }

            players[sid] = p;
        }

        inGame = true;
    }

    public void LeaveGame()
    {
        inGame = false;

        isSpectating = false;
        spectateTargetSid = 0;

        if (mainCamera != null)
            mainCamera.transform.SetParent(null, true);

        ClearPlayers();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClearPlayers()
    {
        foreach (var kv in players)
        {
            if (kv.Value != null)
                playerPool.Release(kv.Value);
        }
        players.Clear();
    }

    private void BindCameraToLocalPlayer(Player me)
    {
        if (mainCamera == null) return;
        if (me == null) return;

        me.BindMainCamera(mainCamera);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnPlayerDespawnRequested(Player p)
    {
        if (p == null) return;

        ulong sid = p.SessionId;
        players.Remove(sid);
        playerPool.Release(p);
    }

    private void Update()
    {
        if (!inGame) return;

        float dt = Time.deltaTime;

        ReadLook();

        if (isSpectating)
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                CycleSpectateTarget();

            UpdateSpectateCamera(dt);
            return;
        }

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

        if (!isSpectating)
        {
            Player me;
            if (players.TryGetValue(mySessionId, out me))
                me.SetLook(yaw, pitch);
        }
    }

    private void ReadFire()
    {
        if (isSpectating) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (!players.TryGetValue(mySessionId, out var me) || me == null)
            return;

        int weaponId = me.WeaponId;
        int shotId = ++shotSeq;

        me.PlayShoot();
        me.PlayMuzzleFlash();

        TcpManagerMarshal.Instance.SendFire(gameId, shotId, tick, weaponId);

        SoundManager.Instance.PlaySfx(SfxType.Player_Shoot);
    }

    private void SendInputTick()
    {
        if (isSpectating) return;

        Vector2 move = ReadMove();
        uint buttons = 0;

        int weaponId = 0;

        Player me;
        if (players.TryGetValue(mySessionId, out me) && me != null)
        {
            weaponId = me.WeaponId;

            if (!me.CanMove)
            {
                move = Vector2.zero;
            }

            me.SetMoveInput(move.x, move.y);
        }

        TcpManagerMarshal.Instance.SendInput(
            gameId,
            tick,
            move.x,
            move.y,
            yaw,
            pitch,
            buttons,
            weaponId
        );
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

    private void HandleGameState(ServerGameStatePacket pkt)
    {
        if (!inGame) return;
        if (pkt.gameId != gameId) return;

        for (int i = 0; i < pkt.playerCount; i++)
        {
            PlayerState3D ps = pkt.players[i];

            Player p;
            if (!players.TryGetValue(ps.sessionId, out p))
                continue;

            Vector3 pos = new Vector3(ps.x, ps.y, ps.z);
            p.ApplyServerState(pos, ps.yaw, ps.pitch, ps.hp, ps.weaponId);

            if (ps.sessionId == mySessionId && ps.hp <= 0)
                OnLocalDiedEnterSpectate();
        }
    }

    private void HandleShotResult(ServerShotResultPacket pkt)
    {
        if (!inGame) return;
        if (pkt.gameId != gameId) return;

        // 1) 누가 쐈는지 찾고, 다른 사람에게만 발사 연출 재생
        if (pkt.shooterSessionId != 0 && pkt.shooterSessionId != mySessionId)
        {
            Player shooter;
            if (players.TryGetValue(pkt.shooterSessionId, out shooter))
            {
                shooter.PlayShoot();
                shooter.PlayMuzzleFlash();
            }
        }

        // 2) 피격 처리 + 죽음 처리
        if (pkt.hit == 1)
        {
            Player victim;
            if (players.TryGetValue(pkt.victimSessionId, out victim))
            {
                victim.SetHp(pkt.victimHp);

                if (pkt.victimSessionId == mySessionId && pkt.victimHp <= 0)
                    OnLocalDiedEnterSpectate();
            }
        }
    }

    private void HandleGameOver(GameOverPacket pkt)
    {
        if (pkt.gameId != gameId) return;

        int count = pkt.rankCount;
        if (count < 0) count = 0;
        if (count > NetConst.MAX_PLAYERS) count = NetConst.MAX_PLAYERS;

        PodiumRankData r1 = new PodiumRankData();
        PodiumRankData r2 = new PodiumRankData();
        PodiumRankData r3 = new PodiumRankData();

        if (count > 0)
        {
            r1.characterId = pkt.rankCharacterIds[0];
            r1.iconId = pkt.rankIconIds[0];
            r1.nickname = PacketUtil.ReadRankNickname(pkt, 0);
        }

        if (count > 1)
        {
            r2.characterId = pkt.rankCharacterIds[1];
            r2.iconId = pkt.rankIconIds[1];
            r2.nickname = PacketUtil.ReadRankNickname(pkt, 1);
        }

        if (count > 2)
        {
            r3.characterId = pkt.rankCharacterIds[2];
            r3.iconId = pkt.rankIconIds[2];
            r3.nickname = PacketUtil.ReadRankNickname(pkt, 2);
        }

        LeaveGame();
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.GameResult });

        if (victoryPodium != null)
            victoryPodium.Show(r1, r2, r3);
    }

    private void OnLocalDiedEnterSpectate()
    {
        if (isSpectating) return;

        if (localHpBar != null)
            localHpBar.gameObject.SetActive(false);

        if (damageOverlay != null)
            damageOverlay.gameObject.SetActive(false);

        EnterSpectate();
    }

    private void EnterSpectate()
    {
        isSpectating = true;

        if (mainCamera != null)
            mainCamera.transform.SetParent(null, true);

        PickNextSpectateTarget();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.MultiGame_Spectator });
    }

    private bool IsAlive(Player p)
    {
        if (p == null) return false;
        if (p.IsDead) return false;
        if (p.Hp <= 0) return false;
        return true;
    }

    private void PickNextSpectateTarget()
    {
        ulong picked = 0;

        foreach (var kv in players)
        {
            ulong sid = kv.Key;
            Player p = kv.Value;

            if (sid == mySessionId) continue;
            if (!IsAlive(p)) continue;

            picked = sid;
            break;
        }

        spectateTargetSid = picked;
    }

    private void CycleSpectateTarget()
    {
        if (players.Count == 0)
        {
            spectateTargetSid = 0;
            return;
        }

        List<ulong> alive = new List<ulong>(players.Count);

        foreach (var kv in players)
        {
            ulong sid = kv.Key;
            Player p = kv.Value;

            if (sid == mySessionId) continue;
            if (!IsAlive(p)) continue;

            alive.Add(sid);
        }

        if (alive.Count == 0)
        {
            spectateTargetSid = 0;
            return;
        }

        int idx = alive.IndexOf(spectateTargetSid);
        idx = (idx + 1) % alive.Count;
        spectateTargetSid = alive[idx];
    }

    private void UpdateSpectateCamera(float dt)
    {
        if (mainCamera == null) return;

        Player target;
        if (spectateTargetSid == 0 || !players.TryGetValue(spectateTargetSid, out target) || !IsAlive(target))
        {
            PickNextSpectateTarget();
            if (spectateTargetSid == 0) return;
            if (!players.TryGetValue(spectateTargetSid, out target)) return;
        }

        Transform t = target.transform;

        Vector3 focus = t.position + Vector3.up * spectateHeight;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, -spectateDistance);

        Vector3 desiredPos = focus + offset;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            desiredPos,
            1f - Mathf.Exp(-spectateLerp * dt)
        );

        mainCamera.transform.rotation = Quaternion.LookRotation(
            (focus - mainCamera.transform.position).normalized,
            Vector3.up
        );
    }
}