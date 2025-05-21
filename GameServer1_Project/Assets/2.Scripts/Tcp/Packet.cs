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
    public const int NAME_SIZE = 64;
}

public enum PTYPE :int
{
    NONE,
    S_ID,
    S_PLAYER_SPAWN,
    C_PLAYER_MOVE,
    S_PLAYER_MOVE
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_ID
{
    public UInt32 Length;
    public int Type;
    public int Id;
}



[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_SPAWN
{
    public UInt32 Length;
    public int Type;
    public int Id;
    public Vector3 Position;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_C_MOVE
{
    public UInt32 Length;
    public int Type;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte Directions; // bool로 사용하려 했으나 C++과 C#에서의 정렬 문제 떄문에 byte로 변경
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PACKET_S_MOVE
{
    public UInt32 Length;
    public int Type;
    public int Id;
    public Vector3 Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct PACKET_C_NICKNAME
{
    public UInt32 Length;
    public int Type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PacketConstants.NAME_SIZE)]
    public string Nickname;
}

public struct PACKET_S_PLAYER_DATA
{
    public UInt32 Length;
    public int Type;
    public int id;
    public string name;
    public int characterType;
    public Vector3 Position;
    public Vector3 Rotation;
}