using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class TcpManager : Singleton<TcpManager>
{ 
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    private int id;

    [SerializeField] private InRoomUI inRoomUI;

    public int Id
    {
        get { return id; }
        set { id = value; }
    }

    void Start()
    {
        id = -1;

        ConnectToServer("127.0.0.1", 8080);
    }

    void ConnectToServer(string ip, int port)
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.Start();
            AppendMessage("서버에 연결되었습니다.");
            
        }
        catch (Exception e)
        {
            AppendMessage("연결 실패: " + e.Message);
        }
    }

    void ReceiveLoop()
    {
        //try
        {
            while (client.Connected)
            {
                // 1단계: 헤더(길이 + 타입) 먼저 수신
                int headerSize = Marshal.SizeOf(typeof(UInt32)) + Marshal.SizeOf(typeof(int));
                byte[] headerBytes = new byte[headerSize];
                int read = 0;
                while (read < headerSize)
                {
                    int r = stream.Read(headerBytes, read, headerSize - read);
                    if (r <= 0) throw new Exception("서버 연결 끊김");
                    read += r;
                }

                // 2단계: 헤더 파싱
                UInt32 length = BitConverter.ToUInt32(headerBytes, 0);
                int type = BitConverter.ToInt32(headerBytes, 4);

                // 3단계: 전체 패킷 길이 - 헤더만큼 나머지 수신
                int bodySize = (int)(length - headerSize);
                if (bodySize > 0)
                {
                    //Debug.Log(bodySize);
                    byte[] bodyBytes = new byte[bodySize];
                    int totalRead = 0;
                    while (totalRead < bodySize)
                    {
                        int r = stream.Read(bodyBytes, totalRead, bodySize - totalRead);
                        if (r <= 0) throw new Exception("본문 수신 실패");
                        totalRead += r;
                    }

                    // 4단계: headerBytes + bodyBytes → full packet으로 병합
                    byte[] fullPacket = new byte[headerSize + bodySize];
                    Buffer.BlockCopy(headerBytes, 0, fullPacket, 0, headerSize);
                    Buffer.BlockCopy(bodyBytes, 0, fullPacket, headerSize, bodySize);

                    // 5단계: 패킷 타입에 따라 역직렬화
                    PacketParsing((PTYPE)type, fullPacket);
                }
            }
        }
        //catch (Exception ex)
        {
            //AppendMessage("수신 중 오류: " + ex.Message);
        }
    }

    private void PacketParsing(PTYPE type, byte[] fullPacket)
    {
        switch (type)
        {
            case PTYPE.NONE:
                break;

            // Title
            case PTYPE.S_C_ID:
                {
                    PACKET_ID packet = BytesToStruct<PACKET_ID>(fullPacket);
                    // 이미 id이 할당된 경우 => 다른 플레이어
                    if (id != -1)
                        return;
                    id = packet.Id;
                    EntityManager.Instance.CurrUserId = packet.Id;
                }
                break;

            case PTYPE.S_C_ENTRY_LOBBY:
                {
                    PACKET_S_C_ENTRY_LOBBY packet = BytesToStruct<PACKET_S_C_ENTRY_LOBBY>(fullPacket);

                    PanelManager.Instance.Activate(PanelType.LOBBY);
                }
                break;

            // Lobby
            // 방 목록에서 특정 방 클릭시 입장
            case PTYPE.S_C_ENTRY_ROOM:
                {
                    Packet_S_C_ROOM_USERS_INFO_HEADER header = new();
                    List<PACKET_S_C_ROOM_USER_INFO> usersInfo = new();
                    GetUserInfo(ref header, usersInfo, fullPacket);

                    PanelManager.Instance.Activate(PanelType.ROOM);
                    inRoomUI.SetInRoomUI(header, usersInfo);
                }
                break;

            // 방 생성
            case PTYPE.S_C_CREATE_ROOM:
                {
                    PACKET_S_C_CREATE_ROOM packet = BytesToStruct<PACKET_S_C_CREATE_ROOM>(fullPacket);
                    LobbyUIManager.Instance.CreateRoom(packet);
                }
                break;

            // 랜덤 입장
            case PTYPE.S_C_ENTRY_RANDOMROOM:
                {
                    Packet_S_C_ROOM_USERS_INFO_HEADER header = new();
                    List<PACKET_S_C_ROOM_USER_INFO> usersInfo = new();
                    GetUserInfo(ref header, usersInfo, fullPacket);

                    PanelManager.Instance.Activate(PanelType.ROOM);
                    inRoomUI.SetInRoomUI(header, usersInfo);
                }
                break;

            // 타이틀로 이동
            case PTYPE.S_C_MOVE_TITLE:
                PanelManager.Instance.Activate(PanelType.TITLE);
                break;

            // Room
            case PTYPE.S_C_READY_BTN:
                {
                    PACKET_S_C_READY_BTN packet = BytesToStruct<PACKET_S_C_READY_BTN>(fullPacket);
                    inRoomUI.Ready(packet);
                }
                break;

            case PTYPE.S_C_GAMETSTART_BTN:
                {
                    //PAEK packet = BytesToStruct<PACKET_S_C_CREATE_ROOM>(fullPacket);
                    //LobbyUIManager.Instance.CreateRoom(packet);
                }
                break;

            case PTYPE.S_C_TEAM_CHANGE:
                {
                    PACKET_S_C_TEAM_CHANGE pack = BytesToStruct<PACKET_S_C_TEAM_CHANGE>(fullPacket);
                    inRoomUI.TeamChange(pack);
                }
                break;

            case PTYPE.S_C_CHANGE_ROOM_OPTION:
                {
                    PACKET_S_C_CHANGE_ROOM_OPTION packet = BytesToStruct<PACKET_S_C_CHANGE_ROOM_OPTION>(fullPacket);

                    // 현재 클라가 로비인 경우
                    if (PanelManager.Instance.CurrentPanel == PanelType.LOBBY)
                        LobbyUIManager.Instance.SetRoomOption(packet);
                    else if (PanelManager.Instance.CurrentPanel == PanelType.ROOM)
                    {
                        inRoomUI.RoomOption(packet);
                    }
                }
                break;

            case PTYPE.S_C_MOVE_LOBBY:
                PanelManager.Instance.Activate(PanelType.LOBBY);
                break;



            // 게임
            //case PTYPE.S_PLAYER_SPAWN:
            //    {
            //        PACKET_S_SPAWN packet = BytesToStruct<PACKET_S_SPAWN>(fullPacket);
            //        EventManager.Instance.PostNotification(EVENT_TYPE.ADD_PLAYER, this, packet);
            //    }
            //    break;
            //case PTYPE.C_PLAYER_MOVE:
            //    {
            //        //PACKET_S_ID packet = BytesToStruct<PACKET_S_ID>(fullPacket);
            //        //id = packet.Id;
                 
            //    }
            //    break;
            //case PTYPE.S_PLAYER_MOVE:
            //    {
            //        PACKET_S_MOVE packet = BytesToStruct<PACKET_S_MOVE>(fullPacket);
            //        EventManager.Instance.PostNotification(EVENT_TYPE.APPLY_PLAYER_MOVEMENT, this, packet);
            //    }
            //    break;

            default:
                break;
        }
    }

    public void RegisterJop(Action job)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(job);
    }

    void AppendMessage(string message)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log(message);
            //chatDisplay.text += message + "\n";
        });
    }

    byte[] StructToBytes<T>(T obj) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        byte[] buffer = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, buffer, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return buffer;
    }

    T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (bytes.Length < size)
            throw new ArgumentException("입력 바이트 배열이 구조체 크기보다 작습니다.");

        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.Copy(bytes, 0, ptr, size);
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null)
        {
            receiveThread.Join(); // 스레드가 끝날 때까지 기다림
        }
        //if (receiveThread != null) receiveThread.Abort();
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }


    public byte GetByteFromBoolArray(bool[] data)
    {
        byte directionsByte = 0;  // byte 변수 (4개의 bool 값을 담을 예정)

        // 각 방향의 bool 값을 byte의 각 비트에 설정
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i])
            {
                directionsByte |= (byte)(1 << i);  // 해당 비트를 1로 설정
            }
        }

        return directionsByte;
    }

    public void SendToServer(PTYPE pType, object param = null)
    {
        switch (pType)
        {

            case PTYPE.C_S_CREATE_ROOM:
                {
                    var packet = new PACKET_C_S_CREATE_ROOM();

                    // 방 이름, 매치 타입
                    packet.Length = (uint)Marshal.SizeOf<PACKET_C_S_CREATE_ROOM>();
                    packet.Type = pType;
                    if (param is ValueTuple<string, MatchType> data)
                    {
                        packet.Id = id;
                        packet.RoomName = data.Item1;
                        packet.MatchType = data.Item2;
                    }

                    Send(packet);
                }
                break;

            case PTYPE.C_S_ENTRY_ROOM:
                {
                    var packet = new PACKET_C_S_ENTRY_ROOM();

                    // 방 번호
                    packet.Length = (uint)Marshal.SizeOf<PACKET_C_S_ENTRY_ROOM>();
                    packet.Type = pType;

                    if (param is int data)
                    {
                        packet.Id = id;
                        packet.RoomNo = data;
                    }

                    Send(packet);
                }
                break;

            // Bool 값만 전달하는 경우
            case PTYPE.C_S_ENTRY_LOBBY:
            case PTYPE.C_S_LOGOUT:
            case PTYPE.C_S_ENTRY_RANDOMROOM:
            case PTYPE.C_S_MOVE_TITLE:
            case PTYPE.C_S_READY_BTN:
            case PTYPE.C_S_GAMETSTART_BTN:
            case PTYPE.C_S_TEAM_CHANGE:
            case PTYPE.C_S_CHANGE_ROOM_OPTION:
            case PTYPE.C_S_MOVE_LOBBY:
                {
                    var packet = new PACKET();

                    // 방 번호
                    packet.Length = (uint)Marshal.SizeOf<PACKET>();
                    packet.Type = pType;

                    Send(packet);
                }
                break;

            default:
                Debug.LogWarning($"pType : {pType}  param:{param}");
                break;
        }
       
    }

    private void Send<T>(T packet) where T : struct, IPacket
    {
        uint size = (uint)Marshal.SizeOf<T>();
        var type = typeof(T);
        var lengthProp = type.GetProperty(nameof(IPacket.Length));
        lengthProp?.SetValue(packet, size);

        byte[] bytes = StructToBytes(packet);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private void GetUserInfo(ref Packet_S_C_ROOM_USERS_INFO_HEADER header, List<PACKET_S_C_ROOM_USER_INFO> usersInfo, byte[] fullPacket)
    {
        int offset = 0;

        // 헤더 파싱 (ref로 전달된 변수에 값 설정)
        header = BytesToStruct<Packet_S_C_ROOM_USERS_INFO_HEADER>(
            fullPacket.Take(Marshal.SizeOf<Packet_S_C_ROOM_USERS_INFO_HEADER>()).ToArray());
        offset += Marshal.SizeOf<Packet_S_C_ROOM_USERS_INFO_HEADER>();

        // 유저들 파싱
        for (int i = 0; i < header.UserCount; i++)
        {
            byte[] slice = fullPacket.Skip(offset).Take(Marshal.SizeOf<PACKET_S_C_ROOM_USER_INFO>()).ToArray();
            PACKET_S_C_ROOM_USER_INFO user = BytesToStruct<PACKET_S_C_ROOM_USER_INFO>(slice);
            offset += Marshal.SizeOf<PACKET_S_C_ROOM_USER_INFO>();

            usersInfo.Add(user); // 매개변수로 받은 리스트에 추가
        }
    }
}
