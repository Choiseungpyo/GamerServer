using System.Text;

public static class PacketUtil
{
    public static string ReadRankNickname(GameOverPacket pkt, int rankIndex)
    {
        if (pkt.rankNicknamesFlat == null) return "";
        if (rankIndex < 0 || rankIndex >= NetConst.MAX_PLAYERS) return "";

        int max = NetConst.MAX_NICK_LEN;
        int start = rankIndex * max;
        if (pkt.rankNicknamesFlat.Length < start + max) return "";

        int len = 0;
        while (len < max && pkt.rankNicknamesFlat[start + len] != 0) len++;

        return Encoding.UTF8.GetString(pkt.rankNicknamesFlat, start, len);
    }
}
