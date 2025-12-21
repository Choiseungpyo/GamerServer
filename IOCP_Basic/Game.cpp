#include "Game.h"
#include "DBManager.h"
#include "IOCP_EchoServer.h"
#include "Logger.h"
#include <cmath>
#include <algorithm>
#include <cstring>
#include <vector>
#include <sstream>

static float DegToRad(float deg) { return deg * 3.1415926535f / 180.0f; }

Game::Game(int gameId, uint64_t matchId, const std::vector<Session*>& playersIn)
    : gameId(gameId), matchId(matchId), gameOverSent(false)
{
    sessions = playersIn;

    InitWeaponStats();

    for (int i = 0; i < MAX_PLAYERS; ++i)
        players[i] = PlayerSim{};

    std::memset(deathOrder, 0, sizeof(deathOrder));
    deathCount = 0;

    for (int i = 0; i < (int)playersIn.size() && i < MAX_PLAYERS; ++i)
    {
        Session* s = playersIn[i];
        if (!s) continue;

        players[i].sessionId = s->id;
        players[i].playerId = s->playerId;
        players[i].iconId = s->iconId;
        players[i].hp = 100;
        players[i].weaponId = s->weaponId;
        players[i].connected = true;

        players[i].characterId = s->gameCharacterId;

        std::memset(players[i].nickname, 0, sizeof(players[i].nickname));
        std::strncpy(players[i].nickname, s->nickname, MAX_NICK_LEN - 1);
        players[i].nickname[MAX_NICK_LEN - 1] = '\0';
    }

    std::vector<float> spx;
    spx.push_back(0.0f);
    spx.push_back(0.0f);
    spx.push_back(-8.0f);

    std::vector<float> spz;
    spz.push_back(13.0f);
    spz.push_back(-13.0f);
    spz.push_back(0.0f);

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (players[i].sessionId == 0) continue;
        players[i].pos = { spx[i], 0.0f, spz[i] };
    }

    {
        std::ostringstream oss;
        oss << "Game created gameId=" << gameId
            << " matchId=" << (unsigned long long)matchId
            << " sessions=" << sessions.size();

        for (int i = 0; i < MAX_PLAYERS; ++i)
        {
            if (players[i].sessionId == 0) continue;
            oss << " p" << i
                << "(sid=" << (unsigned long long)players[i].sessionId
                << " pid=" << players[i].playerId
                << " cid=" << players[i].characterId
                << " wid=" << players[i].weaponId
                << ")";
        }

        Logger::Info(LogTag::GAMECREATE, oss.str());
    }
}

int Game::GetGameId() const { return gameId; }
bool Game::IsFinished() const { return finished; }

int Game::FindIndexBySession(uint64_t sid) const
{
    for (int i = 0; i < MAX_PLAYERS; ++i)
        if (players[i].sessionId == sid) return i;
    return -1;
}

void Game::OnInput(Session* sender, const InputState& in)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (finished) return;
    if (!sender) return;

    int idx = FindIndexBySession(sender->id);
    if (idx < 0) return;
    if (players[idx].hp <= 0) return;

    players[idx].lastInput = in;
    players[idx].lastAckTick = in.tick;
    players[idx].yaw = in.yaw;
    players[idx].pitch = in.pitch;
}

static bool AlreadyInDeathOrder(const uint64_t* arr, int count, uint64_t sid)
{
    for (int i = 0; i < count; ++i)
        if (arr[i] == sid) return true;
    return false;
}

void Game::OnDisconnect(Session* s)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (finished) return;
    if (!s) return;

    int idx = FindIndexBySession(s->id);
    if (idx < 0) return;

    bool wasAlive = (players[idx].hp > 0);

    players[idx].connected = false;
    players[idx].hp = 0;

    for (size_t i = 0; i < sessions.size(); ++i)
    {
        if (sessions[i] == s)
        {
            sessions[i] = nullptr;
            break;
        }
    }

    {
        std::ostringstream oss;
        oss << "Disconnect gameId=" << gameId
            << " sid=" << (unsigned long long)s->id
            << " wasAlive=" << (wasAlive ? 1 : 0);
        Logger::Warning(LogTag::SESSSIONCLOSE, oss.str());
    }

    if (wasAlive)
    {
        uint64_t sid = players[idx].sessionId;
        if (sid != 0 && deathCount < MAX_PLAYERS && !AlreadyInDeathOrder(deathOrder, deathCount, sid))
            deathOrder[deathCount++] = sid;
    }

    int alive = 0;
    uint64_t lastAliveSid = 0;

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (players[i].sessionId == 0) continue;
        if (players[i].hp <= 0) continue;

        alive++;
        lastAliveSid = players[i].sessionId;
    }

    if (alive <= 1)
    {
        finished = true;

        uint64_t rankSid[3] = { 0, 0, 0 };

        BuildRankSid3(rankSid, alive, lastAliveSid, deathOrder, deathCount);
        BroadcastGameOverRank3(rankSid);

        {
            std::ostringstream oss;
            oss << "Game finished by disconnect gameId=" << gameId
                << " rank0=" << (unsigned long long)rankSid[0]
                << " rank1=" << (unsigned long long)rankSid[1]
                << " rank2=" << (unsigned long long)rankSid[2];
            Logger::Info(LogTag::GAMEOVER, oss.str());
        }

    }
}

bool Game::RayVsSphere(const Vec3& o, const Vec3& d, const Vec3& c, float r, float maxDist, float& outT) const
{
    Vec3 oc{ o.x - c.x, o.y - c.y, o.z - c.z };
    float b = oc.x * d.x + oc.y * d.y + oc.z * d.z;
    float cval = oc.x * oc.x + oc.y * oc.y + oc.z * oc.z - r * r;
    float disc = b * b - cval;
    if (disc < 0.0f) return false;

    float s = std::sqrt(disc);
    float t1 = -b - s;
    float t2 = -b + s;

    float t = 1e30f;
    if (t1 >= 0.0f) t = t1;
    else if (t2 >= 0.0f) t = t2;

    if (t < 0.0f || t > maxDist) return false;
    outT = t;
    return true;
}

static float Dot3(const Vec3& a, const Vec3& b)
{
    return a.x * b.x + a.y * b.y + a.z * b.z;
}

static Vec3 Sub3(const Vec3& a, const Vec3& b)
{
    return Vec3{ a.x - b.x, a.y - b.y, a.z - b.z };
}

bool Game::RayVsCapsule(const Vec3& ro, const Vec3& rd, const Vec3& pa, const Vec3& pb, float r, float maxDist, float& outT) const
{
    Vec3 ba = Sub3(pb, pa);
    Vec3 oa = Sub3(ro, pa);

    float baba = Dot3(ba, ba);
    float bard = Dot3(ba, rd);
    float baoa = Dot3(ba, oa);
    float rdoa = Dot3(rd, oa);
    float oaoa = Dot3(oa, oa);

    float a = baba - bard * bard;
    float b = baba * rdoa - baoa * bard;
    float c = baba * oaoa - baoa * baoa - r * r * baba;

    if (std::fabs(a) < 1e-8f)
    {
        {
            Vec3 oc = oa;
            float bb = Dot3(rd, oc);
            float cc = Dot3(oc, oc) - r * r;
            float h = bb * bb - cc;
            if (h >= 0.0f)
            {
                float t = -bb - std::sqrt(h);
                if (t >= 0.0f && t <= maxDist) { outT = t; return true; }
            }
        }
        {
            Vec3 oc = Sub3(ro, pb);
            float bb = Dot3(rd, oc);
            float cc = Dot3(oc, oc) - r * r;
            float h = bb * bb - cc;
            if (h >= 0.0f)
            {
                float t = -bb - std::sqrt(h);
                if (t >= 0.0f && t <= maxDist) { outT = t; return true; }
            }
        }
        return false;
    }

    float h = b * b - a * c;
    if (h < 0.0f) return false;

    float t = (-b - std::sqrt(h)) / a;

    float y = baoa + t * bard;

    if (y > 0.0f && y < baba)
    {
        if (t >= 0.0f && t <= maxDist)
        {
            outT = t;
            return true;
        }
        return false;
    }

    Vec3 oc = (y <= 0.0f) ? oa : Sub3(ro, pb);
    float bb = Dot3(rd, oc);
    float cc = Dot3(oc, oc) - r * r;
    float hh = bb * bb - cc;
    if (hh < 0.0f) return false;

    float t2 = -bb - std::sqrt(hh);
    if (t2 >= 0.0f && t2 <= maxDist)
    {
        outT = t2;
        return true;
    }
    return false;
}

void Game::OnFire(Session* sender, int shotId, int clientTick, int weaponId)
{
    (void)clientTick;
    (void)weaponId;

    ServerShotResultPacket res{};
    res.header.size = sizeof(res);
    res.header.type = S_GAME_SHOT_RESULT;
    res.gameId = gameId;
    res.shotId = shotId;
    res.shooterSessionId = 0;
    res.hit = 0;
    res.victimSessionId = 0;
    res.hitX = 0.0f;
    res.hitY = 0.0f;
    res.hitZ = 0.0f;
    res.damage = 0;
    res.victimHp = 0;

    std::lock_guard<std::mutex> lock(mtx);

    if (!sender)
    {
        Logger::Warning(LogTag::PLAYERSHOOT, "OnFire sender is null");
        BroadcastShotResult(res);
        return;
    }

    if (finished)
    {
        res.shooterSessionId = sender->id;
        BroadcastShotResult(res);
        return;
    }

    int shooter = FindIndexBySession(sender->id);
    if (shooter < 0)
    {
        res.shooterSessionId = sender->id;

        {
            std::ostringstream oss;
            oss << "OnFire shooter not found gameId=" << gameId
                << " sid=" << (unsigned long long)sender->id
                << " shotId=" << shotId;
            Logger::Warning(LogTag::PLAYERSHOOT, oss.str());
        }

        BroadcastShotResult(res);
        return;
    }

    res.shooterSessionId = players[shooter].sessionId;

    if (players[shooter].hp <= 0)
    {
        BroadcastShotResult(res);
        return;
    }

    float yawDeg = players[shooter].yaw;
    float pitchDeg = players[shooter].pitch;

    if (pitchDeg > 180.0f)
        pitchDeg -= 360.0f;

    float yawRad = DegToRad(yawDeg);
    float pitchRad = DegToRad(pitchDeg);

    Vec3 origin = players[shooter].pos;
    origin.y += cameraPosY;

    Vec3 dir;
    dir.x = std::cos(pitchRad) * std::sin(yawRad);
    dir.y = -std::sin(pitchRad);
    dir.z = std::cos(pitchRad) * std::cos(yawRad);

    float len = std::sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
    if (len > 1e-6f)
    {
        float inv = 1.0f / len;
        dir.x *= inv;
        dir.y *= inv;
        dir.z *= inv;
    }

    int usedWeaponId = players[shooter].weaponId;

    float range = RangeByWeapon(usedWeaponId);
    int damage = DamageByWeapon(usedWeaponId);

    float wallDist = range;
    {
        RayHit hit = Map::Instance().RaycastOBB(origin, dir, range);
        if (hit.hit)
            wallDist = hit.t;
    }

    float bestT = wallDist;
    int victim = -1;
    Vec3 hitPoint{ origin.x + dir.x * bestT, origin.y + dir.y * bestT, origin.z + dir.z * bestT };

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (i == shooter) continue;
        if (players[i].sessionId == 0) continue;
        if (players[i].hp <= 0) continue;

        const float HIT_Y0 = 0.2f;
        const float HIT_Y1 = 1.8f;
        const float HIT_R = 0.35f;

        Vec3 pa = players[i].pos;
        Vec3 pb = players[i].pos;
        pa.y += HIT_Y0;
        pb.y += HIT_Y1;

        float t = 0.0f;
        if (RayVsCapsule(origin, dir, pa, pb, HIT_R, bestT, t))
        {
            bestT = t;
            victim = i;
            hitPoint = { origin.x + dir.x * t, origin.y + dir.y * t, origin.z + dir.z * t };
        }
    }

    if (victim >= 0)
    {
        int prevHp = players[victim].hp;

        players[victim].hp -= damage;
        if (players[victim].hp < 0) players[victim].hp = 0;

        res.hit = 1;
        res.victimSessionId = players[victim].sessionId;
        res.hitX = hitPoint.x;
        res.hitY = hitPoint.y;
        res.hitZ = hitPoint.z;
        res.damage = damage;
        res.victimHp = players[victim].hp;

        if (prevHp > 0 && players[victim].hp == 0)
        {
            uint64_t sid = players[victim].sessionId;
            if (sid != 0 && deathCount < MAX_PLAYERS && !AlreadyInDeathOrder(deathOrder, deathCount, sid))
                deathOrder[deathCount++] = sid;

            {
                std::ostringstream oss;
                oss << "Kill gameId=" << gameId
                    << " shooterSid=" << (unsigned long long)players[shooter].sessionId
                    << " victimSid=" << (unsigned long long)players[victim].sessionId
                    << " shotId=" << shotId
                    << " weaponId=" << usedWeaponId
                    << " damage=" << damage;
                Logger::Info(LogTag::PLAYERSHOOT, oss.str());
            }
        }

        int alive = 0;
        uint64_t lastAliveSid = 0;
        for (int i = 0; i < MAX_PLAYERS; ++i)
        {
            if (players[i].sessionId == 0) continue;
            if (players[i].hp <= 0) continue;

            alive++;
            lastAliveSid = players[i].sessionId;
        }

        if (alive <= 1 && !finished)
        {
            finished = true;

            uint64_t rankSid[3] = { 0, 0, 0 };
            BuildRankSid3(rankSid, alive, lastAliveSid, deathOrder, deathCount);


            BroadcastShotResult(res);
            BroadcastGameOverRank3(rankSid);

            {
                std::ostringstream oss;
                oss << "Game finished by combat gameId=" << gameId
                    << " rank0=" << (unsigned long long)rankSid[0]
                    << " rank1=" << (unsigned long long)rankSid[1]
                    << " rank2=" << (unsigned long long)rankSid[2];
                Logger::Info(LogTag::GAMEOVER, oss.str());
            }
            return;
        }
    }

    BroadcastShotResult(res);
}

void Game::BuildRankSid3(uint64_t outRankSid[3], int alive, uint64_t lastAliveSid, const uint64_t* deathOrder, int deathCount)
{
    outRankSid[0] = 0;
    outRankSid[1] = 0;
    outRankSid[2] = 0;

    if (alive == 1)
    {
        outRankSid[0] = lastAliveSid;
        if (deathCount >= 1) outRankSid[1] = deathOrder[deathCount - 1];
        if (deathCount >= 2) outRankSid[2] = deathOrder[deathCount - 2];
        return;
    }

    // alive == 0: 전원 죽었거나 마지막 생존자가 끊긴 케이스
    if (deathCount >= 1) outRankSid[0] = deathOrder[deathCount - 1];
    if (deathCount >= 2) outRankSid[1] = deathOrder[deathCount - 2];
    if (deathCount >= 3) outRankSid[2] = deathOrder[deathCount - 3];
}

void Game::SolvePlayerCollisions()
{
    const float r = 0.5f;
    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (players[i].sessionId == 0) continue;
        if (players[i].hp <= 0) continue;

        for (int j = i + 1; j < MAX_PLAYERS; ++j)
        {
            if (players[j].sessionId == 0) continue;
            if (players[j].hp <= 0) continue;

            float dx = players[j].pos.x - players[i].pos.x;
            float dz = players[j].pos.z - players[i].pos.z;
            float dist2 = dx * dx + dz * dz;
            float minD = r + r;

            if (dist2 < minD * minD && dist2 > 1e-6f)
            {
                float dist = std::sqrt(dist2);
                float push = (minD - dist) * 0.5f;
                float nx = dx / dist;
                float nz = dz / dist;

                players[i].pos.x -= nx * push;
                players[i].pos.z -= nz * push;
                players[j].pos.x += nx * push;
                players[j].pos.z += nz * push;

                Map::Instance().ClampXZ(players[i].pos);
                Map::Instance().ResolveCircleXZ(players[i].pos, 0.5f);

                Map::Instance().ClampXZ(players[j].pos);
                Map::Instance().ResolveCircleXZ(players[j].pos, 0.5f);
            }
        }
    }
}

void Game::Update(float dt)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (finished) return;

    serverTick++;

    const float speed = 6.0f;
    const float radius = 0.5f;

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (players[i].sessionId == 0) continue;
        if (players[i].hp <= 0) continue;

        const InputState& in = players[i].lastInput;

        float yawDeg = players[i].yaw;
        float yawRad = DegToRad(yawDeg);

        float fx = std::sin(yawRad);
        float fz = std::cos(yawRad);

        float rx = std::cos(yawRad);
        float rz = -std::sin(yawRad);

        float mx = in.moveX;
        float mz = in.moveZ;

        float vx = rx * mx + fx * mz;
        float vz = rz * mx + fz * mz;

        float len2 = vx * vx + vz * vz;
        if (len2 > 1.0f)
        {
            float inv = 1.0f / std::sqrt(len2);
            vx *= inv;
            vz *= inv;
        }

        float dx = vx * speed * dt;
        float dz = vz * speed * dt;

        float dist = std::sqrt(dx * dx + dz * dz);

        int steps = (int)std::ceil(dist / 0.05f);
        if (steps < 1) steps = 1;
        if (steps > 10) steps = 10;

        float sx = dx / (float)steps;
        float sz = dz / (float)steps;

        for (int s = 0; s < steps; ++s)
        {
            players[i].pos.x += sx;
            players[i].pos.z += sz;

            Map::Instance().ClampXZ(players[i].pos);
            Map::Instance().ResolveCircleXZ(players[i].pos, radius);
        }
    }

    SolvePlayerCollisions();
    BroadcastState();
}

static void WriteFixedNickFlat(char* flat, int index, const char* nick)
{
    char* dst = flat + index * MAX_NICK_LEN;
    std::memset(dst, 0, MAX_NICK_LEN);

    if (!nick) return;

#if defined(_MSC_VER)
    strncpy_s(dst, MAX_NICK_LEN, nick, _TRUNCATE);
#else
    std::strncpy(dst, nick, MAX_NICK_LEN - 1);
    dst[MAX_NICK_LEN - 1] = '\0';
#endif
}

void Game::BroadcastGameOverRank3(const uint64_t rankSid[3])
{
    if (gameOverSent) return;
    gameOverSent = true;

    int winnerPlayerId = 0;
    if (rankSid[0] != 0)
    {
        int widx = FindIndexBySession(rankSid[0]);
        if (widx >= 0) winnerPlayerId = players[widx].playerId;
    }

    if (matchId != 0)
    {
        DbRankRow rows[3]{};
        int rowCount = 0;

        for (int r = 0; r < 3; ++r)
        {
            int idx = FindIndexBySession(rankSid[r]);
            if (idx < 0) continue;

            rows[rowCount].playerId = players[idx].playerId;
            rows[rowCount].characterId = players[idx].characterId;
            rows[rowCount].rank = r + 1;
            rowCount++;
        }

        if (rowCount > 0)
        {
            bool ok = DBManager::Instance().FinalizeMatchRank3(matchId, rows, rowCount);
            if (!ok)
            {
                std::ostringstream oss;
                oss << "FinalizeMatchRank3 failed matchId=" << (unsigned long long)matchId
                    << " gameId=" << gameId
                    << " rowCount=" << rowCount;
                Logger::Error(LogTag::SYSTEMERROR, oss.str(), false);
            }
        }

        matchId = 0;
    }

    GameOverPacket pkt{};
    pkt.header.size = (uint16_t)sizeof(GameOverPacket);
    pkt.header.type = (uint16_t)S_GAME_OVER;
    pkt.gameId = gameId;

    pkt.rankCount = 3;

    std::memset(pkt.rankSessionIds, 0, sizeof(pkt.rankSessionIds));
    std::memset(pkt.rankCharacterIds, 0, sizeof(pkt.rankCharacterIds));
    std::memset(pkt.rankIconIds, 0, sizeof(pkt.rankIconIds));
    std::memset(pkt.rankNicknamesFlat, 0, sizeof(pkt.rankNicknamesFlat));

    for (int r = 0; r < 3; ++r)
    {
        uint64_t sid = rankSid[r];
        pkt.rankSessionIds[r] = sid;

        int idx = FindIndexBySession(sid);
        if (idx >= 0)
        {
            pkt.rankCharacterIds[r] = players[idx].characterId;
            pkt.rankIconIds[r] = players[idx].iconId;

            WriteFixedNickFlat(pkt.rankNicknamesFlat, r, players[idx].nickname);
        }
        else
        {
            pkt.rankCharacterIds[r] = 0;
            pkt.rankIconIds[r] = 0;
            WriteFixedNickFlat(pkt.rankNicknamesFlat, r, "");
        }
    }

    auto buf = makePacket(pkt);
    auto& net = IOCP_EchoServer::Instance();

    for (auto s : sessions)
    {
        if (!s) continue;
        if (s->closing) continue;
        net.EnqueueSend(s, buf.data(), buf.size());
    }

    for (auto s : sessions)
    {
        if (!s) continue;
        if (s->closing) continue;

        s->totalGameCount += 1;
        if (winnerPlayerId != 0 && s->playerId == winnerPlayerId)
            s->winCount += 1;

        s->gameId = -1;
        s->gameCharacterId = -1;
        s->pendingRoomId = -1;
        s->weaponId = 0;
    }

    {
        std::ostringstream oss;
        oss << "GameOver sent gameId=" << gameId
            << " winnerPlayerId=" << winnerPlayerId;
        Logger::Info(LogTag::GAMEOVER, oss.str());
    }
}

void Game::BroadcastState()
{
    ServerGameStatePacket pkt{};
    pkt.header.size = sizeof(pkt);
    pkt.header.type = S_GAME_STATE;
    pkt.gameId = gameId;
    pkt.serverTick = serverTick;
    pkt.playerCount = 0;

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (players[i].sessionId == 0) continue;

        PlayerState3D ps{};
        ps.sessionId = players[i].sessionId;
        ps.x = players[i].pos.x;
        ps.y = players[i].pos.y;
        ps.z = players[i].pos.z;
        ps.yaw = players[i].yaw;
        ps.pitch = players[i].pitch;
        ps.hp = players[i].hp;
        ps.weaponId = players[i].weaponId;
        ps.lastAckTick = players[i].lastAckTick;

        pkt.players[pkt.playerCount++] = ps;
    }

    auto buf = makePacket(pkt);
    auto& net = IOCP_EchoServer::Instance();

    for (auto s : sessions)
    {
        if (!s) continue;
        if (s->closing) continue;
        net.EnqueueSend(s, buf.data(), buf.size());
    }
}

void Game::BroadcastShotResult(const ServerShotResultPacket& pkt)
{
    auto buf = makePacket(pkt);
    auto& net = IOCP_EchoServer::Instance();

    for (auto s : sessions)
    {
        if (!s) continue;
        if (s->closing) continue;
        net.EnqueueSend(s, buf.data(), buf.size());
    }
}

void Game::InitWeaponStats()
{
    for (int i = 0; i <= MAX_WEAPONS; ++i)
    {
        weaponStats[i].attackPower = 20;
        weaponStats[i].range = 60.0f;
        weaponStats[i].valid = false;
    }

    std::vector<DbWeaponData> list;
    if (!DBManager::Instance().LoadWeapons(list))
    {
        std::ostringstream oss;
        oss << "LoadWeapons failed, using default weapon stats gameId=" << gameId;
        Logger::Warning(LogTag::SYSTEMERROR, oss.str());
        return;
    }

    for (const auto& w : list)
    {
        if (w.weaponId <= 0 || w.weaponId > MAX_WEAPONS) continue;

        weaponStats[w.weaponId].attackPower = (w.attackPower > 0) ? w.attackPower : 20;
        weaponStats[w.weaponId].range = (w.range > 0.0f) ? w.range : 60.0f;
        weaponStats[w.weaponId].valid = true;
    }

    {
        std::ostringstream oss;
        oss << "Weapon stats loaded gameId=" << gameId << " count=" << list.size();
        Logger::Debug(LogTag::GAMECREATE, oss.str());
    }
}

int Game::DamageByWeapon(int weaponId) const
{
    if (weaponId <= 0 || weaponId > MAX_WEAPONS) return 20;
    return weaponStats[weaponId].attackPower;
}

float Game::RangeByWeapon(int weaponId) const
{
    if (weaponId <= 0 || weaponId > MAX_WEAPONS) return 60.0f;
    return weaponStats[weaponId].range;
}