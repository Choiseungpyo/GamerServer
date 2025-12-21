#pragma once
#include <mutex>
#include <vector>
#include <cstdint>
#include <cstring>
#include "Packet.h"
#include "Map.h"

struct Session;

struct InputState
{
    int tick = 0;
    float moveX = 0.0f;
    float moveZ = 0.0f;
    float yaw = 0.0f;
    float pitch = 0.0f;
    uint32_t buttons = 0;
    int weaponId = 0;
};

struct PlayerSim
{
    uint64_t sessionId = 0;
    int playerId = 0;
    int characterId = 0;
    int iconId = 0;
    char nickname[MAX_NICK_LEN]{};

    Vec3 pos{ 0, 0, 0 };
    float yaw = 0.0f;
    float pitch = 0.0f;
    int hp = 100;
    int weaponId = 0;

    int lastAckTick = 0;
    InputState lastInput{};
    bool connected = true;
};

struct WeaponStat
{
    int attackPower = 20;
    float range = 60.0f;
    bool valid = false;
};

class Game
{
public:
    Game(int gameId, uint64_t matchId, const std::vector<Session*>& players);
    int GetGameId() const;
    bool IsFinished() const;

    void OnInput(Session* sender, const InputState& in);
    void OnFire(Session* sender, int shotId, int clientTick, int weaponId);
    void OnDisconnect(Session* s);
    void Update(float dt);

private:
    int FindIndexBySession(uint64_t sid) const;

    void BroadcastState();
    void BroadcastShotResult(const ServerShotResultPacket& pkt);

    void BroadcastGameOverRank3(const uint64_t rankSid[3]);

    void SolvePlayerCollisions();
    bool RayVsSphere(const Vec3& o, const Vec3& d, const Vec3& c, float r, float maxDist, float& outT) const;
    bool RayVsCapsule(const Vec3& ro, const Vec3& rd, const Vec3& pa, const Vec3& pb, float r, float maxDist, float& outT) const;

    int DamageByWeapon(int weaponId) const;
    float RangeByWeapon(int weaponId) const;
    
    void InitWeaponStats();

    static void BuildRankSid3(uint64_t outRankSid[3], int alive, uint64_t lastAliveSid, const uint64_t* deathOrder, int deathCount);

private:
    int gameId = -1;
    int64_t matchId = -1;
    bool finished = false;
    int serverTick = 0;

    std::vector<Session*> sessions;
    PlayerSim players[MAX_PLAYERS]{};
    uint64_t deathOrder[MAX_PLAYERS] = { 0 };
    int deathCount = 0;

    float cameraPosY = 0.7f;

    WeaponStat weaponStats[MAX_WEAPONS + 1]{};
    bool gameOverSent = false;

    mutable std::mutex mtx;
};