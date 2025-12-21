#pragma once
#include <winsock2.h>
#include <mswsock.h>
#include <windows.h>
#include <vector>
#include <queue>
#include <mutex>
#include <atomic>
#include <unordered_map>
#include <thread>
#include <string>
#include "Packet.h"

#pragma comment(lib, "ws2_32.lib")

static constexpr int BUFFER_SIZE = 8192;

struct OverlappedEx : public OVERLAPPED
{
    enum { OP_ACCEPT, OP_RECV, OP_SEND } op;
    void* session;
    WSABUF wsaBuf;
    SOCKET acceptSock;

    OverlappedEx()
        : op(OP_ACCEPT), session(nullptr), acceptSock(INVALID_SOCKET)
    {
        ZeroMemory(this, sizeof(OVERLAPPED));
        wsaBuf.buf = nullptr;
        wsaBuf.len = 0;
    }
};

struct Session
{
    SOCKET sock = INVALID_SOCKET;

    char* rxBuf = nullptr;
    size_t rxUsed = 0;

    std::queue<std::vector<char>> sendQueue;
    std::vector<char> sendingBytes;
    std::mutex sendMtx;

    OverlappedEx ovRecv;
    OverlappedEx ovSend;

    std::atomic<bool> sending{ false };
    std::atomic<bool> closing{ false };

    uint64_t id = 0;
    int gameId = -1;

    bool authed = false;

    int playerId = 0;
    int iconId = 0;
    int totalGameCount = 0;
    int winCount = 0;

    char nickname[MAX_NICK_LEN]{ 0 };

    int gameCharacterId = -1; // 캐릭터 선택에서 사용
    int weaponId = 0;
    int pendingRoomId = -1;
};

class BufferPool
{
public:
    BufferPool(size_t blockSize, size_t initialCount);
    ~BufferPool();

    char* Allocate();
    void Release(char* buf);

private:
    std::mutex mtx_;
    std::vector<char*> pool_;
    size_t blockSize_;
};

class IOCP_EchoServer
{
public:
    static IOCP_EchoServer& Instance();

    IOCP_EchoServer(unsigned short port = 7777, int workerCount = 0, int acceptCount = 0);
    ~IOCP_EchoServer();

    bool Start();
    void Stop();

    void EnqueueSend(Session* session, const char* data, size_t len);
    std::vector<std::pair<uint64_t, Session*>> GetAllSessions();

private:
    void LoadExtensionFunctions();
    void PostAccept();
    void AcceptCompletion(OverlappedEx* ov, DWORD bytes);

    void PostRecv(Session* session);
    void RecvCompletion(Session* session, DWORD bytes);

    void PostSend(Session* session);
    void SendCompletion(Session* session, DWORD bytes);

    void CloseSession(Session* session);

    void WorkerThread();

private:
    unsigned short port_;
    SOCKET listenSock_ = INVALID_SOCKET;
    HANDLE iocpHandle_ = NULL;

    LPFN_ACCEPTEX fnAcceptEx_ = nullptr;
    LPFN_GETACCEPTEXSOCKADDRS fnGetAcceptExSockaddrs_ = nullptr;

    std::atomic<bool> running_{ false };
    std::vector<std::thread> workers_;

    BufferPool bufferPool_{ BUFFER_SIZE, 128 };

    std::unordered_map<uint64_t, Session*> sessionTable_;
    std::mutex sessionTableMtx_;
    std::atomic<uint64_t> sessionIdGen_{ 1 };

    int acceptOutstanding_ = 8;
    int workerCount_ = 0;
};