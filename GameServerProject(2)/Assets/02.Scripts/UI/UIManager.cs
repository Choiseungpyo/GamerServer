using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScreenType
{
    Login = 0,
    Lobby = 1,
    CharacterSelection = 2,
    CrossHair = 3,
    DamageOverlay = 4,
    Hpbar = 5,
    GameResult = 6
}

[Serializable]
public class ScreenData
{
    public ScreenType ScreenType;
    public GameObject Panel;
}


public class UIManager : Singleton<UIManager>, IEventListener<GameFlowStateEvent>
{
    [Header("Panels")]
    [SerializeField] private List<ScreenData> screenDataList;

    protected override void Awake()
    {
        base.Awake();

        EventDispatcher.RegisterListener(this);
    }

    protected override void OnDestroy()
    {
        EventDispatcher.UnregisterListener(this);
    }

    private void Show(List<ScreenType> screenTypes)
    {
        if (screenTypes == null || screenTypes.Count == 0)
        {
            foreach (var sd in screenDataList)
                sd.Panel.SetActive(false);
            return;
        }

        var set = new HashSet<ScreenType>(screenTypes);

        foreach (var sd in screenDataList)
            sd.Panel.SetActive(set.Contains(sd.ScreenType));

    }

    public void OnEvent(GameFlowStateEvent gameFlowStateEvent)
    {
        List<ScreenType> screenTypes = new();
        screenTypes.Clear();

        switch (gameFlowStateEvent.GameFlowState)
        {
            case GameFlowState.Login:
                screenTypes.Add(ScreenType.Login);
                break;

            case GameFlowState.Lobby:
                screenTypes.Add(ScreenType.Lobby);
                break;

            case GameFlowState.Lobby_Matching:
                screenTypes.Add(ScreenType.Lobby);
                break;

            case GameFlowState.MultiGame_CharacterSelection:
                screenTypes.Add(ScreenType.CharacterSelection);
                break;

            case GameFlowState.MultiGame_Playing:
                screenTypes.Add(ScreenType.CrossHair);
                screenTypes.Add(ScreenType.DamageOverlay);
                screenTypes.Add(ScreenType.Hpbar);
                break;

            case GameFlowState.MultiGame_Spectator:
                break;

            case GameFlowState.ZombieGame_Playing:
                screenTypes.Add(ScreenType.CrossHair);
                screenTypes.Add(ScreenType.DamageOverlay);
                screenTypes.Add(ScreenType.Hpbar);
                break;

            case GameFlowState.GameResult:
                screenTypes.Add(ScreenType.GameResult);
                break;

            default:
                break;
        }

        Show(screenTypes);
    }
}