using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPanel : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text totalGameCountText;
    [SerializeField] private TMP_Text winCountText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button matchButton;

    private bool requestedMatch;

    private void Awake()
    {
        if (matchButton != null)
            matchButton.onClick.AddListener(OnClickMatch);
    }

    private void OnEnable()
    {
        requestedMatch = false;

        if (matchButton != null) matchButton.interactable = true;
        if (statusText != null) statusText.text = "";

        var tcp = TcpManagerMarshal.Instance;
        if (tcp != null)
        {
            tcp.OnMatchWait += HandleMatchWait;
            tcp.OnGameStart += HandleGameStart;
        }

        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.OnProfileUpdated += RefreshProfileUI;
        }

        RefreshProfileUI();
    }

    private void OnDisable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp != null)
        {
            tcp.OnMatchWait -= HandleMatchWait;
            tcp.OnGameStart -= HandleGameStart;
        }

        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.OnProfileUpdated -= RefreshProfileUI;
        }
    }

    private void RefreshProfileUI()
    {
        if (nicknameText != null) nicknameText.text = ClientContext.Nickname;
        if (totalGameCountText != null) totalGameCountText.text = "Total: " + ClientContext.Total;
        if (winCountText != null) winCountText.text = "Win : " + ClientContext.Win;
   
        if (iconImage != null)
        {
            var dm = DataManager.Instance;
            iconImage.sprite = (dm != null) ? dm.GetIconSprite(ClientContext.IconId) : null;
        }
    }

    private void OnClickMatch()
    {
        if (requestedMatch) return;
        requestedMatch = true;

        if (matchButton != null) matchButton.interactable = false;
        if (statusText != null) statusText.text = "matching...";

        TcpManagerMarshal.Instance.SendMatchStart();
    }

    private void HandleMatchWait(ServerMatchWaitPacket pkt)
    {
        if (statusText != null)
            statusText.text = "Wating : " + pkt.queueSize + " / " + NetConst.MAX_PLAYERS;

        if (pkt.queueSize >= NetConst.MAX_PLAYERS)
        {
            EventDispatcher.Dispatch(new GameFlowStateEvent
            {
                GameFlowState = GameFlowState.MultiGame_CharacterSelection
            });
        }
    }

    private void HandleGameStart(GameStartPacket pkt)
    {
        if (statusText != null) statusText.text = "game start";

        EventDispatcher.Dispatch(new GameFlowStateEvent
        {
            GameFlowState = GameFlowState.MultiGame_Playing
        });
    }
}