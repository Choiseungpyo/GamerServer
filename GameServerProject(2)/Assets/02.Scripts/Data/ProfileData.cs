using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ProfileData
{
    public int PlayerId { get; private set; }
    public int IconId { get; private set; }
    public string Nickname { get; private set; }
    public int TotalGameCount { get; private set; }
    public int WinCount { get; private set; }

    public void SetProfileData(LobbyProfilePacket pkt)
    {
        PlayerId = pkt.playerId;
        IconId = pkt.iconId;
        Nickname = MarshalNet.ReadFixedAscii(pkt.nickname);
        TotalGameCount = pkt.totalGameCount;
        WinCount = pkt.totalGameCount;
    }
}