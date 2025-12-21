#include "MatchManager.h"
#include "Packet.h"
#include "IOCP_EchoServer.h"
#include "GameManager.h"
#include "DBManager.h"
#include "Logger.h"
#include <algorithm>
#include <sstream>

MatchManager& MatchManager::Instance()
{
    static MatchManager inst;
    return inst;
}

void MatchManager::BroadcastQueueSize_NoLock()
{
    PruneWaiting_NoLock();

    ServerMatchWaitPacket pkt{};
    pkt.header.size = sizeof(pkt);
    pkt.header.type = S_MATCH_WAIT;
    pkt.queueSize = (int32_t)waiting_.size();

    auto buf = makePacket(pkt);
    auto& net = IOCP_EchoServer::Instance();

    for (auto s : waiting_)
    {
        if (!s) continue;
        if (s->closing) continue;
        net.EnqueueSend(s, buf.data(), buf.size());
    }
}

void MatchManager::CancelRoom_NoLock(int roomId)
{
    auto it = rooms_.find(roomId);
    if (it == rooms_.end()) return;

    auto members = it->second.members;
    rooms_.erase(it);

    {
        std::ostringstream oss;
        oss << "대기룸 해제 roomId=" << roomId << " memberCount=" << (int)members.size();
        Logger::Instance().Write(LogTag::MATCHCANCEL, oss.str());
    }

    for (auto s : members)
    {
        if (!s) continue;
        s->pendingRoomId = -1;
        s->gameCharacterId = -1;
        s->weaponId = 0;
    }
}

void MatchManager::TryMatch(Session* session)
{
    if (!session) return;
    if (!session->authed) return;
    if (session->closing) return;
    if (session->gameId >= 0) return;
    if (session->pendingRoomId >= 0) return;

    std::lock_guard<std::mutex> lock(mtx_);

    PruneWaiting_NoLock();

    if (std::find(waiting_.begin(), waiting_.end(), session) == waiting_.end())
    {
        waiting_.push_back(session);

        std::ostringstream oss;
        oss << "매칭 대기열 추가 sid=" << (unsigned long long)session->id
            << " queueSize=" << (int)waiting_.size();
        Logger::Instance().Write(LogTag::MATCHQUEUE, oss.str());
    }

    BroadcastQueueSize_NoLock();

    if ((int)waiting_.size() < MAX_PLAYERS)
        return;

    PendingRoom room{};
    room.id = nextRoomId_++;

    for (int i = 0; i < MAX_PLAYERS; ++i)
        room.members.push_back(waiting_[i]);

    waiting_.erase(waiting_.begin(), waiting_.begin() + MAX_PLAYERS);

    bool hasBad = false;
    for (auto s : room.members)
    {
        if (!s || s->closing) { hasBad = true; break; }
    }

    if (hasBad)
    {
        {
            std::ostringstream oss;
            oss << "대기룸 구성 실패(세션 이상) roomId=" << room.id;
            Logger::Instance().Write(LogTag::MATCH, oss.str());
        }

        for (auto s : room.members)
        {
            if (!s) continue;
            if (s->closing) continue;
            if (std::find(waiting_.begin(), waiting_.end(), s) == waiting_.end())
                waiting_.push_back(s);
        }
        BroadcastQueueSize_NoLock();
        return;
    }

    rooms_[room.id] = room;

    {
        std::ostringstream oss;
        oss << "대기룸 생성 roomId=" << room.id << " memberCount=" << (int)room.members.size();
        Logger::Instance().Write(LogTag::MATCH, oss.str());
    }

    for (auto s : room.members)
    {
        s->pendingRoomId = room.id;
        s->gameCharacterId = -1;
        s->weaponId = 0;

        ServerMatchWaitPacket ready{};
        ready.header.size = sizeof(ready);
        ready.header.type = S_MATCH_WAIT;
        ready.queueSize = MAX_PLAYERS;

        auto buf = makePacket(ready);
        IOCP_EchoServer::Instance().EnqueueSend(s, buf.data(), buf.size());
    }

    BroadcastQueueSize_NoLock();
}

void MatchManager::CancelMatch(Session* session)
{
    if (!session) return;

    std::lock_guard<std::mutex> lock(mtx_);

    if (session->pendingRoomId >= 0)
    {
        int rid = session->pendingRoomId;

        {
            std::ostringstream oss;
            oss << "매칭 취소(대기룸) sid=" << (unsigned long long)session->id << " roomId=" << rid;
            Logger::Instance().Write(LogTag::MATCHCANCEL, oss.str());
        }

        CancelRoom_NoLock(rid);
        return;
    }

    auto it = std::remove(waiting_.begin(), waiting_.end(), session);
    bool removed = (it != waiting_.end());
    waiting_.erase(it, waiting_.end());

    if (removed)
    {
        std::ostringstream oss;
        oss << "매칭 취소(대기열) sid=" << (unsigned long long)session->id
            << " queueSize=" << (int)waiting_.size();
        Logger::Instance().Write(LogTag::MATCHCANCEL, oss.str());

        BroadcastQueueSize_NoLock();
    }
}

void MatchManager::OnSetCharacter(Session* session, int characterId)
{
    if (!session) return;

    std::lock_guard<std::mutex> lock(mtx_);

    ServerSetCharacterPacket res{};
    res.header.size = sizeof(res);
    res.header.type = S_SET_CHARACTER;
    res.ok = 0;
    res.currentCharacterId = -1;

    if (session->pendingRoomId < 0)
    {
        Logger::Instance().Write(LogTag::MATCH, "캐릭터 선택 실패: pendingRoomId 없음");
        auto buf = makePacket(res);
        IOCP_EchoServer::Instance().EnqueueSend(session, buf.data(), buf.size());
        return;
    }

    if (!DBManager::Instance().IsValidCharacter(characterId))
    {
        {
            std::ostringstream oss;
            oss << "캐릭터 선택 실패: 유효하지 않은 characterId=" << characterId
                << " sid=" << (unsigned long long)session->id;
            Logger::Instance().Write(LogTag::MATCH, oss.str());
        }

        auto buf = makePacket(res);
        IOCP_EchoServer::Instance().EnqueueSend(session, buf.data(), buf.size());
        return;
    }

    auto it = rooms_.find(session->pendingRoomId);
    if (it == rooms_.end())
    {
        {
            std::ostringstream oss;
            oss << "캐릭터 선택 실패: 룸 없음 roomId=" << session->pendingRoomId
                << " sid=" << (unsigned long long)session->id;
            Logger::Instance().Write(LogTag::MATCH, oss.str());
        }

        session->pendingRoomId = -1;
        auto buf = makePacket(res);
        IOCP_EchoServer::Instance().EnqueueSend(session, buf.data(), buf.size());
        return;
    }

    session->gameCharacterId = characterId;

    int defaultWeaponId = 0;
    if (!DBManager::Instance().GetDefaultWeaponId(characterId, defaultWeaponId) || defaultWeaponId <= 0)
        defaultWeaponId = 1;

    if (!DBManager::Instance().IsValidWeapon(defaultWeaponId))
        defaultWeaponId = 1;

    session->weaponId = defaultWeaponId;

    res.ok = 1;
    res.currentCharacterId = characterId;

    {
        std::ostringstream oss;
        oss << "캐릭터 선택 완료 sid=" << (unsigned long long)session->id
            << " roomId=" << session->pendingRoomId
            << " characterId=" << characterId
            << " weaponId=" << defaultWeaponId;
        Logger::Instance().Write(LogTag::MATCH, oss.str());
    }

    {
        auto buf = makePacket(res);
        IOCP_EchoServer::Instance().EnqueueSend(session, buf.data(), buf.size());
    }

    bool allReady = true;
    for (auto s : it->second.members)
    {
        if (!s || s->closing) { allReady = false; break; }
        if (s->gameCharacterId < 0) { allReady = false; break; }
        if (s->weaponId <= 0) { allReady = false; break; }
    }
    if (!allReady) return;

    auto members = it->second.members;
    int roomId = it->second.id;
    rooms_.erase(roomId);

    {
        std::ostringstream oss;
        oss << "매칭 성립 roomId=" << roomId << " memberCount=" << (int)members.size();
        Logger::Instance().Write(LogTag::MATCH, oss.str());
    }

    for (auto s : members)
    {
        if (!s) continue;
        s->pendingRoomId = -1;
    }

    int gameId = GameManager::Instance().CreateGame(members);

    {
        std::ostringstream oss;
        oss << "게임 생성 요청 완료 roomId=" << roomId << " gameId=" << gameId;
        Logger::Instance().Write(LogTag::GAMECREATE, oss.str());
    }

    GameStartPacket startPkt{};
    startPkt.header.size = sizeof(startPkt);
    startPkt.header.type = S_GAME_START;
    startPkt.gameId = gameId;
    startPkt.playerCount = (int32_t)members.size();

    std::vector<float> spx{ 0.0f, 0.0f, -8.0f };
    std::vector<float> spy{ 0.0f, 0.0f, 0.0f };
    std::vector<float> spz{ 13.0f, -13.0f, 0.0f };

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        startPkt.sessionIds[i] = 0;
        startPkt.spawnX[i] = 0.0f;
        startPkt.spawnY[i] = 0.0f;
        startPkt.spawnZ[i] = 0.0f;
        startPkt.characterIds[i] = -1;
        startPkt.weaponIds[i] = 0;
    }

    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (i >= (int)members.size()) break;
        if (!members[i]) continue;

        startPkt.sessionIds[i] = members[i]->id;
        startPkt.spawnX[i] = spx[i];
        startPkt.spawnY[i] = spy[i];
        startPkt.spawnZ[i] = spz[i];
        startPkt.characterIds[i] = members[i]->gameCharacterId;
        startPkt.weaponIds[i] = members[i]->weaponId;
    }

    auto& net = IOCP_EchoServer::Instance();
    for (int i = 0; i < MAX_PLAYERS; ++i)
    {
        if (i >= (int)members.size()) break;
        if (!members[i]) continue;

        GameStartPacket pkt = startPkt;
        pkt.selfIndex = i;
        auto buf = makePacket(pkt);
        net.EnqueueSend(members[i], buf.data(), buf.size());
    }
}

void MatchManager::PruneWaiting_NoLock()
{
    waiting_.erase(
        std::remove_if(waiting_.begin(), waiting_.end(),
            [](Session* s)
            {
                if (!s) return true;
                if (s->closing) return true;
                if (!s->authed) return true;
                if (s->gameId >= 0) return true;
                if (s->pendingRoomId >= 0) return true;
                return false;
            }),
        waiting_.end());
}