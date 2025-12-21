#pragma once
#include <string>

struct LoginResult
{
    bool ok = false;
    int playerId = 0;
    int iconId = 0;
    std::string nickname;
    int total = 0;
    int win = 0;
};

class LoginManager
{
public:
    static LoginManager& Instance();
    LoginResult Login(const std::string& userId, const std::string& password);

private:
    LoginManager() = default;
};