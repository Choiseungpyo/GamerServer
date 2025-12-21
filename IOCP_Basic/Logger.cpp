#include "Logger.h"
#include <windows.h>
#include <iostream>
#include <sstream>

Logger& Logger::Instance()
{
    static Logger instance;
    return instance;
}

Logger::Logger()
    : minLevel_(LogLevel::Debug)
{
    std::string dir = GetExeDir();
    std::string path = dir + "\\ServerLogFile.txt";
    file_.open(path, std::ios::out | std::ios::app);

    if (!file_.is_open())
        std::cerr << "Logger file open failed: " << path << std::endl;
}

Logger::~Logger()
{
    if (file_.is_open())
        file_.close();
}

void Logger::SetMinLevel(LogLevel level)
{
    std::lock_guard<std::mutex> lock(Instance().mutex_);
    Instance().minLevel_ = level;
}

void Logger::Debug(LogTag tag, const std::string& msg, bool printConsole)
{
    Instance().WriteInternal(LogLevel::Debug, tag, msg, printConsole);
}

void Logger::Info(LogTag tag, const std::string& msg, bool printConsole)
{
    Instance().WriteInternal(LogLevel::Info, tag, msg, printConsole);
}

void Logger::Warning(LogTag tag, const std::string& msg, bool printConsole)
{
    Instance().WriteInternal(LogLevel::Warning, tag, msg, printConsole);
}

void Logger::Error(LogTag tag, const std::string& msg, bool printConsole)
{
    Instance().WriteInternal(LogLevel::Error, tag, msg, printConsole);
}

void Logger::PacketRecv(uint64_t sessionId, uint16_t packetType, uint16_t packetSize, bool printConsole)
{
    std::ostringstream oss;
    oss << "sid=" << sessionId
        << " type=" << Instance().PacketTypeToString(packetType) << "(" << packetType << ")"
        << " size=" << packetSize;

    Instance().WriteInternal(LogLevel::Debug, LogTag::RECV, oss.str(), printConsole);
}

void Logger::PacketSend(uint64_t sessionId, uint16_t packetType, uint16_t packetSize, bool printConsole)
{
    std::ostringstream oss;
    oss << "sid=" << sessionId
        << " type=" << Instance().PacketTypeToString(packetType) << "(" << packetType << ")"
        << " size=" << packetSize;

    Instance().WriteInternal(LogLevel::Debug, LogTag::SEND, oss.str(), printConsole);
}

void Logger::Write(LogTag tag, const std::string& msg, bool printConsole)
{
    WriteInternal(LogLevel::Info, tag, msg, printConsole);
}

void Logger::WriteInternal(LogLevel level, LogTag tag, const std::string& msg, bool printConsole)
{
    std::lock_guard<std::mutex> lock(mutex_);

    if ((int)level < (int)minLevel_)
        return;

    auto now = std::chrono::system_clock::now();
    auto tt = std::chrono::system_clock::to_time_t(now);

    std::tm tm;
    localtime_s(&tm, &tt);

    auto ms = (int)(std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count() % 1000);
    DWORD tid = GetCurrentThreadId();

    std::ostringstream prefix;
    prefix << std::put_time(&tm, "%Y-%m-%d %H:%M:%S")
        << "." << std::setw(3) << std::setfill('0') << ms
        << " [" << LevelToString(level) << "]"
        << " [" << TagToString(tag) << "]"
        << " [tid=" << (unsigned long)tid << "]";

    std::string lineText = prefix.str() + " " + msg;

    if (printConsole)
    {
        if (level == LogLevel::Error || level == LogLevel::Warning)
            std::cerr << lineText << std::endl;
        else
            std::cout << lineText << std::endl;
    }

    if (file_.is_open())
    {
        file_ << lineText << std::endl;
        if (level == LogLevel::Error || level == LogLevel::Warning)
            file_.flush();
    }
}

const char* Logger::LevelToString(LogLevel level) const
{
    switch (level)
    {
    case LogLevel::Debug:   return "DEBUG";
    case LogLevel::Info:    return "INFO";
    case LogLevel::Warning: return "WARN";
    case LogLevel::Error:   return "ERROR";
    default:                return "UNKNOWN";
    }
}

const char* Logger::TagToString(LogTag tag) const
{
    switch (tag)
    {
    case LogTag::SERVERSTART:    return "SERVERSTART";
    case LogTag::SERVERSTOP:     return "SERVERSTOP";
    case LogTag::SYSTEMERROR:    return "SYSTEMERROR";

    case LogTag::ACCEPT:         return "ACCEPT";
    case LogTag::RECV:           return "RECV";
    case LogTag::SEND:           return "SEND";
    case LogTag::SESSSIONCLOSE:  return "SESSIONCLOSE";

    case LogTag::MATCH:          return "MATCH";
    case LogTag::MATCHQUEUE:     return "MATCHQUEUE";
    case LogTag::MATCHCANCEL:    return "MATCHCANCEL";

    case LogTag::GAMECREATE:     return "GAMECREATE";
    case LogTag::GAMEREMOVE:     return "GAMEREMOVE";
    case LogTag::GAMECOUNT:      return "GAMECOUNT";
    case LogTag::GAMEOVER:       return "GAMEOVER";

    case LogTag::PLAYERMOVE:     return "PLAYERMOVE";
    case LogTag::PLAYERSHOOT:    return "PLAYERSHOOT";

    case LogTag::CHATJOIN:       return "CHATJOIN";
    case LogTag::CHATLEAVE:      return "CHATLEAVE";
    case LogTag::CHATCOUNT:      return "CHATCOUNT";
    case LogTag::CHATMSG:        return "CHATMSG";

    case LogTag::USERLOGOUT:     return "USERLOGOUT";
    default:                     return "UNKNOWN";
    }
}

const char* Logger::PacketTypeToString(uint16_t type) const
{
    switch (type)
    {
    case 1:   return "C_LOGIN_REQ";
    case 101: return "S_LOGIN_RES";

    case 11:  return "C_MATCH_START";
    case 111: return "S_MATCH_WAIT";

    case 13:  return "C_LOBBY_ENTER";
    case 113: return "S_LOBBY_PROFILE";
    case 114: return "S_CHARACTER_LIST";
    case 115: return "S_WEAPON_LIST";

    case 14:  return "C_SET_CHARACTER";
    case 116: return "S_SET_CHARACTER";

    case 121: return "S_GAME_START";

    case 21:  return "C_GAME_INPUT";
    case 22:  return "C_GAME_FIRE";

    case 122: return "S_GAME_STATE";
    case 123: return "S_GAME_SHOT_RESULT";
    case 124: return "S_GAME_OVER";

    case 99:  return "C_QUIT";
    default:  return "UNKNOWN_PACKET";
    }
}

std::string Logger::GetExeDir() const
{
    char path[MAX_PATH] = { 0 };
    GetModuleFileNameA(nullptr, path, MAX_PATH);

    std::string exePath(path);
    size_t pos = exePath.find_last_of("\\/");
    if (pos == std::string::npos)
        return ".";
    return exePath.substr(0, pos);
}