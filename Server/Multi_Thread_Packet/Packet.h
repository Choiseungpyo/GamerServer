#pragma once
#include "stdafx.h"

typedef enum PTYPE
{
	// A_B_COMMAND : A -> B 로 COMMAND 패킷 전달
	NONE,
	// Title
	S_C_ID, // 클라 고유 S_C_ID 전달
	C_S_ENTRY_LOBBY, // 로비 입장 버튼 누른 경우
	C_S_LOGOUT, // 게임 종료 버튼 누른 경우

#pragma region  Lobby
	S_C_USERS_PROFILE, // 유저 프로필 전달

	// 로비 방 정보 UI 업데이트
	S_C_LOBBY_ALL_ROOM_INFO,
	S_C_LOBBY_ROOM_INFO,

	// 방 안의 정보 세팅
	S_C_INROOM_INFO,

	// 방 생성 버튼을 누른 경우
	C_S_CREATE_ROOM,

	// 방 목록에서 특정 방 클릭시 입장하는 경우
	C_S_ENTRY_ROOM,

	// 랜덤 입장 버튼을 누른 경우
	C_S_ENTRY_RANDOMROOM,

	// Exit 버튼 누른 경우
	C_S_EXIT_LOBBY,
	S_C_EXIT_TITLE,
#pragma endregion

#pragma region  Room

	// 준비 완료 버튼을 누른경우
	C_S_INROOM_USERSTATE,
	S_C_INROOM_USERSTATE,

	//게임 시작 버튼을 누른경우
	C_S_GAMETSTART_BTN,
	S_C_GAMETSTART_BTN,

	// 팀 변경 버튼을 누른 경우
	C_S_TEAM_CHANGE,
	S_C_TEAM_CHANGE,

	// 방 옵션 변경을 누른 경우
	C_S_CHANGE_ROOM_OPTION,
	S_C_CHANGE_ROOM_OPTION,

	C_S_EXIT_ROOM,
	S_C_EXIT_ROOM,

#pragma endregion

	S_C_GAME_SPAWN_ALL,

}Ptype;

#pragma pack(push, 1)  // 메모리 정렬을 1바이트로 설정
typedef struct VECTOR3
{
	float x;
	float y;
	float z;

	VECTOR3() : x(0.0f), y(0.0f), z(0.0f) {}
	VECTOR3(float x, float y, float z) : x(x), y(y), z(z) {}

}Vector3;


#pragma pack(push,1)
typedef struct PACKET
{
	DWORD Length;			//길이
	Ptype Type;				//타입
}Packet;
#pragma pack(pop)

// S_C_USERS_PROFILE
// 유저가 로비에 입장할 떄 전체 유저를 얻기 위함
#pragma pack(push,1)
typedef struct PACKET_INFO_HEADER : PACKET
{
	int count;

	PACKET_INFO_HEADER(int count) {
		Type = S_C_USERS_PROFILE;
		Length = sizeof(PACKET_INFO_HEADER);
		this->count = count;
	}
}Packet_info_header;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_INT : PACKET
{
	int value;
	PACKET_INT() {	//패킷 초기화
		Type = S_C_ID;
		Length = sizeof(*this);
		value = 0;
	}

}PACKET_INT;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_LOBBY_USERS_INFO
{
	int userId;
	char userName[NAME_SIZE];

	PACKET_LOBBY_USERS_INFO()
	{
		userId = 0;
		memset(userName, 0, sizeof(userName));
	}
}Packet_lobby_users_info;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_C_S_ENTRY_ROOM : PACKET
{
	int id;
	int roomNo;

	PACKET_C_S_ENTRY_ROOM() {	//패킷 초기화
		Type = C_S_ENTRY_ROOM;
		Length = sizeof(*this);
		id = 0;
		roomNo = 0;
	}

}Packet_c_s_entry_room;
#pragma pack(pop)


#pragma pack(push,1)
typedef struct PACKET_C_S_CREATE_ROOM : PACKET
{
	int id;
	char roomName[ROOM_NAME_SIZE];
	MatchType matchType;

	PACKET_C_S_CREATE_ROOM() {	//패킷 초기화
		Type = C_S_CREATE_ROOM;
		Length = sizeof(*this);
		id = 0;
		memset(roomName, 0, sizeof(roomName));
		matchType = SOLO;
	}

}Packet_c_s_create_room;
#pragma pack(pop)

typedef struct PACKET_S_C_CREATE_ROOM : PACKET
{
	int id;
	int roomNo;
	char roomName[ROOM_NAME_SIZE];
	MatchType matchType;

	PACKET_S_C_CREATE_ROOM() {	//패킷 초기화
		Type = C_S_CREATE_ROOM;
		Length = sizeof(*this);
		id = 0;
		roomNo = 0;
		memset(roomName, 0, sizeof(roomName));
		matchType = SOLO;
	}

}Packet_s_c_create_room;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_ROOM_USER_INFO
{
	int userId;
	char userName[NAME_SIZE];
	InRoomUserState inRoomUserState;
	TeamType teamType;
	int userOrderOfTeam;

	PACKET_ROOM_USER_INFO()
	{
		userId = 0;
		memset(userName, 0, sizeof(userName));
		inRoomUserState = UNREADY;
		teamType = RED;
		userOrderOfTeam = 0;
	}
}Packet_room_user_info;
#pragma pack(pop)

// 유저가 입장할 떄 방에 있는 유저들의 정보를 얻기 위함
#pragma pack(push,1)
typedef struct PACKET_S_C_INROOM_INFO_HEADER : PACKET
{
	int hostId;
	int roomNo;
	char roomName[ROOM_NAME_SIZE];
	MatchType matchType;
	int userCount;

	PACKET_S_C_INROOM_INFO_HEADER() {
		Type = S_C_INROOM_INFO;
		Length = sizeof(PACKET_S_C_INROOM_INFO_HEADER);  // 이후 동적으로 더함
		hostId = 0;
		roomNo = 0;
		memset(roomName, 0, sizeof(roomName));
		matchType = SOLO;
		userCount = 0;
	}
}Packet_RoomUsersHeader;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_CHANGE_INROOM_USERSTATE : PACKET
{
	InRoomUserState inRoomUserState;
	TeamType teamType;
	int orderOfTeam;

	PACKET_S_C_CHANGE_INROOM_USERSTATE()
	{
		Type = S_C_INROOM_USERSTATE;
		Length = sizeof(*this);
		inRoomUserState = UNREADY;
		teamType = RED;
		orderOfTeam = 0;
	}
}Packet_s_c_change_inroom_userstate;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_CHANGE_ROOM_OPTION : PACKET
{
	int roomNo;
	char roomName[ROOM_NAME_SIZE];
	MatchType matchType;

	PACKET_CHANGE_ROOM_OPTION()
	{
		Type = S_C_CHANGE_ROOM_OPTION;
		Length = sizeof(*this);
		roomNo = 0;
		memset(roomName, 0, sizeof(roomName));
		matchType = SOLO;
	}
}PACKET_CHANGE_ROOM_OPTION;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_UPDATE_LOBBY_ROOM_INFO
{
	int roomNo;
	char roomName[ROOM_NAME_SIZE];
	int currNumOfPeople;
	int maxNumOfPeople;
	RoomState roomState;

	PACKET_S_C_UPDATE_LOBBY_ROOM_INFO()
	{
		roomNo = 0;
		memset(roomName, 0, sizeof(roomName));
		currNumOfPeople = 0;
		maxNumOfPeople = 0;
		roomState = WAITING;
	}

	PACKET_S_C_UPDATE_LOBBY_ROOM_INFO(int roomNo, const string& roomName, int currNumOfPeople, int maxNumOfPeople, RoomState roomState)
	{
		this->roomNo = roomNo;
		strncpy_s(this->roomName, sizeof(this->roomName), roomName.c_str(), _TRUNCATE);
		this->currNumOfPeople = currNumOfPeople;
		this->maxNumOfPeople = maxNumOfPeople;
		this->roomState = roomState;
	}
}PACKET_s_c_update_lobby_room_info;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_TEAM_CHANGE : PACKET
{
	int prvOrderOfTeam; // 이전 팀 종류
	int currOrderOfTeam; // 바꾼 후 팀 내 위치
	TeamType currTeamType; // 바꾼 팀 종류

	PACKET_S_C_TEAM_CHANGE()
	{
		Type = S_C_TEAM_CHANGE;
		Length = sizeof(*this);
		prvOrderOfTeam = 0;
		currOrderOfTeam = 0;
		currTeamType = RED;
	}
}Packet_s_c_team_change;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_C_S_PLAYERENTITY_DATA
{
	int userId;
	char userName[NAME_SIZE];
	TeamType teamType;
	unsigned char directions;
	int rotationValue;
	unsigned char isShoot;
	unsigned char isReload;

	PACKET_C_S_PLAYERENTITY_DATA()
	{
		userId = 0;
		memset(userName, 0, sizeof(userName));
;		teamType = RED;
		directions = 0;
		rotationValue = RED;
		isShoot = 0;
		isReload = 0;
	}
}Packet_c_s_palyerentity_data;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_PLAYERENTITY_DATA
{
	int userId;
	char userName[NAME_SIZE];
	TeamType teamType;
	
	Vector3 position;
	Vector3 Rotataion;
	PlayerState state;
	int currHp;

	PACKET_S_C_PLAYERENTITY_DATA()
	{
		userId = 0;
		memset(userName, 0, sizeof(userName));
		teamType = RED;
		position = Vector3();
		Rotataion = Vector3();
		state = PlayerState::IDLE;
		currHp = 0;
	}
}Packet_s_c_palyerentity_data;
#pragma pack(pop)

//#pragma pack(push,1)
//typedef struct PACKET_C_S_MOVE : PACKET
//{
//	unsigned char Directions;
//	PACKET_C_S_MOVE() {	//패킷 초기화
//		Type = C_PLAYER_MOVE;
//		Length = sizeof(*this);
//		Directions = 0;
//	}
//
//}Packet_c_s_move;
//#pragma pack(pop)
//
//
//#pragma pack(push,1)
//typedef struct PACKET_S_MOVE : PACKET
//{
//	int Id;
//	Vector3 Pos;
//	PACKET_S_MOVE() {	//패킷 초기화
//		Type = S_PLAYER_MOVE;
//		Length = sizeof(*this);
//		Id = 0;
//		Pos = Vector3();
//	}
//
//}Packet_s_move;
//#pragma pack(pop)