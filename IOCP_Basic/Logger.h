#pragma once
#include <fstream>
#include <iomanip>
#include <chrono>
#include <mutex>
#include <string>
#include <cstdint>

enum class LogTag
{
    SERVERSTART,
    SERVERSTOP,
    SYSTEMERROR,

    ACCEPT,
    RECV,
    SEND,
    SESSSIONCLOSE,

    MATCH,
    MATCHQUEUE,
    MATCHCANCEL,

    GAMECREATE,
    GAMEREMOVE,
    GAMECOUNT,
    GAMEOVER,

    PLAYERMOVE,
    PLAYERSHOOT,

    CHATJOIN,
    CHATLEAVE,
    CHATCOUNT,
    CHATMSG,

    USERLOGOUT
};

enum class LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
};

class Logger
{
public:
    static Logger& Instance();

    static void Debug(LogTag tag, const std::string& msg, bool printConsole = true);
    static void Info(LogTag tag, const std::string& msg, bool printConsole = true);
    static void Warning(LogTag tag, const std::string& msg, bool printConsole = true);
    static void Error(LogTag tag, const std::string& msg, bool printConsole = true);

    static void PacketRecv(uint64_t sessionId, uint16_t packetType, uint16_t packetSize, bool printConsole = false);
    static void PacketSend(uint64_t sessionId, uint16_t packetType, uint16_t packetSize, bool printConsole = false);

    static void SetMinLevel(LogLevel level);

    void Write(LogTag tag, const std::string& msg, bool printConsole = true);

private:
    Logger();
    ~Logger();
    Logger(const Logger&) = delete;
    Logger& operator=(const Logger&) = delete;

private:
    void WriteInternal(LogLevel level, LogTag tag, const std::string& msg, bool printConsole);

    const char* LevelToString(LogLevel level) const;
    const char* TagToString(LogTag tag) const;
    const char* PacketTypeToString(uint16_t type) const;

    std::string GetExeDir() const;

private:
    std::ofstream file_;
    std::mutex mutex_;
    LogLevel minLevel_;
};