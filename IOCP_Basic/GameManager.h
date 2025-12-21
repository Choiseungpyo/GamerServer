#pragma once
#include <unordered_map>
#include <mutex>
#include <thread>
#include <atomic>
#include <memory>
#include <vector>
#include "IOCP_EchoServer.h"
#include "Packet.h"

class Game;

class GameManager
{
public:
    static GameManager& Instance();

    int CreateGame(const std::vector<Session*>& players);
    Game* Find(int id);

    void StartLoop();
    void StopLoop();

    void OnInputPacket(Session* session, const GameInputPacket* pkt);
    void OnFirePacket(Session* session, const GameFirePacket* pkt);

    void OnDisconnect(Session* session);

private:
    GameManager() = default;
    void Loop();

private:
    std::unordered_map<int, std::unique_ptr<Game>> games;
    std::unordered_map<uint64_t, int> userToGame;
    std::mutex mtx;

    std::atomic<bool> running{ false };
    std::thread loopThread;
    int nextGameId = 1;
};