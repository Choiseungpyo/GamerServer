using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PodiumRankData
{
    public int characterId;
    public string nickname;
    public int iconId;

    public int weaponId; // 없으면 0으로 두면 무기 안 붙음
}

public class VictoryPodiumManager : MonoBehaviour
{
    [Header("Anchors 0=1st, 1=2nd, 2=3rd")]
    [SerializeField] private Transform[] rankAnchors = new Transform[3];

    [Header("UI Rows 0=1st, 1=2nd, 2=3rd")]
    [SerializeField] private PodiumRankUI[] rankUis = new PodiumRankUI[3];

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

    private readonly Character[] spawnedCharacters = new Character[3];
    private readonly CharacterType[] spawnedCharacterTypes = new CharacterType[3];

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);

        SetupPodiumCamera(false);
    }

    private Sprite GetIconSprite(int iconId)
    {
        var dm = DataManager.Instance;
        if (dm == null) return null;
        return dm.GetIconSprite(iconId);
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

    private void ApplyRank(int rankIndex, PodiumRankData rankData)
    {
        if (rankIndex < 0 || rankIndex > 2) return;
        if (rankAnchors == null || rankAnchors.Length < 3) return;
        if (rankData == null) return;

        Transform anchor = rankAnchors[rankIndex];
        if (anchor == null) return;

        var dm = DataManager.Instance;
        if (dm == null) return;

        if (dm.CharacterDb == null) return;
        if (!dm.CharacterDb.TryGetVisual(rankData.characterId, out var visual)) return;
        if (visual == null || visual.modelPrefab == null) return;

        CharacterType ctype = (CharacterType)rankData.characterId;

        Character c = dm.CharacterPool.Get(ctype);
        if (c == null) return;

        spawnedCharacters[rankIndex] = c;
        spawnedCharacterTypes[rankIndex] = ctype;

        c.transform.SetParent(anchor, false);
        c.transform.localPosition = Vector3.zero;
        c.transform.localRotation = Quaternion.identity;
        c.transform.localScale = Vector3.one;

        if (rankUis != null && rankUis.Length >= 3 && rankUis[rankIndex] != null)
        {
            Sprite icon = GetIconSprite(rankData.iconId);
            rankUis[rankIndex].Set(icon, rankData.nickname);
        }

        if (rankData.weaponId > 0 && dm.Equipment != null)
        {
            dm.Equipment.Equip(c, (WeaponType)rankData.weaponId, WeaponViewMode.World, c.GetWorldWeaponSocket());
        }
        else
        {
            if (dm.Equipment != null)
                dm.Equipment.Unequip(c);
        }

        Animator anim = c.GetComponentInChildren<Animator>(true);
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

    private string GetTriggerByRank(int rankIndex)
    {
        if (rankIndex == 0) return firstTrigger;
        if (rankIndex == 1) return secondTrigger;
        return thirdTrigger;
    }

    public void Show(PodiumRankData first, PodiumRankData second, PodiumRankData third)
    {
        Clear();

        SetupPodiumCamera(true);

        ApplyRank(0, first);
        ApplyRank(1, second);
        ApplyRank(2, third);
    }

    public void Clear()
    {
        var dm = DataManager.Instance;

        for (int i = 0; i < 3; i++)
        {
            var c = spawnedCharacters[i];
            if (c == null) continue;

            if (dm != null && dm.Equipment != null)
                dm.Equipment.Unequip(c);

            if (dm != null && dm.CharacterPool != null)
                dm.CharacterPool.Release(spawnedCharacterTypes[i], c);

            spawnedCharacters[i] = null;
            spawnedCharacterTypes[i] = default;
        }

        SetupPodiumCamera(false);
    }

    private void OnClickBack()
    {
        Clear();
        TcpManagerMarshal.Instance.SendLobbyEnter();
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

        Show(r1, r2, r3);
    }
}