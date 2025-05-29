using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.VersionControl.Asset;

public struct RoomOption
{
    public string roomName;
    public MatchType matchType;

    public RoomOption(string roomName, MatchType matchType)
    {
        this.roomName = roomName;
        this.matchType = matchType;
    }
}

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

    bool isCreateBtn;

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
        isCreateBtn = PanelManager.Instance.IsActive(PanelType.LOBBY);
        // Lobby 에서는 생성
        if (isCreateBtn)
            okTxt.text = "Create";
        // In Room 에서는 확인
        else
            okTxt.text = "Ok";
    }

    private void InitRoomNames()
    {
        roomNameDropdown.ClearOptions();

        roomNames.Add("Come on, bro. Prove it.");
        roomNames.Add("Only one walks out alive");
        roomNames.Add("No mercy, no surrender.");
        roomNames.Add("Your trigger, your justice.");
        roomNames.Add("This is your final battlefield.");

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
                return MatchType.Solo;

            case "Duo":
                return MatchType.Solo;

            case "Squad":
                return MatchType.Solo;

            default:
                Debug.LogWarning(matchType);
                return MatchType.Solo;
        }

    }

    /// <summary>
    /// 현재 플레이어가 Create 버튼을 눌렀을 때 동작
    /// </summary>
    private void Create()
    {
        // Lobby에서 방 생성시
        if(isCreateBtn)
        {
            RoomOption roomOption = new RoomOption(roomName, matchType);  // 현재 플레이어
            TcpManager.Instance.SendToServer(PTYPE.C_S_CREATE_ROOM, roomOption);
        }
        // In Game에서 방 옵션 설정시
        else
        {

        }
    }

    private void Cancle()
    {
        if (isCreateBtn)
            PanelManager.Instance.Activate(PanelType.LOBBY);
        else
            PanelManager.Instance.Activate(PanelType.ROOM);

    }
}
