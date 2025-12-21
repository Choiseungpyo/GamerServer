using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SfxType
{
    Zombie_Attack,
    Zombie_Dead,
    Zombie_Hit,
    Player_Shoot,
    UI_Click
}

public enum BgmType
{
    Login,
    Lobby,
    Multi_Playing,
    Zombie_Playing,
    GameResult
}

[Serializable]
public class BgmData
{
    public BgmType state;
    public AudioClip clip;
}

[Serializable]
public class SfxData
{
    public SfxType type;
    public AudioClip clip;
}


public class SoundManager : Singleton<SoundManager>, IEventListener<GameFlowStateEvent>
{
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private List<BgmData> bgmList = new List<BgmData>();

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int sfxDefaultCapacity = 16;
    [SerializeField] private int sfxMaxSize = 64;
    [SerializeField] private List<SfxData> sfxList = new List<SfxData>();

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    private readonly Dictionary<BgmType, AudioClip> bgmDict = new Dictionary<BgmType, AudioClip>();
    private readonly Dictionary<SfxType, AudioClip> sfxDict = new Dictionary<SfxType, AudioClip>();

    private SfxSourcePool sfxPool;

    protected override void Awake()
    {
        base.Awake();

        EventDispatcher.RegisterListener(this);

        sfxPool = new SfxSourcePool(sfxSourcePrefab, transform, sfxDefaultCapacity, sfxMaxSize);
        BuildDict();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventDispatcher.UnregisterListener(this);
    }

    private void BuildDict()
    {
        bgmDict.Clear();
        for (int i = 0; i < bgmList.Count; i++)
        {
            var d = bgmList[i];
            if (d == null || d.clip == null) continue;
            bgmDict[d.state] = d.clip;
        }

        sfxDict.Clear();
        for (int i = 0; i < sfxList.Count; i++)
        {
            var d = sfxList[i];
            if (d == null || d.clip == null) continue;
            sfxDict[d.type] = d.clip;
        }
    }

    private void PlayBgm(BgmType state, bool restartIfSame = false)
    {
        if (!bgmDict.TryGetValue(state, out var clip) || clip == null)
            return;

        if (!restartIfSame && bgmSource.isPlaying && bgmSource.clip == clip)
            return;

        bgmSource.clip = clip;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    private void StopBgm()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySfx(SfxType type)
    {
        if (!sfxDict.TryGetValue(type, out var clip) || clip == null)
            return;

        AudioSource src = sfxPool.Get();
        if (src == null) return;

        src.spatialBlend = 0f;
        src.volume = masterVolume * sfxVolume;
        src.PlayOneShot(clip);

        StartCoroutine(CoReleaseAfter(src, clip.length));
    }

    private IEnumerator CoReleaseAfter(AudioSource src, float seconds)
    {
        if (src == null) yield break;

        float t = 0f;
        while (t < seconds)
        {
            if (src == null) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (src != null)
            sfxPool.Release(src);
    }


    public void OnEvent(GameFlowStateEvent gameFlowStateEvent)
    {
        switch (gameFlowStateEvent.GameFlowState)
        {
            case GameFlowState.Login:
                PlayBgm(BgmType.Login);
                break;

            case GameFlowState.Lobby:
                PlayBgm(BgmType.Lobby);
                break;

            case GameFlowState.Lobby_Matching:
                break;

            case GameFlowState.MultiGame_CharacterSelection:
                PlayBgm(BgmType.Multi_Playing);
                break;

            case GameFlowState.MultiGame_Playing:
                break;

            case GameFlowState.MultiGame_Spectator:
                break;

            case GameFlowState.ZombieGame_Playing:
                PlayBgm(BgmType.Zombie_Playing);
                break;

            case GameFlowState.GameResult:
                PlayBgm(BgmType.GameResult);
                break;

            default:
                break;
        }

       
    }
}
