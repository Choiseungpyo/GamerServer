using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 방 생성시 방 옵션 UI
/// </summary>
public class RoomOptionUI : MonoBehaviour
{
    [SerializeField] TMP_Dropdown roomNameDropdown;
    [SerializeField] TMP_Dropdown matchTypeDropdown;

    [SerializeField] Button cancleBtn;
    [SerializeField] Button okBtn;
    [SerializeField] TMP_Text okTxt;

    List<string> roomNames = new();
    List<string> matchTypes = new();

    string roomName;
    MatchType matchType;

    private void Awake()
    {
        cancleBtn.onClick.AddListener(Cancle);
        okBtn.onClick.AddListener(Create);

        InitRoomNames();
        InitMatchTypes();

        roomNameDropdown.onValueChanged.AddListener((int index) =>
        {
            OnDropdownChanged(roomNameDropdown, index);  // 어떤 드롭다운에서 왔는지 넘김
        });

        matchTypeDropdown.onValueChanged.AddListener((int index) =>
        {
            OnDropdownChanged(matchTypeDropdown, index);
        });
    }

    private void OnEnable()
    {
        SetOkBtnTxt();
    }

    private void SetOkBtnTxt()
    {
        PanelManager.Instance.IsActive(PanelType.LOBBY, isLobby =>
        {
            okTxt.text = isLobby ? "Create" : "Ok";
        });
    }

    private void InitRoomNames()
    {
        roomNameDropdown.ClearOptions();

        roomNames.Add("Come on, bro.");
        roomNames.Add("Boom boom room");
        roomNames.Add("Let's gooo");
        roomNames.Add("I'll Kill You");
        roomNames.Add("Gg ez room");

        roomNameDropdown.AddOptions(roomNames);

        roomName = roomNames[0];
    }

    private void InitMatchTypes()
    {
        matchTypeDropdown.ClearOptions();

        foreach (var matchType in (MatchType[])System.Enum.GetValues(typeof(MatchType)))
            matchTypes.Add(matchType.ToString());

        matchTypeDropdown.AddOptions(matchTypes);

        matchType = (MatchType)System.Enum.Parse(typeof(MatchType), matchTypes[0]);
    }

    private void OnDropdownChanged(TMP_Dropdown source, int index)
    {
        string selectedText = source.options[index].text;

        if (source == roomNameDropdown)
            roomName = selectedText;
        else if (source == matchTypeDropdown)
            matchType = GetMatchType(selectedText);
    }

    private MatchType GetMatchType(string matchType)
    {
        switch (matchType)
        {
            case "Solo":
                return MatchType.SOLO;

            case "Duo":
                return MatchType.DUO;

            case "Squad":
                return MatchType.SQUAD;

            default:
                Debug.LogWarning(matchType);
                return MatchType.SOLO;
        }

    }

    /// <summary>
    /// 현재 플레이어가 Create 버튼을 눌렀을 때 동작
    /// </summary>
    private void Create()
    {
        PanelManager.Instance.IsActive(PanelType.LOBBY, isLobby =>
        {
            if(isLobby)
                TcpManager.Instance.SendToServer(PTYPE.C_S_CREATE_ROOM, (roomName, matchType));
            else
                TcpManager.Instance.SendToServer(PTYPE.C_S_CHANGE_ROOM_OPTION, (roomName, matchType));
        });
    }

    private void Cancle()
    {
        PanelManager.Instance.IsActive(PanelType.LOBBY, isLobby =>
        {
            if (isLobby)
                PanelManager.Instance.Activate(PanelType.LOBBY);
            else
                PanelManager.Instance.Activate(PanelType.ROOM);
        });
    }
}
