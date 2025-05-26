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
    [SerializeField] List<GameObject> panels;

    private void Start()
    {
        Activate(PanelType.TITLE);
    }

    public void Activate(params PanelType[] panelTypes)
    {
        for (int i = 0; i < panels.Count(); i++)
        {
            int index = i; // 캡처용 지역 변수
            bool value = panelTypes.Contains((PanelType)index);

            TcpManager.Instance.RegisterJop(() =>
            {
                panels[index].SetActive(value);
            });
        }
    }
}
