#pragma once
#include <vector>
#include <mutex>
#include <unordered_map>
#include "IOCP_EchoServer.h"

class MatchManager
{
public:
    static MatchManager& Instance();

    void TryMatch(Session* session);

    void CancelMatch(Session* session);

    void OnSetCharacter(Session* session, int characterId);

private:
    MatchManager() = default;

    struct PendingRoom
    {
        int id = 0;
        std::vector<Session*> members;
    };

    void PruneWaiting_NoLock();
    void BroadcastQueueSize_NoLock();
    void CancelRoom_NoLock(int roomId);

private:
    std::vector<Session*> waiting_;
    std::unordered_map<int, PendingRoom> rooms_;
    int nextRoomId_ = 1;
    std::mutex mtx_;
};