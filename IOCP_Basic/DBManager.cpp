#include "DBManager.h"
#include "Logger.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <algorithm>
#include <sstream>
#include "Game.h"

DBManager& DBManager::Instance()
{
    static DBManager inst;
    return inst;
}

bool DBManager::Initialize(const char* host, int port, const char* user, const char* pass, const char* db)
{
    std::lock_guard<std::mutex> lock(mtx);

    if (conn)
        return true;

    conn = mysql_init(nullptr);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "DB Initialize failed: mysql_init returned null");
        return false;
    }

    if (!mysql_real_connect(conn, host, user, pass, db, port, nullptr, 0))
    {
        char msg[512];
        std::snprintf(msg, sizeof(msg),
            "DB connect fail host=%s port=%d db=%s err=%s",
            (host ? host : "null"),
            port,
            (db ? db : "null"),
            mysql_error(conn));

        Logger::Error(LogTag::SYSTEMERROR, msg);

        mysql_close(conn);
        conn = nullptr;
        return false;
    }

    {
        char msg[256];
        std::snprintf(msg, sizeof(msg),
            "DB connect ok host=%s port=%d db=%s",
            (host ? host : "null"),
            port,
            (db ? db : "null"));
        Logger::Info(LogTag::SERVERSTART, msg);
    }

    return true;
}

void DBManager::Finalize()
{
    std::lock_guard<std::mutex> lock(mtx);

    if (conn)
    {
        mysql_close(conn);
        conn = nullptr;
        Logger::Info(LogTag::SERVERSTOP, "DB connection closed");
    }
}

bool DBManager::CheckLoginProfile(const char* userId, const char* password, DbLoginProfile& outProfile)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "CheckLoginProfile called but DB is not connected");
        return false;
    }
    if (!userId || !password)
    {
        Logger::Warning(LogTag::SYSTEMERROR, "CheckLoginProfile invalid args: null userId or password");
        return false;
    }

    char escId[256];
    char escPw[256];

    unsigned long idLen = (unsigned long)std::min<size_t>(std::strlen(userId), sizeof(escId) - 1);
    unsigned long pwLen = (unsigned long)std::min<size_t>(std::strlen(password), sizeof(escPw) - 1);

    mysql_real_escape_string(conn, escId, userId, idLen);
    mysql_real_escape_string(conn, escPw, password, pwLen);

    char q[1024];
    std::snprintf(
        q, sizeof(q),
        "SELECT p.player_id, p.nickname, p.icon_id, p.total_game_count, p.win_count "
        "FROM `Login` l "
        "JOIN `Player` p ON p.player_id = l.player_id "
        "WHERE l.user_id='%s' AND l.password_hash = SHA2(CONCAT('%s', l.salt), 256) "
        "LIMIT 1",
        escId, escPw
    );

    if (mysql_query(conn, q) != 0)
    {
        // 보안상 비밀번호 포함 쿼리는 로그로 남기지 않는다
        std::ostringstream oss;
        oss << "DB CheckLoginProfile query fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_RES* res = mysql_store_result(conn);
    if (!res)
    {
        std::ostringstream oss;
        oss << "DB CheckLoginProfile store_result fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_ROW row = mysql_fetch_row(res);
    if (!row)
    {
        mysql_free_result(res);
        // 로그인 실패는 정상 흐름일 수 있으니 로그를 남기지 않는다
        return false;
    }

    outProfile.playerId = row[0] ? std::atoi(row[0]) : 0;

    std::memset(outProfile.nickname, 0, sizeof(outProfile.nickname));
    if (row[1]) std::strncpy(outProfile.nickname, row[1], MAX_NICK_LEN - 1);

    outProfile.iconId = row[2] ? std::atoi(row[2]) : 0;
    outProfile.totalGameCount = row[3] ? std::atoi(row[3]) : 0;
    outProfile.winCount = row[4] ? std::atoi(row[4]) : 0;

    mysql_free_result(res);
    return true;
}

bool DBManager::LoadCharacters(std::vector<DbCharacterData>& outChars)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "LoadCharacters called but DB is not connected");
        return false;
    }

    outChars.clear();

    const char* q =
        "SELECT character_id, character_name, hp, move_speed, attack_power "
        "FROM `Character` "
        "ORDER BY character_id ASC";

    if (mysql_query(conn, q) != 0)
    {
        std::ostringstream oss;
        oss << "DB LoadCharacters query fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_RES* res = mysql_store_result(conn);
    if (!res)
    {
        std::ostringstream oss;
        oss << "DB LoadCharacters store_result fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_ROW row;
    while ((row = mysql_fetch_row(res)) != nullptr)
    {
        DbCharacterData c{};

        c.characterId = row[0] ? std::atoi(row[0]) : 0;

        std::memset(c.characterName, 0, sizeof(c.characterName));
        if (row[1]) std::strncpy(c.characterName, row[1], MAX_CHAR_NAME_LEN - 1);

        c.hp = row[2] ? std::atoi(row[2]) : 0;
        c.moveSpeed = row[3] ? (float)std::atof(row[3]) : 0.0f;
        c.attackPower = row[4] ? std::atoi(row[4]) : 0;

        outChars.push_back(c);

        if ((int)outChars.size() >= MAX_CHARACTERS)
            break;
    }

    mysql_free_result(res);

    {
        std::ostringstream oss;
        oss << "LoadCharacters ok count=" << outChars.size();
        Logger::Debug(LogTag::MATCH, oss.str());
    }

    return true;
}

bool DBManager::IsValidCharacter(int characterId)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "IsValidCharacter called but DB is not connected");
        return false;
    }

    char q[256];
    std::snprintf(q, sizeof(q),
        "SELECT 1 FROM `Character` WHERE character_id=%d LIMIT 1",
        characterId
    );

    if (mysql_query(conn, q) != 0)
    {
        std::ostringstream oss;
        oss << "DB IsValidCharacter query fail characterId=" << characterId << " err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_RES* res = mysql_store_result(conn);
    if (!res)
    {
        std::ostringstream oss;
        oss << "DB IsValidCharacter store_result fail characterId=" << characterId << " err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_ROW row = mysql_fetch_row(res);
    mysql_free_result(res);

    return row != nullptr;
}

bool DBManager::CreateMatch(uint64_t& outMatchId)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "CreateMatch called but DB is not connected");
        return false;
    }

    const char* q = "INSERT INTO `GameMatch`() VALUES()";
    if (mysql_query(conn, q) != 0)
    {
        std::ostringstream oss;
        oss << "DB CreateMatch insert fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    outMatchId = (uint64_t)mysql_insert_id(conn);

    if (outMatchId == 0)
    {
        Logger::Error(LogTag::SYSTEMERROR, "DB CreateMatch insert_id returned 0");
        return false;
    }

    {
        std::ostringstream oss;
        oss << "CreateMatch ok matchId=" << (unsigned long long)outMatchId;
        Logger::Info(LogTag::MATCH, oss.str());
    }

    return true;
}

bool DBManager::FinalizeMatchRank3(uint64_t matchId, const DbRankRow rows[3], int rowCount)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "FinalizeMatchRank3 called but DB is not connected");
        return false;
    }
    if (matchId == 0) return false;
    if (rowCount <= 0) return false;

    if (mysql_query(conn, "START TRANSACTION") != 0)
    {
        std::ostringstream oss;
        oss << "DB FinalizeMatchRank3 START TRANSACTION fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    bool ok = true;
    bool loggedFail = false;

    int winnerPlayerId = 0;
    for (int i = 0; i < rowCount; ++i)
        if (rows[i].rank == 1)
            winnerPlayerId = rows[i].playerId;

    {
        char q[256];
        if (winnerPlayerId > 0)
        {
            std::snprintf(q, sizeof(q),
                "UPDATE `GameMatch` SET ended_at=CURRENT_TIMESTAMP, winner_player_id=%d WHERE match_id=%llu",
                winnerPlayerId, (unsigned long long)matchId
            );
        }
        else
        {
            std::snprintf(q, sizeof(q),
                "UPDATE `GameMatch` SET ended_at=CURRENT_TIMESTAMP, winner_player_id=NULL WHERE match_id=%llu",
                (unsigned long long)matchId
            );
        }

        if (mysql_query(conn, q) != 0)
        {
            ok = false;
            if (!loggedFail)
            {
                std::ostringstream oss;
                oss << "DB FinalizeMatchRank3 update GameMatch fail matchId=" << (unsigned long long)matchId
                    << " err=" << mysql_error(conn);
                Logger::Error(LogTag::SYSTEMERROR, oss.str());
                loggedFail = true;
            }
        }
    }

    for (int i = 0; i < rowCount && ok; ++i)
    {
        if (rows[i].playerId <= 0) continue;
        if (rows[i].characterId <= 0) continue;
        if (rows[i].rank <= 0) continue;

        char q[256];
        std::snprintf(q, sizeof(q),
            "INSERT INTO `GameResult`(match_id, player_id, character_id, rank) "
            "VALUES(%llu, %d, %d, %d)",
            (unsigned long long)matchId,
            rows[i].playerId,
            rows[i].characterId,
            rows[i].rank
        );

        if (mysql_query(conn, q) != 0)
        {
            ok = false;
            if (!loggedFail)
            {
                std::ostringstream oss;
                oss << "DB FinalizeMatchRank3 insert GameResult fail matchId=" << (unsigned long long)matchId
                    << " err=" << mysql_error(conn);
                Logger::Error(LogTag::SYSTEMERROR, oss.str());
                loggedFail = true;
            }
        }
    }

    if (ok)
    {
        for (int i = 0; i < rowCount; ++i)
        {
            if (rows[i].playerId <= 0) continue;

            char q[256];
            std::snprintf(q, sizeof(q),
                "UPDATE `Player` SET total_game_count = total_game_count + 1 WHERE player_id=%d",
                rows[i].playerId
            );
            if (mysql_query(conn, q) != 0)
            {
                ok = false;
                if (!loggedFail)
                {
                    std::ostringstream oss;
                    oss << "DB FinalizeMatchRank3 update total_game_count fail matchId=" << (unsigned long long)matchId
                        << " err=" << mysql_error(conn);
                    Logger::Error(LogTag::SYSTEMERROR, oss.str());
                    loggedFail = true;
                }
                break;
            }
        }
    }

    if (ok && winnerPlayerId > 0)
    {
        char q[256];
        std::snprintf(q, sizeof(q),
            "UPDATE `Player` SET win_count = win_count + 1 WHERE player_id=%d",
            winnerPlayerId
        );
        if (mysql_query(conn, q) != 0)
        {
            ok = false;
            if (!loggedFail)
            {
                std::ostringstream oss;
                oss << "DB FinalizeMatchRank3 update win_count fail matchId=" << (unsigned long long)matchId
                    << " err=" << mysql_error(conn);
                Logger::Error(LogTag::SYSTEMERROR, oss.str());
                loggedFail = true;
            }
        }
    }

    if (ok)
    {
        mysql_query(conn, "COMMIT");

        std::ostringstream oss;
        oss << "FinalizeMatchRank3 ok matchId=" << (unsigned long long)matchId
            << " rowCount=" << rowCount
            << " winnerPlayerId=" << winnerPlayerId;
        Logger::Info(LogTag::GAMEOVER, oss.str());
    }
    else
    {
        mysql_query(conn, "ROLLBACK");

        std::ostringstream oss;
        oss << "FinalizeMatchRank3 rollback matchId=" << (unsigned long long)matchId;
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
    }

    return ok;
}

bool DBManager::LoadWeapons(std::vector<DbWeaponData>& outWeapons)
{
    std::lock_guard<std::mutex> lock(mtx);
    outWeapons.clear();
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "LoadWeapons called but DB is not connected");
        return false;
    }

    const char* q =
        "SELECT weapon_id, weapon_name, attack_power, `range` "
        "FROM `Weapon` "
        "ORDER BY weapon_id ASC";

    if (mysql_query(conn, q) != 0)
    {
        std::ostringstream oss;
        oss << "DB LoadWeapons query fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_RES* res = mysql_store_result(conn);
    if (!res)
    {
        std::ostringstream oss;
        oss << "DB LoadWeapons store_result fail err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_ROW row = nullptr;
    while ((row = mysql_fetch_row(res)) != nullptr)
    {
        DbWeaponData w{};
        w.weaponId = row[0] ? std::atoi(row[0]) : 0;

        std::memset(w.weaponName, 0, MAX_WEAPON_NAME_LEN);
        if (row[1]) std::strncpy(w.weaponName, row[1], MAX_WEAPON_NAME_LEN - 1);

        w.attackPower = row[2] ? std::atoi(row[2]) : 0;
        w.range = row[3] ? (float)std::atof(row[3]) : 60.0f;

        outWeapons.push_back(w);
        if ((int)outWeapons.size() >= MAX_WEAPONS) break;
    }

    mysql_free_result(res);

    {
        std::ostringstream oss;
        oss << "LoadWeapons ok count=" << outWeapons.size();
        Logger::Debug(LogTag::MATCH, oss.str());
    }

    return true;
}

bool DBManager::IsValidWeapon(int weaponId)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "IsValidWeapon called but DB is not connected");
        return false;
    }

    char q[256]{ 0 };
    std::snprintf(q, sizeof(q),
        "SELECT 1 FROM `Weapon` WHERE weapon_id=%d LIMIT 1",
        weaponId);

    if (mysql_query(conn, q) != 0)
    {
        std::ostringstream oss;
        oss << "DB IsValidWeapon query fail weaponId=" << weaponId << " err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_RES* res = mysql_store_result(conn);
    if (!res)
    {
        std::ostringstream oss;
        oss << "DB IsValidWeapon store_result fail weaponId=" << weaponId << " err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    bool ok = (mysql_num_rows(res) > 0);
    mysql_free_result(res);
    return ok;
}

bool DBManager::GetDefaultWeaponId(int characterId, int& outWeaponId)
{
    std::lock_guard<std::mutex> lock(mtx);

    outWeaponId = 0;

    if (!conn)
    {
        Logger::Error(LogTag::SYSTEMERROR, "GetDefaultWeaponId called but DB is not connected");
        return false;
    }
    if (characterId <= 0)
        return false;

    char q[256];
    std::snprintf(
        q, sizeof(q),
        "SELECT weapon_id FROM `CharacterDefaultWeapon` WHERE character_id=%d LIMIT 1",
        characterId
    );

    if (mysql_query(conn, q) != 0)
    {
        std::ostringstream oss;
        oss << "DB GetDefaultWeaponId query fail characterId=" << characterId << " err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_RES* res = mysql_store_result(conn);
    if (!res)
    {
        std::ostringstream oss;
        oss << "DB GetDefaultWeaponId store_result fail characterId=" << characterId << " err=" << mysql_error(conn);
        Logger::Error(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    MYSQL_ROW row = mysql_fetch_row(res);
    if (!row || !row[0])
    {
        mysql_free_result(res);
        return false;
    }

    outWeaponId = std::atoi(row[0]);
    mysql_free_result(res);

    return outWeaponId > 0;
}