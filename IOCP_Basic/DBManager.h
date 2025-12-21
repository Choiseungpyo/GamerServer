#pragma once
#include <mutex>
#include <string>
#include <vector>
#include <cstdint>
#include <mysql.h>
#include "Packet.h"

struct PlayerSim;

struct DbLoginProfile
{
    int playerId = 0;
    char nickname[MAX_NICK_LEN] = { 0 };

    int iconId = 0;

    int totalGameCount = 0;
    int winCount = 0;
};

struct DbCharacterData
{
    int characterId = 0;
    char characterName[MAX_CHAR_NAME_LEN] = { 0 };
    int hp = 0;
    float moveSpeed = 0.0f;
    int attackPower = 0;
};

struct DbRankRow
{
    int playerId = 0;
    int characterId = 0;
    int rank = 0; // 1,2,3
};

struct DbWeaponData
{
    int weaponId = 0;
    char weaponName[MAX_WEAPON_NAME_LEN] = { 0 };
    int attackPower = 0;
    float range = 60.0f;
};

class DBManager
{
public:
    static DBManager& Instance();

    bool Initialize(const char* host = "127.0.0.1", int port = 3307,
        const char* user = "root", const char* pass = "bitnami",
        const char* db = "GameServerDB");
    void Finalize();

    bool CheckLoginProfile(const char* userId, const char* password, DbLoginProfile& outProfile);

    bool CreateMatch(uint64_t& outMatchId);
    bool FinalizeMatchRank3(uint64_t matchId, const DbRankRow rows[3], int rowCount);

    bool LoadCharacters(std::vector<DbCharacterData>& outChars);
    bool IsValidCharacter(int characterId);

    bool LoadWeapons(std::vector<DbWeaponData>& outWeapons);
    bool IsValidWeapon(int weaponId);

    bool GetDefaultWeaponId(int characterId, int& outWeaponId);

private:
    DBManager() = default;

private:
    MYSQL* conn = nullptr;
    std::mutex mtx;
};