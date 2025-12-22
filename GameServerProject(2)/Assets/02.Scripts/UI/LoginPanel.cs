using System;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour
{
    [Header("Account")]
    [SerializeField] private TMP_InputField idInput;
    [SerializeField] private TMP_InputField pwInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Server")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;

    private const string PrefIp = "server_ip";
    private const string PrefPort = "server_port";

    private bool loginInFlight;

    private void Awake()
    {
        loginButton.onClick.AddListener(OnClickLogin);
    }

    private void OnEnable()
    {
        TcpManagerMarshal.Instance.OnLoginRes += HandleLoginRes;

        loginInFlight = false;
        loginButton.interactable = true;
        statusText.text = "";

        LoadServerPrefsToUI();
    }

    private void OnDisable()
    {
        TcpManagerMarshal.Instance.OnLoginRes -= HandleLoginRes;
    }

    private void LoadServerPrefsToUI()
    {
        string ip = PlayerPrefs.GetString(PrefIp, NetConst.IP);
        int port = PlayerPrefs.GetInt(PrefPort, NetConst.PORT);

        if (ipInput != null) ipInput.text = ip;
        if (portInput != null) portInput.text = port.ToString();
    }

    private bool TryGetServerEndpoint(out string ip, out int port)
    {
        ip = (ipInput != null) ? ipInput.text.Trim() : "";
        string portStr = (portInput != null) ? portInput.text.Trim() : "";

        if (string.IsNullOrEmpty(ip)) ip = NetConst.IP;

        if (!int.TryParse(portStr, out port))
            port = NetConst.PORT;

        if (port < 1 || port > 65535)
            return false;

        IPAddress dummy;
        if (!IPAddress.TryParse(ip, out dummy))
            return false;

        return true;
    }

    private void SaveServerPrefs(string ip, int port)
    {
        PlayerPrefs.SetString(PrefIp, ip);
        PlayerPrefs.SetInt(PrefPort, port);
        PlayerPrefs.Save();
    }

    private void OnClickLogin()
    {
        if (loginInFlight) return;

        string ip;
        int port;
        if (!TryGetServerEndpoint(out ip, out port))
        {
            statusText.text = "invalid ip/port";
            return;
        }

        SaveServerPrefs(ip, port);

        loginInFlight = true;
        loginButton.interactable = false;

        string id = idInput.text;
        string pw = pwInput.text;

        statusText.text = "connecting";

        if (!TcpManagerMarshal.Instance.EnsureConnected(ip, port))
        {
            statusText.text = "connect fail";
            loginInFlight = false;
            loginButton.interactable = true;
            return;
        }

        TcpManagerMarshal.Instance.SendLogin(id, pw);
        statusText.text = "login request sent";
    }

    private void HandleLoginRes(LoginResPacket pkt)
    {
        loginInFlight = false;
        loginButton.interactable = true;

        if (pkt.ok == 0)
        {
            statusText.text = "login failed";
            return;
        }

        TcpManagerMarshal.Instance.SendLobbyEnter();

        statusText.text = "Login Ok : Lodding";
    }
}