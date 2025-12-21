#include "GameManager.h"
#include "Game.h"
#include "DBManager.h"
#include "Logger.h"
#include <sstream>

GameManager& GameManager::Instance()
{
    static GameManager inst;
    return inst;
}

int GameManager::CreateGame(const std::vector<Session*>& players)
{
    std::lock_guard<std::mutex> lock(mtx);

    int gameId = nextGameId++;

    uint64_t matchId = 0;
    if (!DBManager::Instance().CreateMatch(matchId))
    {
        matchId = 0;

        std::ostringstream oss;
        oss << "CreateMatch failed gameId=" << gameId;
        Logger::Warning(LogTag::SYSTEMERROR, oss.str());
    }

    games[gameId] = std::make_unique<Game>(gameId, matchId, players);

    for (auto s : players)
    {
        if (!s) continue;
        s->gameId = gameId;
        userToGame[s->id] = gameId;
    }

    {
        std::ostringstream oss;
        oss << "CreateGame gameId=" << gameId
            << " matchId=" << (unsigned long long)matchId
            << " playerCount=" << players.size();

        int idx = 0;
        for (auto s : players)
        {
            if (!s) continue;
            oss << " p" << idx
                << "(sid=" << (unsigned long long)s->id
                << " pid=" << s->playerId
                << " cid=" << s->gameCharacterId
                << " wid=" << s->weaponId
                << ")";
            idx++;
        }

        Logger::Info(LogTag::GAMECREATE, oss.str());
    }

    return gameId;
}

Game* GameManager::Find(int id)
{
    auto it = games.find(id);
    if (it == games.end()) return nullptr;
    return it->second.get();
}

void GameManager::StartLoop()
{
    if (running.exchange(true))
        return;

    Logger::Info(LogTag::GAMECOUNT, "Game loop start");

    loopThread = std::thread(&GameManager::Loop, this);
}

void GameManager::StopLoop()
{
    if (!running.exchange(false))
        return;

    Logger::Info(LogTag::GAMECOUNT, "Game loop stop");

    if (loopThread.joinable())
        loopThread.join();
}

void GameManager::Loop()
{
    const float dt = 0.02f;

    while (running)
    {
        {
            std::lock_guard<std::mutex> lock(mtx);
            for (auto it = games.begin(); it != games.end(); )
            {
                auto& g = it->second;
                if (g->IsFinished())
                {
                    int gid = it->first;

                    for (auto u = userToGame.begin(); u != userToGame.end(); )
                    {
                        if (u->second == gid) u = userToGame.erase(u);
                        else ++u;
                    }

                    it = games.erase(it);

                    std::ostringstream oss;
                    oss << "RemoveGame gameId=" << gid << " remain=" << games.size();
                    Logger::Info(LogTag::GAMEREMOVE, oss.str());
                }
                else
                {
                    g->Update(dt);
                    ++it;
                }
            }
        }

        Sleep(20);
    }
}

void GameManager::OnInputPacket(Session* session, const GameInputPacket* pkt)
{
    if (!session || !pkt)
    {
        Logger::Warning(LogTag::SYSTEMERROR, "OnInputPacket invalid args");
        return;
    }

    std::lock_guard<std::mutex> lock(mtx);
    Game* g = Find(pkt->gameId);
    if (!g)
    {
        std::ostringstream oss;
        oss << "OnInputPacket game not found gameId=" << pkt->gameId
            << " sid=" << (unsigned long long)session->id;
        Logger::Debug(LogTag::PLAYERMOVE, oss.str());
        return;
    }

    InputState in{};
    in.tick = pkt->tick;
    in.moveX = pkt->moveX;
    in.moveZ = pkt->moveZ;
    in.yaw = pkt->yaw;
    in.pitch = pkt->pitch;
    in.buttons = pkt->buttons;
    in.weaponId = pkt->weaponId;

    g->OnInput(session, in);
}

void GameManager::OnFirePacket(Session* session, const GameFirePacket* pkt)
{
    if (!session || !pkt)
    {
        Logger::Warning(LogTag::SYSTEMERROR, "OnFirePacket invalid args");
        return;
    }

    std::lock_guard<std::mutex> lock(mtx);
    Game* g = Find(pkt->gameId);
    if (!g)
    {
        std::ostringstream oss;
        oss << "OnFirePacket game not found gameId=" << pkt->gameId
            << " sid=" << (unsigned long long)session->id
            << " shotId=" << pkt->shotId;
        Logger::Debug(LogTag::PLAYERSHOOT, oss.str());
        return;
    }

    g->OnFire(session, pkt->shotId, pkt->clientTick, pkt->weaponId);
}

void GameManager::OnDisconnect(Session* session)
{
    if (!session)
    {
        Logger::Warning(LogTag::SYSTEMERROR, "OnDisconnect session is null");
        return;
    }

    std::lock_guard<std::mutex> lock(mtx);
    auto it = userToGame.find(session->id);
    if (it == userToGame.end())
        return;

    int gid = it->second;
    Game* g = Find(gid);
    if (g)
        g->OnDisconnect(session);
}