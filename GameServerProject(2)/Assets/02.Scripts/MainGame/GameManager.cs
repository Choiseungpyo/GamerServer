using System.Collections;
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

public class GameManager : Singleton<GameManager>, IEventListener<GameFlowStateEvent>
{
    public GameFlowState GameFlowState { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        EventDispatcher.RegisterListener(this);
    }

    // Start is called before the first frame update
    private void Start()
    {
        GameFlowState = GameFlowState.Login;
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = this.GameFlowState });   
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventDispatcher.UnregisterListener(this);
    }


    public void OnEvent(GameFlowStateEvent gameFlowStateEvent)
    {
        GameFlowState = gameFlowStateEvent.GameFlowState;
    }
}
