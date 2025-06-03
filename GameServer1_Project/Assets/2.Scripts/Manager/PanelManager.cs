using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PanelType : int
{
    TITLE=0,
    LOBBY,
    ROOMOPTION,
    ROOM,
    GAME
}

public class PanelManager : Singleton<PanelManager>
{
    [System.Serializable]
    public struct PanelData
    {
        public PanelType type;
        public GameObject panel;
    }

    [SerializeField] private List<PanelData> panelDatas;
    private Dictionary<PanelType, GameObject> panelDict = new();


    protected override void Awake()
    {
        base.Awake();

        foreach (var panel in panelDatas)
            panelDict.Add(panel.type, panel.panel);
    }

    private void Start()
    {
        Activate((PanelType[])Enum.GetValues(typeof(PanelType)));
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
                // 활성해야할 경우 && 해당 패널이 비활성화 되어있는 경우
                if(isActive && !panelObject.activeSelf)
                {
                    panelObject.SetActive(isActive);
                }
                // 비활성해야할 경우 && 해당 패널이 활성화 되어있는 경우
                else if(!isActive && panelObject.activeSelf)
                {
                    panelObject.SetActive(isActive);
                }
                
            });
        }
    }

    public void IsActive(PanelType panelType, Action<bool> callback)
    {
        if (!panelDict.TryGetValue(panelType, out var panel))
        {
            Debug.LogWarning(panelType);
            callback?.Invoke(false);
            return;
        }

        TcpManager.Instance.RegisterJop(() =>
        {
            callback?.Invoke(panel.activeSelf);
        });
    }

}
