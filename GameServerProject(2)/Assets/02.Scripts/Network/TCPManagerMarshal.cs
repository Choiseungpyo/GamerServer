using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class TcpManagerMarshal : Singleton<TcpManagerMarshal>
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread recvThread;
    private volatile bool running;

    public Action<LoginResPacket> OnLoginRes;
    public Action<LobbyProfilePacket> OnLobbyProfile;

    public Action<CharacterListPacket> OnCharacterList;
    public Action<WeaponListPacket> OnWeaponList;


    public Action<ServerMatchWaitPacket> OnMatchWait;

    public Action<ServerSetCharacterPacket> OnSetCharacter;

    public Action<GameStartPacket> OnGameStart;
    public Action<ServerGameStatePacket> OnGameState;
    public Action<ServerShotResultPacket> OnShotResult;
    public Action<GameOverPacket> OnGameOver;


    private string connectedIp = "";
    private int connectedPort = -1;
    private readonly object connLock = new object();

    public bool IsConnected
    {
        get
        {
            try
            {
                return client != null && client.Connected && stream != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool EnsureConnected(string ip, int port)
    {
        lock (connLock)
        {
            if (IsConnected)
            {
                bool same =
                    string.Equals(connectedIp, ip, StringComparison.Ordinal) &&
                    connectedPort == port;

                if (same) return true;

                DisconnectInternal();
            }

            bool ok = Connect(ip, port);
            if (ok)
            {
                connectedIp = ip;
                connectedPort = port;
            }
            return ok;
        }
    }

    private void DisconnectInternal()
    {
        try
        {
            Disconnect();
        }
        finally
        {
            connectedIp = "";
            connectedPort = -1;
        }
    }

    public bool Connect(string ip, int port)
    {
        try
        {
            Disconnect();

            client = new TcpClient(ip, port);
            client.NoDelay = true;
            stream = client.GetStream();

            running = true;
            recvThread = new Thread(ReceiveLoop);
            recvThread.IsBackground = true;
            recvThread.Start();

            Debug.Log("Connect OK " + ip + ":" + port);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Connect FAIL " + ip + ":" + port + " " + e.Message);
            Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        running = false;

        try { if (stream != null) stream.Close(); } catch { }
        try { if (client != null) client.Close(); } catch { }

        stream = null;
        client = null;

        if (recvThread != null && recvThread.IsAlive && Thread.CurrentThread != recvThread)
        {
            try { recvThread.Join(200); } catch { }
        }
        recvThread = null;
    }

    private void ReceiveLoop()
    {
        try
        {
            while (running && client != null && client.Connected)
            {
                byte[] headerBytes = ReadExact(4);
                ushort size = BitConverter.ToUInt16(headerBytes, 0);
                ushort type = BitConverter.ToUInt16(headerBytes, 2);

                if (size < 4) throw new Exception("bad packet size");

                int bodySize = size - 4;
                byte[] bodyBytes = bodySize > 0 ? ReadExact(bodySize) : Array.Empty<byte>();

                byte[] full = new byte[size];
                Buffer.BlockCopy(headerBytes, 0, full, 0, 4);
                if (bodySize > 0) Buffer.BlockCopy(bodyBytes, 0, full, 4, bodySize);

                HandlePacket((PacketType)type, full);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ReceiveLoop end: " + e.Message);
        }
        finally
        {
            Disconnect();
        }
    }

    private byte[] ReadExact(int bytes)
    {
        var s = stream;
        if (s == null) throw new Exception("disconnected");

        byte[] buffer = new byte[bytes];
        int read = 0;

        while (read < bytes)
        {
            int r = s.Read(buffer, read, bytes - read);
            if (r <= 0) throw new Exception("disconnected");
            read += r;
        }

        return buffer;
    }

    private void HandlePacket(PacketType type, byte[] full)
    {
        switch (type)
        {
            case PacketType.S_LOGIN_RES:
                {
                    var pkt = MarshalNet.BytesToStruct<LoginResPacket>(full);
                    UnityMainThread(() => OnLoginRes?.Invoke(pkt));
                    break;
                }

            case PacketType.S_LOBBY_PROFILE:
                {
                    var pkt = MarshalNet.BytesToStruct<LobbyProfilePacket>(full);
                    UnityMainThread(() =>
                    {
                        OnLobbyProfile?.Invoke(pkt);
                    });
                    break;
                }

            case PacketType.S_CHARACTER_LIST:
                {
                    var pkt = MarshalNet.BytesToStruct<CharacterListPacket>(full);
                    UnityMainThread(() =>
                    {
                        if (pkt.characters == null || pkt.characters.Length != NetConst.MAX_CHARACTERS)
                            pkt.characters = new CharacterRow[NetConst.MAX_CHARACTERS];

                        OnCharacterList?.Invoke(pkt);
                    });
                    break;
                }

            case PacketType.S_WEAPON_LIST:
                {
                    var pkt = MarshalNet.BytesToStruct<WeaponListPacket>(full);
                    UnityMainThread(() =>
                    {
                        if (pkt.weapons == null || pkt.weapons.Length != NetConst.MAX_WEAPONS)
                            pkt.weapons = new WeaponInfo[NetConst.MAX_WEAPONS];

                        OnWeaponList?.Invoke(pkt);
                    });
                    break;
                }

            case PacketType.S_MATCH_WAIT:
                {
                    var pkt = MarshalNet.BytesToStruct<ServerMatchWaitPacket>(full);
                    UnityMainThread(() => OnMatchWait?.Invoke(pkt));
                    break;
                }

            case PacketType.S_GAME_START:
                {
                    var pkt = MarshalNet.BytesToStruct<GameStartPacket>(full);

                    if (pkt.sessionIds == null || pkt.sessionIds.Length != NetConst.MAX_PLAYERS)
                        pkt.sessionIds = new ulong[NetConst.MAX_PLAYERS];

                    if (pkt.spawnX == null || pkt.spawnX.Length != NetConst.MAX_PLAYERS)
                        pkt.spawnX = new float[NetConst.MAX_PLAYERS];

                    if (pkt.spawnY == null || pkt.spawnY.Length != NetConst.MAX_PLAYERS)
                        pkt.spawnY = new float[NetConst.MAX_PLAYERS];

                    if (pkt.spawnZ == null || pkt.spawnZ.Length != NetConst.MAX_PLAYERS)
                        pkt.spawnZ = new float[NetConst.MAX_PLAYERS];

                    if (pkt.characterIds == null || pkt.characterIds.Length != NetConst.MAX_PLAYERS)
                        pkt.characterIds = new int[NetConst.MAX_PLAYERS];

                    UnityMainThread(() => OnGameStart?.Invoke(pkt));
                    break;
                }
            case PacketType.S_GAME_STATE:
                {
                    var pkt = MarshalNet.BytesToStruct<ServerGameStatePacket>(full);
                    UnityMainThread(() => OnGameState?.Invoke(pkt));
                    break;
                }

            case PacketType.S_GAME_SHOT_RESULT:
                {
                    var pkt = MarshalNet.BytesToStruct<ServerShotResultPacket>(full);
                    UnityMainThread(() => OnShotResult?.Invoke(pkt));
                    break;
                }
            case PacketType.S_GAME_OVER:
                {
                    var pkt = MarshalNet.BytesToStruct<GameOverPacket>(full);

                    if (pkt.rankSessionIds == null || pkt.rankSessionIds.Length != NetConst.MAX_PLAYERS)
                        pkt.rankSessionIds = new ulong[NetConst.MAX_PLAYERS];

                    if (pkt.rankCharacterIds == null || pkt.rankCharacterIds.Length != NetConst.MAX_PLAYERS)
                        pkt.rankCharacterIds = new int[NetConst.MAX_PLAYERS];

                    if (pkt.rankIconIds == null || pkt.rankIconIds.Length != NetConst.MAX_PLAYERS)
                        pkt.rankIconIds = new int[NetConst.MAX_PLAYERS];

                    if (pkt.rankNicknamesFlat == null || pkt.rankNicknamesFlat.Length != NetConst.MAX_PLAYERS * NetConst.MAX_NICK_LEN)
                        pkt.rankNicknamesFlat = new byte[NetConst.MAX_PLAYERS * NetConst.MAX_NICK_LEN];

                    UnityMainThread(() => OnGameOver?.Invoke(pkt));
                    break;
                }

            case PacketType.S_SET_CHARACTER:
                {
                    var pkt = MarshalNet.BytesToStruct<ServerSetCharacterPacket>(full);
                    UnityMainThread(() => OnSetCharacter?.Invoke(pkt));
                    break;
                }

            default:
                break;
        }
    }

    private void UnityMainThread(Action a)
    {
        if (a == null) return;

        var dispatcher = UnityMainThreadDispatcher.Instance;
        if (dispatcher == null)
        {
            return;
        }

        dispatcher.Enqueue(a);
    }

    private void SendRaw(byte[] data)
    {
        if (stream == null) return;
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    public void SendLogin(string id, string pw)
    {
        LoginReqPacket pkt = new LoginReqPacket();
        pkt.header.size = (ushort)Marshal.SizeOf(typeof(LoginReqPacket));
        pkt.header.type = (ushort)PacketType.C_LOGIN_REQ;

        pkt.userId = new byte[NetConst.MAX_ID_LEN];
        pkt.password = new byte[NetConst.MAX_PW_LEN];

        MarshalNet.WriteFixedAscii(pkt.userId, id);
        MarshalNet.WriteFixedAscii(pkt.password, pw);

        SendRaw(MarshalNet.StructToBytes(pkt));
    }

    public void SendMatchStart()
    {
        MatchStartPacket pkt = new MatchStartPacket();
        pkt.header.size = (ushort)Marshal.SizeOf(typeof(MatchStartPacket));
        pkt.header.type = (ushort)PacketType.C_MATCH_START;

        SendRaw(MarshalNet.StructToBytes(pkt));
    }

    public void SendLobbyEnter()
    {
        LobbyEnterPacket pkt = new LobbyEnterPacket();
        pkt.header.size = (ushort)Marshal.SizeOf(typeof(LobbyEnterPacket));
        pkt.header.type = (ushort)PacketType.C_LOBBY_ENTER;

        SendRaw(MarshalNet.StructToBytes(pkt));
    }

    public void SendInput(int gameId, int tick, float moveX, float moveZ, float yaw, float pitch, uint buttons, int weaponId)
    {
        GameInputPacket pkt = new GameInputPacket();
        pkt.header.size = (ushort)Marshal.SizeOf(typeof(GameInputPacket));
        pkt.header.type = (ushort)PacketType.C_GAME_INPUT;

        pkt.gameId = gameId;
        pkt.tick = tick;
        pkt.moveX = moveX;
        pkt.moveZ = moveZ;
        pkt.yaw = yaw;
        pkt.pitch = pitch;
        pkt.buttons = buttons;
        pkt.weaponId = weaponId;

        SendRaw(MarshalNet.StructToBytes(pkt));
    }

    public void SendFire(int gameId, int clientTick, int weaponId)
    {
        GameFirePacket pkt = new GameFirePacket();
        pkt.header.size = (ushort)Marshal.SizeOf(typeof(GameFirePacket));
        pkt.header.type = (ushort)PacketType.C_GAME_FIRE;

        pkt.gameId = gameId;
        pkt.clientTick = clientTick;
        pkt.weaponId = weaponId;

        SendRaw(MarshalNet.StructToBytes(pkt));
    }

    public void SendSetCharacter(int characterId)
    {
        SetCharacterPacket pkt = new SetCharacterPacket();
        pkt.header.size = (ushort)Marshal.SizeOf(typeof(SetCharacterPacket));
        pkt.header.type = (ushort)PacketType.C_SET_CHARACTER;
        pkt.characterId = characterId;

        SendRaw(MarshalNet.StructToBytes(pkt));
    }
}