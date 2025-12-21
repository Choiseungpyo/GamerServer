using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PodiumRankData
{
    public int characterId;
    public string nickname;
    public int iconId;
}

public class VictoryPodiumManager : MonoBehaviour
{
    [Header("Anchors 0=1st, 1=2nd, 2=3rd")]
    [SerializeField] private Transform[] rankAnchors = new Transform[3];

    [Header("UI Rows 0=1st, 1=2nd, 2=3rd")]
    [SerializeField] private PodiumRankUI[] rankUis = new PodiumRankUI[3];

    [Header("Optional Overrides (can be null)")]
    [SerializeField] private CharacterVisualDatabaseSO visualDbOverride;
    [SerializeField] private WeaponDatabaseSO weaponDbOverride;

    [Header("Animator")]
    [SerializeField] private RuntimeAnimatorController resultController;
    [SerializeField] private string idleStateName = "Idle";

    [Header("Triggers")]
    [SerializeField] private string firstTrigger = "PowerUp";
    [SerializeField] private string secondTrigger = "Victory";
    [SerializeField] private string thirdTrigger = "KnockDown";

    [Header("Back")]
    [SerializeField] private Button backButton;

    [Header("Podium Camera To UI")]
    [SerializeField] private Camera podiumCamera;
    [SerializeField] private RawImage podiumImage;
    [SerializeField] private RenderTexture podiumRT;

    private readonly GameObject[] spawned = new GameObject[3];
    private readonly GameObject[] spawnedWeapons = new GameObject[3];

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);

        BuildLocalDbsIfNeeded();
        SetupPodiumCamera(false);
    }

    private void BuildLocalDbsIfNeeded()
    {
        var vdb = GetCharactertVisualDb();
        if (vdb != null) vdb.Build();

        var wdb = GetWeaponDb();
        if (wdb != null) wdb.Build();
    }

    private CharacterVisualDatabaseSO GetCharactertVisualDb()
    {
        if (visualDbOverride != null) return visualDbOverride;
        var dm = DataManager.Instance;
        return (dm != null) ? dm.CharacterVisualDb : null;
    }

    private WeaponDatabaseSO GetWeaponDb()
    {
        if (weaponDbOverride != null) return weaponDbOverride;
        var dm = DataManager.Instance;
        return (dm != null) ? dm.WeaponVisualDb : null;
    }

    private Sprite GetIconSprite(int iconId)
    {
        var dm = DataManager.Instance;
        return (dm != null) ? dm.GetIconSprite(iconId) : null;
    }

    private void SetupPodiumCamera(bool on)
    {
        if (podiumCamera != null)
        {
            podiumCamera.targetTexture = on ? podiumRT : null;
            podiumCamera.enabled = on;
        }

        if (podiumImage != null)
            podiumImage.texture = on ? podiumRT : null;
    }

    private void ApplyRank(int rankIndex, PodiumRankData data)
    {
        if (rankIndex < 0 || rankIndex > 2) return;
        if (rankAnchors == null || rankAnchors.Length < 3) return;
        if (data == null) return;

        Transform anchor = rankAnchors[rankIndex];
        if (anchor == null) return;

        var visualDb = GetCharactertVisualDb();
        if (visualDb == null) return;

        CharacterVisualRow row;
        if (!visualDb.TryGet(data.characterId, out row)) return;
        if (row == null || row.modelPrefab == null) return;

        GameObject go = Instantiate(row.modelPrefab, anchor);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        spawned[rankIndex] = go;

        if (rankUis != null && rankUis.Length >= 3 && rankUis[rankIndex] != null)
        {
            Sprite icon = GetIconSprite(data.iconId);
            rankUis[rankIndex].Set(icon, data.nickname);
        }

        AttachWorldWeapon(rankIndex, go.transform, row.defaultWeaponId);

        Animator anim = go.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            anim.applyRootMotion = false;

            if (resultController != null)
                anim.runtimeAnimatorController = resultController;

            anim.Rebind();
            anim.Update(0f);

            if (!string.IsNullOrEmpty(idleStateName))
                anim.Play(idleStateName, 0, 0f);

            string trig = GetTriggerByRank(rankIndex);
            if (!string.IsNullOrEmpty(trig))
                anim.SetTrigger(trig);
        }
    }

    private void AttachWorldWeapon(int rankIndex, Transform modelRoot, int weaponId)
    {
        if (rankIndex < 0 || rankIndex > 2) return;

        if (spawnedWeapons[rankIndex] != null)
        {
            Destroy(spawnedWeapons[rankIndex]);
            spawnedWeapons[rankIndex] = null;
        }

        var weaponDb = GetWeaponDb();
        if (weaponDb == null) return;

        WeaponRow wrow;
        if (!weaponDb.TryGet(weaponId, out wrow)) return;
        if (wrow == null || wrow.worldPrefab == null) return;

        Transform hand = WeaponAttachUtil.GetRightHand(modelRoot);
        if (hand == null) hand = modelRoot;

        GameObject w = Instantiate(wrow.worldPrefab, hand);
        spawnedWeapons[rankIndex] = w;

        w.transform.localPosition = wrow.worldLocalPos;
        w.transform.localRotation = Quaternion.Euler(wrow.worldLocalEuler);
    }

    private string GetTriggerByRank(int rankIndex)
    {
        if (rankIndex == 0) return firstTrigger;
        if (rankIndex == 1) return secondTrigger;
        return thirdTrigger;
    }

    public void Show(PodiumRankData first, PodiumRankData second, PodiumRankData third)
    {
        Clear();

        BuildLocalDbsIfNeeded();
        SetupPodiumCamera(true);

        ApplyRank(0, first);
        ApplyRank(1, second);
        ApplyRank(2, third);
    }

    public void Clear()
    {
        for (int i = 0; i < 3; i++)
        {
            if (spawnedWeapons[i] != null)
            {
                Destroy(spawnedWeapons[i]);
                spawnedWeapons[i] = null;
            }

            if (spawned[i] != null)
            {
                Destroy(spawned[i]);
                spawned[i] = null;
            }
        }

        SetupPodiumCamera(false);
    }

    private void OnClickBack()
    {
        Clear();

        if (DataManager.Instance != null)
            DataManager.Instance.RequestLobbyData();
    }

    public void ShowFromGameOverPacket(GameOverPacket pkt)
    {
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

        Show(r1, r2, r3);
    }
}