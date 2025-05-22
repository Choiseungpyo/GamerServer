using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PanelType
{
    LOBBY=0,
    ROOMOPTION=1,
    ROOM=2
}

public class PanelManager : Singleton<PanelManager>
{
    [SerializeField] List<GameObject> panels;

    private void Start()
    {
        HideAllPanel();
        ActivateOnly(PanelType.LOBBY);
    }

    private void HideAllPanel()
    {
        for (int i = 0; i < panels.Count; i++)
            SetPanelState((PanelType)i, false);
    }

    public void SetPanelState(PanelType panelType, bool value)
    {
        GameObject panel = panels[(int)panelType];
        if (panel)
            panel.SetActive(value);
        else
            Debug.LogWarning(panelType);
    }

    public void ActivateOnly(PanelType panelType)
    {
        foreach (var panel in panels)
            panel.SetActive(panel == panels[(int)panelType]);
    }

    public void ActivateOnly(params PanelType[] panelTypes)
    {
        foreach (var panel in panels)
        {
            // 활성화할 대상 배열에 포함되어 있으면 true, 아니면 false
            bool shouldActivate = System.Array.IndexOf(panelTypes, panel) >= 0;
            panel.SetActive(shouldActivate);
        }
    }
}
