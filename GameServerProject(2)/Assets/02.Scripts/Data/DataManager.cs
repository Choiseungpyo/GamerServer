using System;
using UnityEngine;
using UnityEngine.Profiling;

public class DataManager : Singleton<DataManager>
{
    [Header("DB")]
    [SerializeField] private CharacterDatabase characterDb;
    [SerializeField] private WeaponDatabase weaponDb;
    [SerializeField] private IconVisualDatabaseSO iconDb;
    private ProfileData profileData = new();

    [Header("Pool Settings")]
    [SerializeField] private int characterDefaultCapacity = 10;
    [SerializeField] private int characterMaxSize = 64;
    [SerializeField] private int weaponDefaultCapacity = 10;
    [SerializeField] private int weaponMaxSize = 64;

    [SerializeField] private Transform characterPoolRoot;
    [SerializeField] private Transform weaponPoolRoot;

    public event Action OnProfileUpdated;
    public event Action OnCharacterListUpdated;
    public event Action OnWeaponListUpdated;

    public CharacterDatabase CharacterDb => characterDb;
    public WeaponDatabase WeaponDb => weaponDb;
    public ProfileData ProfileData => profileData;
    public CharacterPool CharacterPool { get; private set; }
    public WeaponPool WeaponPool { get; private set; }
    public Equipment Equipment { get; private set; }


    protected override void Awake()
    {
        base.Awake();

        if (characterDb != null) characterDb.Init();
        if (weaponDb != null) weaponDb.Init();
        if (iconDb != null) iconDb.Build();

        var tcp = TcpManagerMarshal.Instance;
        if (tcp != null)
        {
            tcp.OnLobbyProfile += HandleLobbyProfile;
            tcp.OnCharacterList += HandleCharacterList;
            tcp.OnWeaponList += HandleWeaponList;

        }

        InitPools();
        InitServices();
    }

    private void Start()
    {
        CharacterPool.Prewarm(CharacterType.Male, 8);
        CharacterPool.Prewarm(CharacterType.Female, 8);

        WeaponPool.Prewarm(WeaponType.Rifle_Default, 8);
        WeaponPool.Prewarm(WeaponType.Rifle_Dessert, 8);
        WeaponPool.Prewarm(WeaponType.Rifle_Forest, 8);
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();

        var tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        tcp.OnLobbyProfile -= HandleLobbyProfile;
        tcp.OnCharacterList -= HandleCharacterList;
        tcp.OnWeaponList -= HandleWeaponList;
    }

    private void InitPools()
    {
        bool check = Debug.isDebugBuild;

        CharacterPool = new CharacterPool(characterDb, characterPoolRoot, characterDefaultCapacity, characterMaxSize, check);
        WeaponPool = new WeaponPool(weaponDb, weaponPoolRoot, weaponDefaultCapacity, weaponMaxSize, check);
    }

    private void InitServices()
    {
        Equipment = new Equipment(weaponDb, WeaponPool);
    }

    private void HandleLobbyProfile(LobbyProfilePacket pkt)
    {
        profileData.SetProfileData(pkt);
        OnProfileUpdated?.Invoke();

        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.Lobby });
    }

    private void HandleCharacterList(CharacterListPacket pkt)
    {
        characterDb.SetServerStats(pkt.characters);
        OnCharacterListUpdated?.Invoke();
    }

    private void HandleWeaponList(WeaponListPacket pkt)
    {
        OnWeaponListUpdated?.Invoke();
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

        if (!characterDb.TryGetVisual(characterId, out var visual)) return false;
        if (visual == null || visual.modelPrefab == null) return false;

        p.ApplyVisual(characterId, weaponIdFromServer);
        return true;
    }
}