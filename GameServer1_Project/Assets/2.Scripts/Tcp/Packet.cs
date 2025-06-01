using System.Runtime.InteropServices;
using System;
using System.Numerics;
using UnityEngine.UIElements;

//[StructLayout(LayoutKind.Sequential, Pack = 1)]
//public struct Position
//{
//    public float x;
//    public float y;
//    public float z;
//};

public static class PacketConstants
{
    public const int NAME_SIZE = 30;
    public const int ROOM_NAME_SIZE = 64;
}

public enum PTYPE :int
{
    // A_B_COMMAND : A -> B 로 COMMAND 패킷 전달
    NONE,

    // Title
    S_C_ID, // 클라 고유 ID 전달
    C_S_ENTRY_LOBBY, // 로비 입장 버튼 누른 경우 : bool
    C_S_LOGOUT, // 게임 종료 버튼 누른 경우 : bool

    // Lobby
    S_C_USERS_PROFILE, // 유저 프로필 전달 

    // 로비 방 정보 UI 업데이트
    S_C_LOBBY_ALL_ROOM_INFO,
    S_C_LOBBY_ROOM_INFO,

    // 방 안의 정보 세팅
    S_C_INROOM_INFO,

    // 방 생성 버튼을 누른 경우
    C_S_CREATE_ROOM,

    // 방 목록에서 특정 방 클릭시 입장하는 경우
    C_S_ENTRY_ROOM,
    
    // 랜덤 입장 버튼을 누른 경우
    C_S_ENTRY_RANDOMROOM, // bool

    // Exit 버튼 누른 경우
    C_S_EXIT_LOBBY, // bool
    S_C_EXIT_LOBBY, // bool

    // Room

    // 준비 완료 버튼을 누른경우
    C_S_INROOM_USERSTATE, // bool
    S_C_INROOM_USERSTATE,

    //게임 시작 버튼을 누른경우
    C_S_GAMETSTART_BTN, // bool
    S_C_GAMETSTART_BTN, 

    // 팀 변경 버튼을 누른 경우
    C_S_TEAM_CHANGE, // bool
    S_C_TEAM_CHANGE,

    // 방 옵션 변경을 누른 경우
    C_S_CHANGE_ROOM_OPTION, // bool
    S_C_CHANGE_ROOM_OPTION,

    // 로비로 나가기 버튼을 누른 경우
    C_S_EXIT_ROOM,
    S_C_EXIT_ROOM,



    S_PLAYER_SPAWN,
    C_PLAYER_MOVE,
    S_PLAYER_MOVE
}

public interface IPacket
{
    UInt32 Length { get; set; }
    PTYPE Type { get; set; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_ID : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_S_C_LOBBY_USERS_INFO
{
    public int UserId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.NAME_SIZE)]
    public string UserName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_C_LOBBY_USERS_INFO_HEADER : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Count;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_S_C_ROOM_USER_INFO
{
    public int UserId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.NAME_SIZE)]
    public string UserName;
    public InRoomUserState InRoomUserState;
    public TeamType teamType;
    public int userOrderOfTeam;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_S_C_INROOM_INFO_HEADER : IPacket
{ 
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int HostId;
    public int RoomNo;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.ROOM_NAME_SIZE)]
    public string RoomName;
    public MatchType MatchType;
    public int Count;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_C_S_ENTRY_ROOM : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Id;

    public int RoomNo;
}


[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_C_S_CREATE_ROOM : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.ROOM_NAME_SIZE)]
    public string RoomName;
    public MatchType MatchType;
}


[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_S_C_CREATE_ROOM : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Id;

    public int RoomNo;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.ROOM_NAME_SIZE)]
    public string RoomName;
    public MatchType MatchType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_S_C_LOBBY_ROOM_INFO
{
    public int RoomNo;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.ROOM_NAME_SIZE)]
    public string RoomName; 
    public int CurrNumOfPeople;
    public int MaxNumOfPeople;
    public RoomState RoomState;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_C_LOBBY_ROOM_INFO_HEADER : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }

    public int Count;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_C_TEAM_CHANGE : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }

    public int PrvOrderOfTeam;
    public int CurrOrderOfTeam;
    public TeamType CurrTeamType;
}




[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_C_CHANGE_INROOM_USERSTATE : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }

    public InRoomUserState InRoomUserState;
    public TeamType TeamType;
    public int OrderOfTeam;
}


[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_CHANGE_ROOM_OPTION : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }

    public int RoomNo;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.ROOM_NAME_SIZE)]
    public string RoomName;
    public MatchType MatchType;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_SPAWN : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Id;
    public Vector3 Position;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_C_MOVE : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte Directions; // bool로 사용하려 했으나 C++과 C#에서의 정렬 문제 떄문에 byte로 변경
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_MOVE : IPacket
{
    public UInt32 Length { get; set; }
    public PTYPE Type { get; set; }
    public int Id;
    public Vector3 Position;
}

public struct PACKET_S_PLAYER_DATA
{
    public UInt32 Length;
    public PTYPE Type;
    public int Id;
    public string name;
    public int characterType;
    public Vector3 Position;
    public Vector3 Rotation;
}
