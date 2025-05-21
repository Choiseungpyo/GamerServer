using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IListener
{
    void OnEvent(EVENT_TYPE EventType, Component Sender, object Param = null);
}
