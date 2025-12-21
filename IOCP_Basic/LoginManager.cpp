#include "LoginManager.h"
#include "DBManager.h"
#include "Logger.h"
#include <sstream>

LoginManager& LoginManager::Instance()
{
    static LoginManager inst;
    return inst;
}

LoginResult LoginManager::Login(const std::string& userId, const std::string& password)
{
    LoginResult out{};

    {
        std::ostringstream oss;
        oss << "로그인 처리 시작 userId=" << userId;
        Logger::Instance().Write(LogTag::RECV, oss.str());
    }

    DbLoginProfile profile{};
    bool ok = DBManager::Instance().CheckLoginProfile(
        userId.c_str(),
        password.c_str(),
        profile
    );

    out.ok = ok;

    if (ok)
    {
        out.playerId = profile.playerId;
        out.iconId = profile.iconId;
        out.nickname = profile.nickname;
        out.total = profile.totalGameCount;
        out.win = profile.winCount;

        std::ostringstream oss;
        oss << "로그인 성공 userId=" << userId
            << " playerId=" << out.playerId
            << " iconId=" << out.iconId;
        Logger::Instance().Write(LogTag::RECV, oss.str());
    }
    else
    {
        out.playerId = 0;
        out.iconId = 0;
        out.nickname.clear();
        out.total = 0;
        out.win = 0;

        std::ostringstream oss;
        oss << "로그인 실패 userId=" << userId;
        Logger::Instance().Write(LogTag::RECV, oss.str());
    }

    return out;
}