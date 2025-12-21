using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private PlayerPoolComponent playerPool;

    [Header("Runtime DB (from server)")]
    [SerializeField] private CharacterDatabaseSO characterStatDb;
    [SerializeField] private WeaponRuntimeDatabaseSO weaponStatDb;

    [Header("UI")]
    [SerializeField] private LocalHpBarUI localHpBar;
    [SerializeField] private DamageOverlayUI damageOverlay;

    [SerializeField] private VictoryPodiumManager victoryPodium;

    private ulong spectateTargetSid;

    private readonly Dictionary<ulong, int> sidToCharacterId = new Dictionary<ulong, int>(NetConst.MAX_CHARACTERS);
    private readonly Dictionary<ulong, Player> players = new Dictionary<ulong, Player>(NetConst.MAX_PLAYERS);

    private int selectedCharacterId = -1;


    private int selfIndex;
    private int playerCount;

    public int GameId { get; private set; }

    public ulong MySessionId { get; private set; }

    public GameFlowState GameFlowState { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        EventDispatcher.RegisterListener(this);
        selectedCharacterId = -1;

        spectateTargetSid = 0;
    }

    private void Start()
    {
        GameFlowState = GameFlowState.Login;
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = this.GameFlowState });
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventDispatcher.UnregisterListener(this);
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

        for (int i = 0; i < playerCount; i++)
        {
            ulong sid = pkt.sessionIds[i];
            Vector3 pos = new Vector3(pkt.spawnX[i], pkt.spawnY[i], pkt.spawnZ[i]);

            // 플레이어 세팅
            Player player = playerPool.Get();
            player.OnDespawnRequested = OnPlayerDespawnRequested;

            bool isMe = (sid == MySessionId);
            player.Spawn(sid, isMe, pos);

            // 캐릭터 세팅
            int characterId = 0;
            if (pkt.characterIds != null && pkt.characterIds.Length >= playerCount)
                characterId = pkt.characterIds[i];

            sidToCharacterId[sid] = characterId;

            var characterVisualDB = DataManager.Instance.CharacterVisualDb;
            GameObject characterPrefab;

            characterVisualDB.TryGetPrefab(characterId, out characterPrefab);

            player.SetCharacterModel(characterId, characterPrefab);
            int wid = 0;
            if (pkt.weaponIds != null && pkt.weaponIds.Length >= playerCount)
                wid = pkt.weaponIds[i];

            // 무기 세팅
            player.SetDefaultWeapon(wid);

            // 카메라 세팅
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

            Player p;
            if (!players.TryGetValue(ps.sessionId, out p))
                continue;

            Vector3 pos = new Vector3(ps.x, ps.y, ps.z);
            p.ApplyServerState(pos, ps.yaw, ps.pitch, ps.hp, ps.weaponId);
        }
    }

    private void HandleShotResult(ServerShotResultPacket pkt)
    {
        if (pkt.gameId != GameId) return;

        // 1) 누가 쐈는지 찾고, 다른 사람에게만 발사 연출 재생
        if (pkt.shooterSessionId != 0 && pkt.shooterSessionId != MySessionId)
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
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.MultiGame_Spectator });

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

    /// <summary>
    /// 관전 대상 변경
    /// </summary>
    public void SetSpectateTarget()
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

            if (sid == MySessionId) continue;
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


        var spectateTarget = players[spectateTargetSid];

        // 관전 대상 변경시 플레이어 FP, TP에 따른 비활성화 모습 변경해야함

        CameraController.Instance.SetCameraPos(spectateTarget.CameraPivot, false);
        localHpBar.Bind(spectateTarget);
    }

    public Player GetLocalPlayer()
    {
        Player me = null;
        if (players.TryGetValue(MySessionId, out me))
        {
            return me;
        }

        Debug.LogWarning($"Local Player ID : {MySessionId}");
        Debug.LogWarning($"Player Ids : {players.Keys}");
        return null;
    }
}