using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PanelType : int
{
    TITLE=0,
    LOBBY,
    ROOMOPTION,
    ROOM
}

public class PanelManager : Singleton<PanelManager>
{
    [SerializeField] List<GameObject> panels;

    private void Start()
    {
        Activate(PanelType.TITLE);
    }

    public void Activate(params PanelType[] panelTypes)
    {
        foreach (var panel in panels)
        {
            // 활성화할 대상 배열에 포함되어 있으면 true, 아니면 false
            bool shouldActivate = System.Array.IndexOf(panelTypes, panel) >= 0;
            panel.SetActive(shouldActivate);
        }
    }
}
