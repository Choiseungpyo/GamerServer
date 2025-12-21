using System;
using UnityEngine;

public class DataManager : Singleton<DataManager>, IEventListener<GameFlowStateEvent>
{
    [Header("Runtime DB (from server)")]
    [SerializeField] private CharacterDatabaseSO characterStatDb;
    [SerializeField] private WeaponRuntimeDatabaseSO weaponStatDb;

    [Header("Visual DB (local asset)")]
    [SerializeField] private CharacterVisualDatabaseSO characterVisualDb;
    [SerializeField] private WeaponDatabaseSO weaponVisualDb;
    [SerializeField] private IconVisualDatabaseSO iconDb;

    private bool gotProfile;
    private bool gotChars;
    private bool gotWeapons;
    private bool firedLobbyReady;

    public bool IsLobbyDataReady => gotProfile && gotChars && gotWeapons;

    public event Action OnLobbyDataReady;
    public event Action OnProfileUpdated;
    public event Action OnCharacterListUpdated;
    public event Action OnWeaponListUpdated;

    public CharacterVisualDatabaseSO CharacterVisualDb => characterVisualDb;
    public WeaponDatabaseSO WeaponVisualDb => weaponVisualDb;
    public CharacterDatabaseSO CharacterStatDb => characterStatDb;
    public WeaponRuntimeDatabaseSO WeaponStatDb => weaponStatDb;


    protected override void Awake()
    {
        base.Awake();

        if (characterVisualDb != null) characterVisualDb.Build();
        if (weaponVisualDb != null) weaponVisualDb.Build();
        if (iconDb != null) iconDb.Build();
    }

    private void OnEnable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        tcp.OnLobbyProfile += HandleLobbyProfile;
        tcp.OnCharacterList += HandleCharacterList;
        tcp.OnWeaponList += HandleWeaponList;
    }

    private void OnDisable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        tcp.OnLobbyProfile -= HandleLobbyProfile;
        tcp.OnCharacterList -= HandleCharacterList;
        tcp.OnWeaponList -= HandleWeaponList;
    }

    public void RequestLobbyData()
    {
        ResetLobbyBootstrap();
        TcpManagerMarshal.Instance.SendLobbyEnter();
    }

    public void ResetLobbyBootstrap()
    {
        gotProfile = false;
        gotChars = false;
        gotWeapons = false;
        firedLobbyReady = false;
    }

    private void HandleLobbyProfile(LobbyProfilePacket pkt)
    {
        ClientContext.PlayerId = pkt.playerId;
        ClientContext.IconId = pkt.iconId;
        ClientContext.Nickname = MarshalNet.ReadFixedAscii(pkt.nickname);
        ClientContext.Total = pkt.totalGameCount;
        ClientContext.Win = pkt.winCount;

        gotProfile = true;
        TryFireLobbyReady();

        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.Lobby });
    }

    private void HandleCharacterList(CharacterListPacket pkt)
    {
        if (characterStatDb != null)
            characterStatDb.BuildFromCharacterList(pkt);

        gotChars = true;
        OnCharacterListUpdated?.Invoke();
        TryFireLobbyReady();
    }

    private void HandleWeaponList(WeaponListPacket pkt)
    {
        if (weaponStatDb != null)
            weaponStatDb.BuildFromWeaponList(pkt);

        gotWeapons = true;
        OnWeaponListUpdated?.Invoke();
        TryFireLobbyReady();
    }

    private void TryFireLobbyReady()
    {
        if (firedLobbyReady) return;
        if (!IsLobbyDataReady) return;

        firedLobbyReady = true;
        OnLobbyDataReady?.Invoke();
    }

    public Sprite GetIconSprite(int iconId)
    {
        if (iconDb == null) 
            return null;

        return iconDb.GetOrDefault(iconId);
    }

    public bool TryApplyPlayerVisual(Player p, int characterId, int weaponIdFromServer)
    {
        if (p == null) return false;

        if (characterVisualDb != null) characterVisualDb.Build();
        if (weaponVisualDb != null) weaponVisualDb.Build();

        if (characterVisualDb == null) return false;

        CharacterVisualRow row;
        if (!characterVisualDb.TryGet(characterId, out row)) return false;
        if (row == null || row.modelPrefab == null) return false;

        p.SetCharacterModel(characterId, row.modelPrefab);

        int wid = weaponIdFromServer;
        if (wid < 0) wid = row.defaultWeaponId;

        p.SetDefaultWeapon(wid);
        return true;
    }

    public void OnEvent(GameFlowStateEvent gameFlowStateEvent)
    {
        switch(gameFlowStateEvent.GameFlowState)
        {
            case GameFlowState.MultiGame_Playing:
                characterVisualDb.Build();
                break;
        }
    }
}