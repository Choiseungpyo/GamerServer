#pragma once
#include <cstdint>
#include <vector>
#include <cstring>

static constexpr int MAX_PLAYERS = 3;
static constexpr int MAX_ID_LEN = 32;
static constexpr int MAX_PW_LEN = 32;
static constexpr int MAX_NICK_LEN = 32;

static constexpr int MAX_CHARACTERS = 64;
static constexpr int MAX_CHAR_NAME_LEN = 32;

static constexpr int MAX_WEAPONS = 16;
static constexpr int MAX_WEAPON_NAME_LEN = 32;

struct WeaponInfo
{
    int32_t weaponId;
    char weaponName[MAX_WEAPON_NAME_LEN];
    int32_t attackPower;
};


enum PacketType : uint16_t
{
    // Login
    C_LOGIN_REQ = 1,
    S_LOGIN_RES = 101,

    // Match
    C_MATCH_START = 11,
    S_MATCH_WAIT = 111,

    // Lobby
    C_LOBBY_ENTER = 13,
    S_LOBBY_PROFILE = 113,
    S_CHARACTER_LIST = 114,
    S_WEAPON_LIST = 115,

    // Character select
    C_SET_CHARACTER = 14,
    S_SET_CHARACTER = 116,

    // Game flow
    S_GAME_START = 121,

    // In-game
    C_GAME_INPUT = 21,
    C_GAME_FIRE = 22,

    S_GAME_STATE = 122,
    S_GAME_SHOT_RESULT = 123,
    S_GAME_OVER = 124,

    // Quit
    C_QUIT = 99
};

#pragma pack(push, 1)

struct PacketHeader
{
    uint16_t size;
    uint16_t type;
};

struct LoginReqPacket
{
    PacketHeader header;
    char userId[MAX_ID_LEN];
    char password[MAX_PW_LEN];
};

struct LoginResPacket
{
    PacketHeader header;
    uint8_t ok;
    int32_t playerId;
    int32_t iconId;
    char nickname[MAX_NICK_LEN];
    int32_t totalGameCount;
    int32_t winCount;
};

struct MatchStartPacket
{
    PacketHeader header;
};

struct MatchCancelPacket
{
    PacketHeader header;
};

struct ServerMatchWaitPacket
{
    PacketHeader header;
    int32_t queueSize;
};

struct LobbyEnterPacket
{
    PacketHeader header;
};

struct CharacterRow
{
    int32_t characterId;
    char characterName[MAX_CHAR_NAME_LEN];
    int32_t hp;
    float moveSpeed;
    int32_t attackPower;
};

struct LobbyProfilePacket
{
    PacketHeader header;
    int32_t playerId;
    int32_t iconId;
    char nickname[MAX_NICK_LEN];
    int32_t totalGameCount;
    int32_t winCount;
};

struct CharacterListPacket
{
    PacketHeader header;
    int32_t characterCount;
    CharacterRow characters[MAX_CHARACTERS];
};

struct WeaponListPacket
{
    PacketHeader header;
    int32_t weaponCount;
    WeaponInfo weapons[MAX_WEAPONS];
};


struct SetCharacterPacket
{
    PacketHeader header;
    int32_t characterId;
};

struct ServerSetCharacterPacket
{
    PacketHeader header;
    uint8_t ok;
    int32_t currentCharacterId;
};

struct GameStartPacket
{
    PacketHeader header;
    int32_t gameId;
    int32_t selfIndex;
    int32_t playerCount;

    uint64_t sessionIds[MAX_PLAYERS];

    float spawnX[MAX_PLAYERS];
    float spawnY[MAX_PLAYERS];
    float spawnZ[MAX_PLAYERS];

    int32_t characterIds[MAX_PLAYERS];
    int32_t weaponIds[MAX_PLAYERS];
};

struct GameInputPacket
{
    PacketHeader header;
    int32_t gameId;
    int32_t tick;
    float moveX;
    float moveZ;
    float yaw;
    float pitch;
    uint32_t buttons;
    int32_t weaponId;
};

struct GameFirePacket
{
    PacketHeader header;
    int32_t gameId;
    int32_t shotId;
    int32_t clientTick;
    int32_t weaponId;
};

struct PlayerState3D
{
    uint64_t sessionId;
    float x;
    float y;
    float z;
    float yaw;
    float pitch;
    int32_t hp;
    int32_t weaponId;
    int32_t lastAckTick;
};

struct ServerGameStatePacket
{
    PacketHeader header;
    int32_t gameId;
    int32_t serverTick;
    int32_t playerCount;
    PlayerState3D players[MAX_PLAYERS];
};

struct ServerShotResultPacket
{
    PacketHeader header;
    int32_t gameId;
    int32_t shotId;
    uint64_t shooterSessionId;
    uint8_t hit;
    uint64_t victimSessionId;
    float hitX;
    float hitY;
    float hitZ;
    int32_t damage;
    int32_t victimHp;
};

struct GameOverPacket
{
    PacketHeader header;
    int32_t gameId;

    int32_t rankCount;

    uint64_t rankSessionIds[MAX_PLAYERS];
    int32_t  rankCharacterIds[MAX_PLAYERS];
    int32_t  rankIconIds[MAX_PLAYERS];
    char     rankNicknamesFlat[MAX_PLAYERS * MAX_NICK_LEN];
};

#pragma pack(pop)

template<typename T>
inline std::vector<char> makePacket(const T& packet)
{
    const char* ptr = reinterpret_cast<const char*>(&packet);
    return std::vector<char>(ptr, ptr + sizeof(T));
}