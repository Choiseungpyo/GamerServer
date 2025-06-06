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
    private string userName;

    [SerializeField] private InRoomUI inRoomUI;
    [SerializeField] private LobbyUIManager lobbyUIManger;


    public int Id
    {
        get { return id; }
        private set { id = value; }
    }

    public string UserName
    {
        get { return userName; }
        private set { userName = value; }
    }

    void Start()
    {
        id = -1;
        userName = "";

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
        try
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
                // 기본 패킷만 보낸 경우
                else
                {
                    byte[] fullPacket = new byte[headerSize];
                    Buffer.BlockCopy(headerBytes, 0, fullPacket, 0, headerSize);
                    PacketParsing((PTYPE)type, fullPacket);
                }
            }
        }
        catch (Exception ex)
        {
            AppendMessage("수신 중 오류: " + ex.Message);
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
                    PACKET_INT packet = BytesToStruct<PACKET_INT>(fullPacket);
                    // 이미 id이 할당된 경우 => 다른 플레이어
                    if (id != -1)
                        return;
                    id = packet.Value;
                }
                break;

            case PTYPE.S_C_USERS_PROFILE:
                {
                    PACKET_INFO_HEADER header = new();
                    List<PACKET_S_C_LOBBY_USERS_INFO> usersInfo = new();
                    GetUserInfo(ref header, usersInfo, fullPacket);

                    if(userName.Equals(""))
                    {
                        foreach (var userInfo in usersInfo)
                        {
                            if (userInfo.UserId == id)
                                userName = userInfo.UserName;
                        }
                    }


                    PanelManager.Instance.Activate(PanelType.LOBBY);
                    lobbyUIManger.AddUserProfile(usersInfo);
                }
                break;

            // 맨 처음 로비 입장시
            // 방에서 로비로 이동시
            // 방 생성시
            case PTYPE.S_C_LOBBY_ALL_ROOM_INFO:
                {
                    PACKET_INFO_HEADER header = new();
                    List<PACKET_S_C_LOBBY_ROOM_INFO> roomsInfo = new();
                    GetUserInfo(ref header, roomsInfo, fullPacket);

                    PanelManager.Instance.Activate(PanelType.LOBBY);
                    lobbyUIManger.UpdateAllRoomInfo(roomsInfo);
                }
                break;

            // 방 하나의 정보만 바뀌었을 경우
            case PTYPE.S_C_LOBBY_ROOM_INFO:
                {
                    PACKET_INFO_HEADER header = new();
                    List<PACKET_S_C_LOBBY_ROOM_INFO> roomsInfo = new();
                    GetUserInfo(ref header, roomsInfo, fullPacket);

                    // PACKET_S_C_LOBBY_ROOM_INFO packet = BytesToStruct<PACKET_S_C_LOBBY_ROOM_INFO>(fullPacket);
                    lobbyUIManger.UpdateRoomInfo(roomsInfo[0]);
                }
                break;

            // Lobby
            // 방 안에서의 정보 업데이트
            case PTYPE.S_C_INROOM_INFO:
                {
                    PACKET_S_C_INROOM_INFO_HEADER header = new();
                    List<PACKET_S_C_ROOM_USER_INFO> usersInfo = new();
                    GetUserInfo(ref header, usersInfo, fullPacket);

                    PanelManager.Instance.Activate(PanelType.ROOM);
                    inRoomUI.SetInRoomUI(id, header, usersInfo);
                }
                break;

            case PTYPE.S_C_CHAT_LOBBY:
                {
                    PACKET_CHAT packet = BytesToStruct<PACKET_CHAT>(fullPacket);
                    lobbyUIManger.AddMsg(packet.Msg);
                }
                break;

            case PTYPE.S_C_CHAT_ROOM:
                {
                    PACKET_CHAT packet = BytesToStruct<PACKET_CHAT>(fullPacket);
                    inRoomUI.AddMsg(packet.Msg);
                }
                break;

            // 타이틀로 이동
            case PTYPE.S_C_EXIT_LOBBY:
                PanelManager.Instance.Activate(PanelType.TITLE);
                break;

            // Room
            case PTYPE.S_C_INROOM_USERSTATE:
                {
                    PACKET_S_C_CHANGE_INROOM_USERSTATE packet = BytesToStruct<PACKET_S_C_CHANGE_INROOM_USERSTATE>(fullPacket);
                    inRoomUI.ChangeInRoomUserState(packet);
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
                    PACKET_CHANGE_ROOM_OPTION packet = BytesToStruct<PACKET_CHANGE_ROOM_OPTION>(fullPacket);

                    PanelManager.Instance.IsActive(PanelType.LOBBY, isActive =>
                    {
                        // 현재 클라가 로비인 경우
                        if (isActive)
                        {
                            lobbyUIManger.SetRoomOption(packet);
                        }
                        // 현재 클라가 나머지인 경우(방, 방 옵션)
                        else
                        {
                            PanelManager.Instance.Activate(PanelType.ROOM);
                            inRoomUI.RoomOption(packet);
                        }
                    });
                }
                break;

            case PTYPE.S_C_EXIT_ROOM:
                PanelManager.Instance.Activate(PanelType.LOBBY);
                break;

            case PTYPE.S_C_GAME_SPAWN_ALL:
                {
                    PACKET_INFO_HEADER header = new();
                    List<PACKET_S_C_PLAYERENTITY_DATA> datas = new();
                    GetUserInfo(ref header, datas, fullPacket);

                    PanelManager.Instance.Activate(PanelType.GAME);
                    EntityManager.Instance.SpawnAllEntity(datas);
                }
                break;

            // 게임
            //case PTYPE.S_PlayerEntity_SPAWN:
            //    {
            //        PACKET_S_SPAWN packet = BytesToStruct<PACKET_S_SPAWN>(fullPacket);
            //        EventManager.Instance.PostNotification(EVENT_TYPE.ADD_PlayerEntity, this, packet);
            //    }
            //    break;
            //case PTYPE.C_S_MOVE_PLAYER:
            //    {
            //        //PACKET_S_ID packet = BytesToStruct<PACKET_S_ID>(fullPacket);
            //        //id = packet.Id;

            //    }
            //    break;
            //case PTYPE.S_C_MOVE_PLAYER:
            //    {
            //        PACKET_S_MOVE packet = BytesToStruct<PACKET_S_MOVE>(fullPacket);
            //        EventManager.Instance.PostNotification(EVENT_TYPE.APPLY_PlayerEntity_MOVEMENT, this, packet);
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
        RegisterJop(() =>
        {
            Debug.Log(message);
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

            case PTYPE.C_S_CHAT_LOBBY:
            case PTYPE.C_S_CHAT_ROOM:
                {
                    PACKET_CHAT packet = new PACKET_CHAT();
                    packet.Type = pType;
                    packet.Msg = (string)param;

                    Send(packet);
                }
                break;

            case PTYPE.C_S_CHANGE_ROOM_OPTION:
                {
                    var packet = new PACKET_CHANGE_ROOM_OPTION();

                    // 방 이름, 매치 타입
                    packet.Length = (uint)Marshal.SizeOf<PACKET_CHANGE_ROOM_OPTION>();
                    packet.Type = pType;
                    if (param is ValueTuple<string, MatchType> data)
                    {
                        packet.RoomNo = -1;
                        packet.RoomName = data.Item1;
                        packet.MatchType = data.Item2;
                    }

                    Send(packet);
                }
                break;

            //case PTYPE.C_S_GAME_REDTEAM_SPAWNPOS:
            //case PTYPE.C_S_GAME_BLUETEAM_SPAWNPOS:
            //    {
            //        var (posList, teamType) = ((List<Vector3>, TeamType))param;

            //        List<PACKET_POSITION> packs = new List<PACKET_POSITION>();
                    
            //        foreach(var pos in posList)
            //        {
            //            PACKET_POSITION pack = new PACKET_POSITION();
            //            pack.Position.X = pos.x;
            //            pack.Position.Y = pos.y;
            //            pack.Position.Z = pos.z;
            //            packs.Add(pack);
            //        }

            //        PACKET_C_S_TEAM_SPAWNPOS header = new PACKET_C_S_TEAM_SPAWNPOS();
            //        header.Type = pType;
            //        header.TeamType = teamType;
            //        header.PositionCount = (ushort)packs.Count;
            //        header.Length = (ushort)(Marshal.SizeOf(typeof(PACKET_INFO_HEADER)) + packs.Count * Marshal.SizeOf(typeof(PACKET_POSITION)));
                    
            //        List<byte> buffer = new List<byte>();
            //        buffer.AddRange(StructToBytes(header));

            //        foreach (var p in packs)
            //        {
            //            buffer.AddRange(StructToBytes(p));
            //        }

            //        Send(buffer.ToArray());
            //    }
            //    break;

            // 기본 패킷 이용하는 경우
            case PTYPE.C_S_ENTRY_LOBBY:
            case PTYPE.C_S_LOGOUT:
            case PTYPE.C_S_ENTRY_RANDOMROOM:
            case PTYPE.S_C_EXIT_LOBBY:
            case PTYPE.C_S_INROOM_USERSTATE:
            case PTYPE.C_S_TEAM_CHANGE:
            case PTYPE.C_S_EXIT_ROOM:
            case PTYPE.C_S_EXIT_LOBBY:
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

    private void Send(byte[] data)
    {
        stream.Write(data, 0, data.Length);
        stream.Flush();
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

    private void GetUserInfo<THeader, TBody>(ref THeader header, List<TBody> usersInfo, byte[] fullPacket) where THeader : struct where TBody : struct
    {
        int offset = 0;

        // 헤더 파싱
        header = BytesToStruct<THeader>(fullPacket.Take(Marshal.SizeOf<THeader>()).ToArray());
        offset += Marshal.SizeOf<THeader>();

        // Count 프로퍼티를 반사(reflection)로 가져옴
        var userCountProp = typeof(THeader).GetField("Count");
        if (userCountProp == null)
            throw new Exception("THeader must have a Count property");

        int userCount = (int)userCountProp.GetValue(header);

        // 유저들 파싱
        for (int i = 0; i < userCount; i++)
        {
            byte[] slice = fullPacket.Skip(offset).Take(Marshal.SizeOf<TBody>()).ToArray();
            TBody user = BytesToStruct<TBody>(slice);
            offset += Marshal.SizeOf<TBody>();

            usersInfo.Add(user);
        }
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        if (receiveThread != null)
        {
            receiveThread.Join(); // 스레드가 끝날 때까지 기다림
        }
        //if (receiveThread != null) receiveThread.Abort();
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }

}
