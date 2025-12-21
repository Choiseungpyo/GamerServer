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

    [SerializeField] private CharacterVisualDatabaseSO visualDb;

    private int leftCharacterId = -1;
    private int rightCharacterId = -1;
    private bool waitingAck;

    private void Awake()
    {
        leftButton.onClick.AddListener(OnClickLeft);
        rightButton.onClick.AddListener(OnClickRight);

        leftPreview.Setup(leftRT, leftMask);
        rightPreview.Setup(rightRT, rightMask);

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
        TcpManagerMarshal.Instance.OnSetCharacter += HandleSetCharacter;

        waitingAck = false;
        SetInteractable(true);

        BindTwoCharacters();
    }

    private void OnDisable()
    {
        if (TcpManagerMarshal.Instance == null) return;
        TcpManagerMarshal.Instance.OnSetCharacter -= HandleSetCharacter;
    }

    private void BindTwoCharacters()
    {
        leftCharacterId = 1;
        rightCharacterId = 2;

        if (visualDb != null && visualDb.TryGet(leftCharacterId, out var leftEntry))
            leftPreview.SetCharacter(leftEntry.modelPrefab, leftEntry.defaultWeaponId);
        else
            leftPreview.SetCharacter(null, 0);

        if (visualDb != null && visualDb.TryGet(rightCharacterId, out var rightEntry))
            rightPreview.SetCharacter(rightEntry.modelPrefab, rightEntry.defaultWeaponId);
        else
            rightPreview.SetCharacter(null, 0);
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

        waitingAck = true;
        SetInteractable(false);

        TcpManagerMarshal.Instance.SendSetCharacter(characterId);
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
        EventDispatcher.Dispatch(new GameFlowStateEvent { GameFlowState = GameFlowState.MultiGame_Playing });
    }

    private void SetInteractable(bool on)
    {
        if (leftButton != null) leftButton.interactable = on;
        if (rightButton != null) rightButton.interactable = on;
    }
}