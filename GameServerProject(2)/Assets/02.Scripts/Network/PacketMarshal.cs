using System;
using System.Runtime.InteropServices;

public static class NetConst
{
    public const string IP = "127.0.0.1";
    public const int PORT = 9000;

    public const int MAX_PLAYERS = 3;
    public const int MAX_ID_LEN = 32;
    public const int MAX_PW_LEN = 32;
    public const int MAX_NICK_LEN = 32;

    public const int MAX_CHARACTERS = 64;
    public const int MAX_CHAR_NAME_LEN = 32;

    public const int MAX_WEAPONS = 16;
    public const int MAX_WEAPON_NAME_LEN = 32;
}

public enum PacketType : ushort
{
    C_LOGIN_REQ = 1,
    S_LOGIN_RES = 101,

    C_MATCH_START = 11,
    S_MATCH_WAIT = 111,

    C_LOBBY_ENTER = 13,
    S_LOBBY_PROFILE = 113,
    S_CHARACTER_LIST = 114,
    S_WEAPON_LIST = 115,

    C_SET_CHARACTER = 14,
    S_SET_CHARACTER = 116,

    S_GAME_START = 121,

    C_GAME_INPUT = 21,
    C_GAME_FIRE = 22,

    S_GAME_STATE = 122,
    S_GAME_SHOT_RESULT = 123,
    S_GAME_OVER = 124,

    C_QUIT = 99
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PacketHeader
{
    public ushort size;
    public ushort type;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LoginReqPacket
{
    public PacketHeader header;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_ID_LEN)]
    public byte[] userId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PW_LEN)]
    public byte[] password;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LoginResPacket
{
    public PacketHeader header;
    public byte ok;
    public int playerId;
    public int iconId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_NICK_LEN)]
    public byte[] nickname;

    public int totalGameCount;
    public int winCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MatchStartPacket
{
    public PacketHeader header;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ServerMatchWaitPacket
{
    public PacketHeader header;
    public int queueSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LobbyEnterPacket
{
    public PacketHeader header;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LobbyProfilePacket
{
    public PacketHeader header;
    public int playerId;
    public int iconId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_NICK_LEN)]
    public byte[] nickname;

    public int totalGameCount;
    public int winCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CharacterInfo
{
    public int characterId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_CHAR_NAME_LEN)]
    public byte[] characterName;

    public int hp;
    public float moveSpeed;
    public int attackPower;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CharacterListPacket
{
    public PacketHeader header;
    public int characterCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_CHARACTERS)]
    public CharacterInfo[] characters;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WeaponInfo
{
    public int weaponId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_WEAPON_NAME_LEN)]
    public byte[] weaponName;

    public int attackPower;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WeaponListPacket
{
    public PacketHeader header;
    public int weaponCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_WEAPONS)]
    public WeaponInfo[] weapons;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SetCharacterPacket
{
    public PacketHeader header;
    public int characterId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ServerSetCharacterPacket
{
    public PacketHeader header;
    public byte ok;
    public int currentCharacterId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GameStartPacket
{
    public PacketHeader header;
    public int gameId;
    public int selfIndex;
    public int playerCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public ulong[] sessionIds;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public float[] spawnX;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public float[] spawnY;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public float[] spawnZ;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public int[] characterIds;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public int[] weaponIds;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GameInputPacket
{
    public PacketHeader header;
    public int gameId;
    public int tick;
    public float moveX;
    public float moveZ;
    public float yaw;
    public float pitch;
    public uint buttons;
    public int weaponId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GameFirePacket
{
    public PacketHeader header;
    public int gameId;
    public int shotId;
    public int clientTick;
    public int weaponId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerState3D
{
    public ulong sessionId;
    public float x;
    public float y;
    public float z;
    public float yaw;
    public float pitch;
    public int hp;
    public int weaponId;
    public int lastAckTick;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ServerGameStatePacket
{
    public PacketHeader header;
    public int gameId;
    public int serverTick;
    public int playerCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public PlayerState3D[] players;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ServerShotResultPacket
{
    public PacketHeader header;
    public int gameId;
    public int shotId;
    public ulong shooterSessionId;
    public byte hit;
    public ulong victimSessionId;
    public float hitX;
    public float hitY;
    public float hitZ;
    public int damage;
    public int victimHp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GameOverPacket
{
    public PacketHeader header;
    public int gameId;

    public int rankCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public ulong[] rankSessionIds;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public int[] rankCharacterIds;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS)]
    public int[] rankIconIds;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NetConst.MAX_PLAYERS * NetConst.MAX_NICK_LEN)]
    public byte[] rankNicknamesFlat;
}