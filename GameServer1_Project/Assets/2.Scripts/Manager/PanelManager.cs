using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [System.Serializable]
    public struct PanelData
    {
        public PanelType type;
        public GameObject panel;
    }

    [SerializeField] List<PanelData> panels;
    Dictionary<PanelType, GameObject> panelDict = new();

    private void Start()
    {
        Activate(PanelType.TITLE);
    }

    public void Activate(params PanelType[] panelTypes)
    {
        foreach (var v in panelDict)
        {
            var panelType = v.Key;
            var panelObject = v.Value;
            bool isActive = panelTypes.Contains(panelType);

            TcpManager.Instance.RegisterJop(() =>
            {
                panelObject.SetActive(isActive);
            });
        }
    }

    public bool IsActive(PanelType panelType)
    {
        if (panelDict.TryGetValue(panelType, out var panel))
        {
            return panel.activeSelf;
        }

        Debug.LogWarning($"[UI] PanelType '{panelType}' not found in panelDict.");
        return false; // 또는 기본값
    }

}
