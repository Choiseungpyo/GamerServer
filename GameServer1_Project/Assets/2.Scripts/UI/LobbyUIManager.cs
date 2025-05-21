using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private InputField nicknameInput;
    [SerializeField] private Button nicknameBtn;

    // Start is called before the first frame update
    void Start()
    {
        nicknameBtn.onClick.AddListener(SetNickname);
    }

    public void SetNickname()
    {
        if (string.IsNullOrEmpty(nicknameInput.text))
            return;

        EventManager.Instance.PostNotification(EVENT_TYPE.SEND_USER_NICKNAME, this, nicknameInput.text);
    }

}
