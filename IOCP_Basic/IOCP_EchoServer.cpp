#include "IOCP_EchoServer.h"
#include "Packet.h"
#include "MatchManager.h"
#include "GameManager.h"
#include "LoginManager.h"
#include "Util.h"
#include "DBManager.h"
#include "Logger.h"
#include <algorithm>
#include <sstream>
#include "Map.h"

BufferPool::BufferPool(size_t blockSize, size_t initialCount)
    : blockSize_(blockSize)
{
    for (size_t i = 0; i < initialCount; ++i)
        pool_.push_back(new char[blockSize_]);
}

BufferPool::~BufferPool()
{
    for (auto p : pool_)
        delete[] p;
    pool_.clear();
}

char* BufferPool::Allocate()
{
    std::lock_guard<std::mutex> lock(mtx_);
    if (!pool_.empty())
    {
        char* p = pool_.back();
        pool_.pop_back();
        return p;
    }
    return new char[blockSize_];
}

void BufferPool::Release(char* buf)
{
    if (!buf) return;
    std::lock_guard<std::mutex> lock(mtx_);
    pool_.push_back(buf);
}

IOCP_EchoServer& IOCP_EchoServer::Instance()
{
    static IOCP_EchoServer inst;
    return inst;
}

IOCP_EchoServer::IOCP_EchoServer(unsigned short port, int workerCount, int acceptCount)
    : port_(port)
{
    SYSTEM_INFO si{};
    GetSystemInfo(&si);
    workerCount_ = (workerCount > 0) ? workerCount : (int)si.dwNumberOfProcessors;
    acceptOutstanding_ = (acceptCount > 0) ? acceptCount : 8;
}

IOCP_EchoServer::~IOCP_EchoServer()
{
    Stop();
}

bool IOCP_EchoServer::Start()
{
    if (running_.exchange(true))
        return false;

    Logger::Instance().Write(LogTag::SERVERSTART, "서버 시작 시도");

    Map::Instance().SetWorldBounds(20.36f, 30.36f);

    {
        bool ok = Map::Instance().LoadOBBsFromBinary("./map_obb.bin");
        if (!ok)
        {
            Logger::Instance().Write(LogTag::SYSTEMERROR, "map_obb.bin 로드 실패");
            running_ = false;
            return false;
        }
        Logger::Instance().Write(LogTag::SERVERSTART, "map_obb.bin 로드 성공");
    }

    WSADATA wsa{};
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
    {
        Logger::Instance().Write(LogTag::SYSTEMERROR, "WSAStartup 실패");
        running_ = false;
        return false;
    }

    listenSock_ = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (listenSock_ == INVALID_SOCKET)
    {
        std::ostringstream oss;
        oss << "리스닝 소켓 생성 실패 wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        Stop();
        return false;
    }

    BOOL opt = TRUE;
    setsockopt(listenSock_, IPPROTO_TCP, TCP_NODELAY, (char*)&opt, sizeof(opt));

    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port = htons(port_);

    if (bind(listenSock_, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR)
    {
        std::ostringstream oss;
        oss << "bind 실패 wsaErr=" << WSAGetLastError() << " port=" << port_;
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        Stop();
        return false;
    }

    if (listen(listenSock_, SOMAXCONN) == SOCKET_ERROR)
    {
        std::ostringstream oss;
        oss << "listen 실패 wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        Stop();
        return false;
    }

    iocpHandle_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    if (!iocpHandle_)
    {
        std::ostringstream oss;
        oss << "IOCP 핸들 생성 실패 gle=" << GetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        Stop();
        return false;
    }

    if (!CreateIoCompletionPort((HANDLE)listenSock_, iocpHandle_, 0, 0))
    {
        std::ostringstream oss;
        oss << "IOCP 연결 실패(리스닝) gle=" << GetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        Stop();
        return false;
    }

    LoadExtensionFunctions();

    if (!fnAcceptEx_ || !fnGetAcceptExSockaddrs_)
    {
        Logger::Instance().Write(LogTag::SYSTEMERROR, "확장 함수 로드 실패(AcceptEx)");
        Stop();
        return false;
    }

    for (int i = 0; i < workerCount_; ++i)
        workers_.emplace_back(&IOCP_EchoServer::WorkerThread, this);

    for (int i = 0; i < acceptOutstanding_; ++i)
        PostAccept();

    GameManager::Instance().StartLoop();

    if (!DBManager::Instance().Initialize())
        Logger::Instance().Write(LogTag::SYSTEMERROR, "DB 초기화 실패");

    {
        std::ostringstream oss;
        oss << "서버 시작 완료 port=" << port_ << " worker=" << workerCount_ << " accept=" << acceptOutstanding_;
        Logger::Instance().Write(LogTag::SERVERSTART, oss.str());
    }

    return true;
}

void IOCP_EchoServer::Stop()
{
    if (!running_.exchange(false))
        return;

    Logger::Instance().Write(LogTag::SERVERSTOP, "서버 중지");

    GameManager::Instance().StopLoop();
    DBManager::Instance().Finalize();

    if (listenSock_ != INVALID_SOCKET)
    {
        closesocket(listenSock_);
        listenSock_ = INVALID_SOCKET;
    }

    if (iocpHandle_)
    {
        for (int i = 0; i < (int)workers_.size(); ++i)
            PostQueuedCompletionStatus(iocpHandle_, 0, 0, NULL);

        for (auto& t : workers_)
        {
            if (t.joinable())
                t.join();
        }
        workers_.clear();

        CloseHandle(iocpHandle_);
        iocpHandle_ = NULL;
    }

    {
        std::lock_guard<std::mutex> lock(sessionTableMtx_);
        for (auto& kv : sessionTable_)
            CloseSession(kv.second);
        sessionTable_.clear();
    }

    WSACleanup();
}

void IOCP_EchoServer::LoadExtensionFunctions()
{
    DWORD bytes = 0;

    GUID guidAcceptEx = WSAID_ACCEPTEX;
    int r1 = WSAIoctl(listenSock_, SIO_GET_EXTENSION_FUNCTION_POINTER,
        &guidAcceptEx, sizeof(guidAcceptEx),
        &fnAcceptEx_, sizeof(fnAcceptEx_),
        &bytes, NULL, NULL);

    if (r1 == SOCKET_ERROR)
    {
        std::ostringstream oss;
        oss << "WSAIoctl(AcceptEx) 실패 wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
    }

    GUID guidGetAcceptExSockaddrs = WSAID_GETACCEPTEXSOCKADDRS;
    int r2 = WSAIoctl(listenSock_, SIO_GET_EXTENSION_FUNCTION_POINTER,
        &guidGetAcceptExSockaddrs, sizeof(guidGetAcceptExSockaddrs),
        &fnGetAcceptExSockaddrs_, sizeof(fnGetAcceptExSockaddrs_),
        &bytes, NULL, NULL);

    if (r2 == SOCKET_ERROR)
    {
        std::ostringstream oss;
        oss << "WSAIoctl(GetAcceptExSockaddrs) 실패 wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
    }
}

void IOCP_EchoServer::PostAccept()
{
    if (!running_) return;

    OverlappedEx* ov = new OverlappedEx();
    ov->op = OverlappedEx::OP_ACCEPT;
    ov->session = nullptr;

    ov->acceptSock = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (ov->acceptSock == INVALID_SOCKET)
    {
        std::ostringstream oss;
        oss << "Accept 소켓 생성 실패 wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        delete ov;
        return;
    }

    static char acceptBuf[(sizeof(sockaddr_in) + 16) * 2] = { 0 };
    DWORD bytes = 0;

    BOOL ok = fnAcceptEx_(
        listenSock_,
        ov->acceptSock,
        acceptBuf,
        0,
        sizeof(sockaddr_in) + 16,
        sizeof(sockaddr_in) + 16,
        &bytes,
        ov
    );

    if (!ok && WSAGetLastError() != ERROR_IO_PENDING)
    {
        std::ostringstream oss;
        oss << "AcceptEx 실패 wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        closesocket(ov->acceptSock);
        delete ov;
        return;
    }
}

void IOCP_EchoServer::AcceptCompletion(OverlappedEx* ov, DWORD bytes)
{
    (void)bytes;

    Session* s = new Session();
    s->sock = ov->acceptSock;
    s->id = sessionIdGen_++;

    s->rxBuf = bufferPool_.Allocate();
    s->rxUsed = 0;

    if (!CreateIoCompletionPort((HANDLE)s->sock, iocpHandle_, (ULONG_PTR)s, 0))
    {
        std::ostringstream oss;
        oss << "IOCP 연결 실패(클라이언트) sid=" << (unsigned long long)s->id << " gle=" << GetLastError();
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        CloseSession(s);
        PostAccept();
        delete ov;
        return;
    }

    {
        std::lock_guard<std::mutex> lock(sessionTableMtx_);
        sessionTable_[s->id] = s;
    }

    {
        std::ostringstream oss;
        oss << "세션 접속 sid=" << (unsigned long long)s->id;
        Logger::Instance().Write(LogTag::ACCEPT, oss.str());
    }

    PostRecv(s);

    PostAccept();
    delete ov;
}

void IOCP_EchoServer::PostRecv(Session* session)
{
    if (!session || session->closing) return;

    ZeroMemory(&session->ovRecv, sizeof(OverlappedEx));
    session->ovRecv.op = OverlappedEx::OP_RECV;
    session->ovRecv.session = session;

    session->ovRecv.wsaBuf.buf = session->rxBuf + session->rxUsed;
    session->ovRecv.wsaBuf.len = (ULONG)(BUFFER_SIZE - session->rxUsed);

    DWORD flags = 0;
    DWORD recvBytes = 0;

    int ret = WSARecv(session->sock, &session->ovRecv.wsaBuf, 1, &recvBytes, &flags, &session->ovRecv, NULL);
    if (ret == SOCKET_ERROR && WSAGetLastError() != WSA_IO_PENDING)
    {
        std::ostringstream oss;
        oss << "WSARecv 실패 sid=" << (unsigned long long)session->id << " wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::RECV, oss.str());
        CloseSession(session);
    }
}

void IOCP_EchoServer::RecvCompletion(Session* session, DWORD bytes)
{
    if (!session) return;

    if (bytes == 0)
    {
        CloseSession(session);
        return;
    }

    session->rxUsed += bytes;

    while (session->rxUsed >= sizeof(PacketHeader))
    {
        PacketHeader* header = reinterpret_cast<PacketHeader*>(session->rxBuf);

        if (header->size < sizeof(PacketHeader))
        {
            std::ostringstream oss;
            oss << "패킷 크기 오류 sid=" << (unsigned long long)session->id
                << " type=" << header->type
                << " size=" << header->size;
            Logger::Instance().Write(LogTag::RECV, oss.str());
            CloseSession(session);
            return;
        }

        if (header->size > BUFFER_SIZE)
        {
            std::ostringstream oss;
            oss << "패킷 과대 오류 sid=" << (unsigned long long)session->id
                << " type=" << header->type
                << " size=" << header->size;
            Logger::Instance().Write(LogTag::RECV, oss.str());
            CloseSession(session);
            return;
        }

        if (session->rxUsed < header->size)
            break;

        bool closeNow = false;

        switch (header->type)
        {
        case C_LOGIN_REQ:
        {
            auto pkt = reinterpret_cast<LoginReqPacket*>(session->rxBuf);

            std::string uid = SafeString(pkt->userId, MAX_ID_LEN);
            std::string pw = SafeString(pkt->password, MAX_PW_LEN);

            if (session->authed)
            {
                LoginResPacket res{};
                res.header.size = sizeof(res);
                res.header.type = S_LOGIN_RES;
                res.ok = 0;
                res.playerId = 0;
                FillFixedString(res.nickname, MAX_NICK_LEN, "");
                res.totalGameCount = 0;
                res.winCount = 0;

                auto buf = makePacket(res);
                EnqueueSend(session, buf.data(), buf.size());

                std::ostringstream oss;
                oss << "로그인 요청 거부(이미 로그인) sid=" << (unsigned long long)session->id;
                Logger::Instance().Write(LogTag::RECV, oss.str());
                break;
            }

            auto r = LoginManager::Instance().Login(uid, pw);
            bool ok = r.ok;

            if (ok)
            {
                std::lock_guard<std::mutex> lock(sessionTableMtx_);
                for (auto& kv : sessionTable_)
                {
                    Session* other = kv.second;
                    if (!other) continue;
                    if (other == session) continue;

                    if (other->authed && other->playerId == r.playerId)
                    {
                        ok = false;
                        break;
                    }
                }
            }

            if (ok)
            {
                session->authed = true;
                session->playerId = r.playerId;
                session->iconId = r.iconId;
                FillFixedString(session->nickname, MAX_NICK_LEN, r.nickname);
                session->gameCharacterId = -1;
                session->pendingRoomId = -1;
                session->totalGameCount = r.total;
                session->winCount = r.win;

                std::ostringstream oss;
                oss << "로그인 성공 sid=" << (unsigned long long)session->id
                    << " uid=" << uid
                    << " playerId=" << r.playerId;
                Logger::Instance().Write(LogTag::RECV, oss.str());
            }
            else
            {
                session->authed = false;
                session->playerId = 0;
                std::memset(session->nickname, 0, sizeof(session->nickname));
                session->gameCharacterId = -1;
                session->pendingRoomId = -1;
                session->totalGameCount = -1;
                session->winCount = -1;

                std::ostringstream oss;
                oss << "로그인 실패 sid=" << (unsigned long long)session->id
                    << " uid=" << uid;
                Logger::Instance().Write(LogTag::RECV, oss.str());
            }

            LoginResPacket res{};
            res.header.size = sizeof(res);
            res.header.type = S_LOGIN_RES;
            res.ok = (uint8_t)(ok ? 1 : 0);
            res.playerId = ok ? r.playerId : 0;
            res.iconId = ok ? r.iconId : 0;

            FillFixedString(res.nickname, MAX_NICK_LEN, ok ? r.nickname : "");
            res.totalGameCount = ok ? r.total : 0;
            res.winCount = ok ? r.win : 0;

            auto buf = makePacket(res);
            EnqueueSend(session, buf.data(), buf.size());
            break;
        }

        case C_LOBBY_ENTER:
        {
            if (!session || !session->authed) break;

            session->gameCharacterId = -1;

            {
                std::ostringstream oss;
                oss << "로비 진입 sid=" << (unsigned long long)session->id
                    << " playerId=" << session->playerId;
                Logger::Instance().Write(LogTag::RECV, oss.str());
            }

            LobbyProfilePacket profile{};
            profile.header.size = sizeof(profile);
            profile.header.type = S_LOBBY_PROFILE;

            profile.playerId = session->playerId;
            profile.iconId = session->iconId;
            FillFixedString(profile.nickname, MAX_NICK_LEN, session->nickname);
            profile.totalGameCount = session->totalGameCount;
            profile.winCount = session->winCount;

            auto b1 = makePacket(profile);
            EnqueueSend(session, b1.data(), b1.size());

            CharacterListPacket cl{};
            cl.header.size = sizeof(cl);
            cl.header.type = S_CHARACTER_LIST;
            cl.characterCount = 0;
            std::memset(cl.characters, 0, sizeof(cl.characters));

            std::vector<DbCharacterData> chars;
            if (DBManager::Instance().LoadCharacters(chars))
            {
                cl.characterCount = (int32_t)chars.size();
                if (cl.characterCount > MAX_CHARACTERS)
                    cl.characterCount = MAX_CHARACTERS;

                for (int i = 0; i < cl.characterCount; ++i)
                {
                    cl.characters[i].characterId = chars[i].characterId;
                    std::memset(cl.characters[i].characterName, 0, MAX_CHAR_NAME_LEN);
                    std::strncpy(cl.characters[i].characterName, chars[i].characterName, MAX_CHAR_NAME_LEN - 1);
                    cl.characters[i].hp = chars[i].hp;
                    cl.characters[i].moveSpeed = chars[i].moveSpeed;
                    cl.characters[i].attackPower = chars[i].attackPower;
                }
            }
            else
            {
                std::ostringstream oss;
                oss << "캐릭터 목록 로드 실패 sid=" << (unsigned long long)session->id;
                Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
            }

            auto b2 = makePacket(cl);
            EnqueueSend(session, b2.data(), b2.size());

            WeaponListPacket wl{};
            wl.header.size = sizeof(wl);
            wl.header.type = S_WEAPON_LIST;
            wl.weaponCount = 0;
            std::memset(wl.weapons, 0, sizeof(wl.weapons));

            std::vector<DbWeaponData> weapons;
            if (DBManager::Instance().LoadWeapons(weapons))
            {
                wl.weaponCount = (int32_t)weapons.size();
                if (wl.weaponCount > MAX_WEAPONS) wl.weaponCount = MAX_WEAPONS;
                for (int i = 0; i < wl.weaponCount; ++i)
                {
                    wl.weapons[i].weaponId = weapons[i].weaponId;
                    std::memset(wl.weapons[i].weaponName, 0, MAX_WEAPON_NAME_LEN);
                    std::strncpy(wl.weapons[i].weaponName, weapons[i].weaponName, MAX_WEAPON_NAME_LEN - 1);
                    wl.weapons[i].attackPower = weapons[i].attackPower;
                }
            }
            else
            {
                std::ostringstream oss;
                oss << "무기 목록 로드 실패 sid=" << (unsigned long long)session->id;
                Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
            }

            auto b3 = makePacket(wl);
            EnqueueSend(session, b3.data(), b3.size());

            break;
        }

        case C_MATCH_START:
        {
            {
                std::ostringstream oss;
                oss << "매칭 시작 요청 sid=" << (unsigned long long)session->id;
                Logger::Instance().Write(LogTag::MATCHQUEUE, oss.str());
            }
            MatchManager::Instance().TryMatch(session);
            break;
        }

        case C_SET_CHARACTER:
        {
            auto pkt = reinterpret_cast<SetCharacterPacket*>(session->rxBuf);
            {
                std::ostringstream oss;
                oss << "캐릭터 선택 sid=" << (unsigned long long)session->id
                    << " characterId=" << pkt->characterId;
                Logger::Instance().Write(LogTag::MATCH, oss.str());
            }
            MatchManager::Instance().OnSetCharacter(session, pkt->characterId);
            break;
        }

        case C_GAME_INPUT:
        {
            auto pkt = reinterpret_cast<GameInputPacket*>(session->rxBuf);
            GameManager::Instance().OnInputPacket(session, pkt);
            break;
        }

        case C_GAME_FIRE:
        {
            auto pkt = reinterpret_cast<GameFirePacket*>(session->rxBuf);
            GameManager::Instance().OnFirePacket(session, pkt);
            break;
        }

        case C_QUIT:
        {
            closeNow = true;
            break;
        }

        default:
        {
            std::ostringstream oss;
            oss << "알 수 없는 패킷 sid=" << (unsigned long long)session->id
                << " type=" << header->type
                << " size=" << header->size;
            Logger::Instance().Write(LogTag::RECV, oss.str());
            break;
        }
        }

        if (closeNow)
        {
            CloseSession(session);
            return;
        }

        size_t remain = session->rxUsed - header->size;
        memmove(session->rxBuf, session->rxBuf + header->size, remain);
        session->rxUsed = remain;
    }

    PostRecv(session);
}

void IOCP_EchoServer::EnqueueSend(Session* session, const char* data, size_t len)
{
    if (!session || session->closing) return;
    if (!data || len == 0) return;

    {
        std::lock_guard<std::mutex> lock(session->sendMtx);
        session->sendQueue.push(std::vector<char>(data, data + len));
    }

    bool wasSending = session->sending.exchange(true);
    if (!wasSending)
        PostSend(session);
}

void IOCP_EchoServer::PostSend(Session* session)
{
    if (!session || session->closing) return;

    session->sendingBytes.clear();

    {
        std::lock_guard<std::mutex> lock(session->sendMtx);

        if (session->sendQueue.empty())
        {
            session->sending = false;
            return;
        }

        int cnt = 0;
        while (!session->sendQueue.empty() && cnt < 16)
        {
            auto& front = session->sendQueue.front();
            session->sendingBytes.insert(session->sendingBytes.end(), front.begin(), front.end());
            session->sendQueue.pop();
            cnt++;
        }
    }

    ZeroMemory(&session->ovSend, sizeof(OverlappedEx));
    session->ovSend.op = OverlappedEx::OP_SEND;
    session->ovSend.session = session;

    session->ovSend.wsaBuf.buf = session->sendingBytes.data();
    session->ovSend.wsaBuf.len = (ULONG)session->sendingBytes.size();

    DWORD sentBytes = 0;
    int ret = WSASend(session->sock, &session->ovSend.wsaBuf, 1, &sentBytes, 0, &session->ovSend, NULL);
    if (ret == SOCKET_ERROR && WSAGetLastError() != WSA_IO_PENDING)
    {
        std::ostringstream oss;
        oss << "WSASend 실패 sid=" << (unsigned long long)session->id << " wsaErr=" << WSAGetLastError();
        Logger::Instance().Write(LogTag::SEND, oss.str());
        CloseSession(session);
    }
}

void IOCP_EchoServer::SendCompletion(Session* session, DWORD bytes)
{
    (void)bytes;
    if (!session || session->closing) return;

    session->sendingBytes.clear();
    PostSend(session);
}

void IOCP_EchoServer::CloseSession(Session* session)
{
    if (!session) return;
    if (session->closing.exchange(true)) return;

    {
        std::ostringstream oss;
        oss << "세션 종료 sid=" << (unsigned long long)session->id
            << " authed=" << (session->authed ? 1 : 0)
            << " playerId=" << session->playerId;
        Logger::Instance().Write(LogTag::SESSSIONCLOSE, oss.str());
    }

    MatchManager::Instance().CancelMatch(session);
    GameManager::Instance().OnDisconnect(session);

    shutdown(session->sock, SD_BOTH);
    closesocket(session->sock);

    if (session->rxBuf)
    {
        bufferPool_.Release(session->rxBuf);
        session->rxBuf = nullptr;
    }

    {
        std::lock_guard<std::mutex> lock(sessionTableMtx_);
        sessionTable_.erase(session->id);
    }

    delete session;
}

void IOCP_EchoServer::WorkerThread()
{
    while (running_)
    {
        DWORD bytes = 0;
        ULONG_PTR key = 0;
        LPOVERLAPPED ovRaw = nullptr;

        BOOL ok = GetQueuedCompletionStatus(iocpHandle_, &bytes, &key, &ovRaw, INFINITE);
        if (!running_) break;
        if (!ovRaw) continue;

        OverlappedEx* ov = reinterpret_cast<OverlappedEx*>(ovRaw);
        Session* s = reinterpret_cast<Session*>(key);

        if (!ok && ov->op != OverlappedEx::OP_ACCEPT)
        {
            if (s)
            {
                std::ostringstream oss;
                oss << "GQCS 실패 sid=" << (unsigned long long)s->id << " gle=" << GetLastError();
                Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
                CloseSession(s);
            }
            continue;
        }

        if (ov->op == OverlappedEx::OP_ACCEPT)
        {
            AcceptCompletion(ov, bytes);
            continue;
        }

        if (!s) continue;

        if (ov->op == OverlappedEx::OP_RECV)
            RecvCompletion(s, bytes);
        else if (ov->op == OverlappedEx::OP_SEND)
            SendCompletion(s, bytes);
    }
}

std::vector<std::pair<uint64_t, Session*>> IOCP_EchoServer::GetAllSessions()
{
    std::vector<std::pair<uint64_t, Session*>> out;
    std::lock_guard<std::mutex> lock(sessionTableMtx_);
    out.reserve(sessionTable_.size());
    for (auto& kv : sessionTable_)
        out.push_back(kv);
    return out;
}