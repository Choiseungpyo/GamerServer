using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPanel : MonoBehaviour
{
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [SerializeField] private RawImage leftRaw;
    [SerializeField] private RawImage rightRaw;

    [SerializeField] private CharacterPreviewRig leftPreview;
    [SerializeField] private CharacterPreviewRig rightPreview;

    [SerializeField] private RenderTexture leftRT;
    [SerializeField] private RenderTexture rightRT;

    [SerializeField] private LayerMask leftMask;
    [SerializeField] private LayerMask rightMask;

    private int leftCharacterId = -1;
    private int rightCharacterId = -1;
    private bool waitingAck;

    private void Awake()
    {
        if (leftButton != null) leftButton.onClick.AddListener(OnClickLeft);
        if (rightButton != null) rightButton.onClick.AddListener(OnClickRight);

        if (leftPreview != null) leftPreview.Setup(leftRT, leftMask);
        if (rightPreview != null) rightPreview.Setup(rightRT, rightMask);

        if (leftRaw != null)
        {
            leftRaw.texture = leftRT;
            leftRaw.raycastTarget = false;
        }

        if (rightRaw != null)
        {
            rightRaw.texture = rightRT;
            rightRaw.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp != null) tcp.OnSetCharacter += HandleSetCharacter;

        waitingAck = false;
        SetInteractable(true);

        BindTwoCharacters();
    }

    private void OnDisable()
    {
        var tcp = TcpManagerMarshal.Instance;
        if (tcp != null) tcp.OnSetCharacter -= HandleSetCharacter;
    }

    private void BindTwoCharacters()
    {
        leftCharacterId = 1;
        rightCharacterId = 2;

        if (leftPreview != null)
            leftPreview.SetCharacter(leftCharacterId, 0);

        if (rightPreview != null)
            rightPreview.SetCharacter(rightCharacterId, 0);
    }

    private void OnClickLeft()
    {
        TrySend(leftCharacterId);
    }

    private void OnClickRight()
    {
        TrySend(rightCharacterId);
    }

    private void TrySend(int characterId)
    {
        if (waitingAck) return;
        if (characterId < 0) return;

        var tcp = TcpManagerMarshal.Instance;
        if (tcp == null) return;

        waitingAck = true;
        SetInteractable(false);

        tcp.SendSetCharacter(characterId);
    }

    private void HandleSetCharacter(ServerSetCharacterPacket pkt)
    {
        waitingAck = false;

        bool ok = pkt.ok != 0;
        if (!ok)
        {
            SetInteractable(true);
            return;
        }

        GameSessionManager.Instance.SetSelectedCharacter(pkt.currentCharacterId);
    }

    private void SetInteractable(bool on)
    {
        if (leftButton != null) leftButton.interactable = on;
        if (rightButton != null) rightButton.interactable = on;
    }
}