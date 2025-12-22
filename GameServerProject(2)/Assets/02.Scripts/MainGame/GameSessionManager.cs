using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameFlowState
{
    Login,
    Lobby,
    Lobby_Matching,
    MultiGame_CharacterSelection,
    MultiGame_Playing,
    MultiGame_Spectator,
    ZombieGame_Playing,
    GameResult
}

public class GameSessionManager : Singleton<GameSessionManager>, IEventListener<GameFlowStateEvent>
{
    [Header("Scene Refs")]
    [SerializeField] private PlayerPool playerPool;

    [Header("UI")]
    [SerializeField] private LocalHpBarUI localHpBar;
    [SerializeField] private DamageOverlayUI damageOverlay;

    [SerializeField] private VictoryPodiumManager victoryPodium;

    private ulong spectateTargetSid;

    private readonly Dictionary<ulong, int> sidToCharacterId = new Dictionary<ulong, int>(NetConst.MAX_PLAYERS);
    private readonly Dictionary<ulong, Player> players = new Dictionary<ulong, Player>(NetConst.MAX_PLAYERS);

    private int selectedCharacterId = -1;

    private int selfIndex;
    private int playerCount;

    public int GameId { get; private set; }
    public ulong MySessionId { get; private set; }
    public GameFlowState GameFlowState { get; private set; }

    private TcpManagerMarshal tcp;
    private bool tcpBound;

    protected override void Awake()
    {
        base.Awake();

        EventDispatcher.RegisterListener(this);
        selectedCharacterId = -1;
        spectateTargetSid = 0;
    }

    private void OnEnable()
    {
        TryBindTcp();
    }

    private void OnDisable()
    {
        UnbindTcp();
    }

    private void Start()
    {
        GameFlowState = GameFlowState.Login;
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = this.GameFlowState });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        UnbindTcp();
        EventDispatcher.UnregisterListener(this);
    }

    private void TryBindTcp()
    {
        if (tcpBound) return;

        tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        tcpBound = true;

        tcp.OnGameStart += HandleGameStart;
        tcp.OnGameState += HandleGameState;
        tcp.OnShotResult += HandleShotResult;
        tcp.OnGameOver += HandleGameOver;
    }

    private void UnbindTcp()
    {
        if (!tcpBound) return;

        if (tcp == null)
            tcp = TcpManagerMarshal.Instance;

        if (tcp != null)
        {
            tcp.OnGameStart -= HandleGameStart;
            tcp.OnGameState -= HandleGameState;
            tcp.OnShotResult -= HandleShotResult;
            tcp.OnGameOver -= HandleGameOver;
        }

        tcpBound = false;
        tcp = null;
    }

    private void UnbindLocalUi()
    {
        if (localHpBar != null) localHpBar.Bind(null);
        if (damageOverlay != null) damageOverlay.Bind(null);
    }

    private void HandleGameStart(GameStartPacket pkt)
    {
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.MultiGame_Playing });

        if (selectedCharacterId >= 0)
            EnterGame(pkt);
    }

    public void SetSelectedCharacter(int characterId)
    {
        selectedCharacterId = characterId;
    }

    public void EnterGame(GameStartPacket pkt)
    {
        ClearPlayers();

        spectateTargetSid = 0;

        GameId = pkt.gameId;
        selfIndex = pkt.selfIndex;
        playerCount = pkt.playerCount;
        MySessionId = pkt.sessionIds[selfIndex];

        sidToCharacterId.Clear();

        var dm = DataManager.Instance;
        if (dm == null) return;

        var characterDb = dm.CharacterDb;

        for (int i = 0; i < playerCount; i++)
        {
            ulong sid = pkt.sessionIds[i];
            Vector3 pos = new Vector3(pkt.spawnX[i], pkt.spawnY[i], pkt.spawnZ[i]);

            Player player = playerPool.Get();
            if (player == null) continue;

            player.OnDespawnRequested = OnPlayerDespawnRequested;

            bool isMe = (sid == MySessionId);
            player.Spawn(sid, isMe, pos);
            player.SetObservedByLocalCamera(isMe);

            int characterId = 0;
            if (pkt.characterIds != null && pkt.characterIds.Length >= playerCount)
                characterId = pkt.characterIds[i];

            int wid = 0;
            if (pkt.weaponIds != null && pkt.weaponIds.Length >= playerCount)
                wid = pkt.weaponIds[i];

            sidToCharacterId[sid] = characterId;

            if (characterDb != null && characterDb.TryGetVisual(characterId, out var v) && v != null && v.modelPrefab != null)
            {
                player.ApplyVisual(characterId, wid);
            }
            else
            {
                player.ClearVisual();
            }

            if (isMe)
            {
                BindCameraToLocalPlayer(player);

                if (localHpBar != null)
                    localHpBar.Bind(player);

                if (damageOverlay != null)
                    damageOverlay.Bind(player);
            }

            players[sid] = player;
        }
    }

    public void LeaveGame()
    {
        spectateTargetSid = 0;

        ClearPlayers();

        CameraController.Instance.SetCameraPos(null, true);
        UnbindLocalUi();

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

    private void BindCameraToLocalPlayer(Player player)
    {
        CameraController.Instance.SetCameraPos(player.CameraPivot, false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnPlayerDespawnRequested(Player player)
    {
        if (player == null) return;

        ulong sid = player.SessionId;
        players.Remove(sid);
        playerPool.Release(player);
    }

    private void HandleGameState(ServerGameStatePacket pkt)
    {
        if (pkt.gameId != GameId) return;

        for (int i = 0; i < pkt.playerCount; i++)
        {
            PlayerState3D ps = pkt.players[i];

            if (!players.TryGetValue(ps.sessionId, out var p))
                continue;

            Vector3 pos = new Vector3(ps.x, ps.y, ps.z);
            p.ApplyServerState(pos, ps.yaw, ps.pitch, ps.hp, ps.weaponId);
        }
    }

    private void HandleShotResult(ServerShotResultPacket pkt)
    {
        if (pkt.gameId != GameId) return;

        if (pkt.shooterSessionId != 0 && pkt.shooterSessionId != MySessionId)
        {
            if (players.TryGetValue(pkt.shooterSessionId, out var shooter))
            {
                shooter.PlayShoot();
                shooter.PlayMuzzleFlash();
            }
        }

        if (pkt.hit == 1)
        {
            if (players.TryGetValue(pkt.victimSessionId, out var victim))
            {
                victim.SetHp(pkt.victimHp);

                if (pkt.victimSessionId == MySessionId && pkt.victimHp <= 0)
                    OnLocalDiedEnterSpectate();
            }
        }
    }

    private void HandleGameOver(GameOverPacket pkt)
    {
        if (pkt.gameId != GameId) return;

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
            r1.weaponId = 0;
        }

        if (count > 1)
        {
            r2.characterId = pkt.rankCharacterIds[1];
            r2.iconId = pkt.rankIconIds[1];
            r2.nickname = PacketUtil.ReadRankNickname(pkt, 1);
            r2.weaponId = 0;
        }

        if (count > 2)
        {
            r3.characterId = pkt.rankCharacterIds[2];
            r3.iconId = pkt.rankIconIds[2];
            r3.nickname = PacketUtil.ReadRankNickname(pkt, 2);
            r3.weaponId = 0;
        }

        LeaveGame();
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.GameResult });

        if (victoryPodium != null)
            victoryPodium.Show(r1, r2, r3);
    }

    private void OnLocalDiedEnterSpectate()
    {
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.MultiGame_Spectator });

        if (players.TryGetValue(MySessionId, out var me) && me != null)
            me.SetObservedByLocalCamera(false);

        spectateTargetSid = 0;
        SetSpectateTarget();
    }

    private bool IsAlive(Player p)
    {
        if (p == null) return false;
        if (p.IsDead) return false;
        if (p.Hp <= 0) return false;
        return true;
    }

    public void OnEvent(GameFlowStateEvent gameFlowStateEvent)
    {
        GameFlowState = gameFlowStateEvent.GameFlowState;
    }

    public void SetSpectateTarget()
    {
        if (players.Count == 0)
        {
            spectateTargetSid = 0;
            CameraController.Instance.SetCameraPos(null, true);
            UnbindLocalUi();
            return;
        }

        List<ulong> alive = new List<ulong>(players.Count);

        foreach (var kv in players)
        {
            ulong sid = kv.Key;
            Player p = kv.Value;

            if (sid == MySessionId) continue;
            if (!IsAlive(p)) continue;

            alive.Add(sid);
        }

        if (alive.Count == 0)
        {
            spectateTargetSid = 0;
            CameraController.Instance.SetCameraPos(null, true);
            UnbindLocalUi();
            return;
        }

        ulong prevSid = spectateTargetSid;

        int idx = alive.IndexOf(spectateTargetSid);
        idx = (idx + 1) % alive.Count;
        spectateTargetSid = alive[idx];

        if (prevSid != 0 && players.TryGetValue(prevSid, out var prevTarget) && prevTarget != null)
            prevTarget.SetObservedByLocalCamera(false);

        if (!players.TryGetValue(spectateTargetSid, out var target) || target == null)
            return;

        target.SetObservedByLocalCamera(true);

        CameraController.Instance.SetCameraPos(target.CameraPivot, false);
        if (localHpBar != null) localHpBar.Bind(target);
        if (damageOverlay != null) damageOverlay.Bind(target);
    }

    public Player GetLocalPlayer()
    {
        if (players.TryGetValue(MySessionId, out var me))
            return me;

        Debug.LogWarning("Local Player not found");
        return null;
    }
}