using UnityEngine;

public interface IEvent { }

public class GameFlowStateEvent : IEvent
{
    public GameFlowState GameFlowState;
}