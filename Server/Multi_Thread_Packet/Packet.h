#pragma once

typedef enum PTYPE
{
	// A_B_COMMAND : A -> B 로 COMMAND 패킷 전달
	NONE,
	// Title
	S_C_ID, // 클라 고유 S_C_ID 전달
	C_S_ENTRY_LOBBY, // 로비 입장 버튼 누른 경우
	C_S_LOGOUT, // 게임 종료 버튼 누른 경우

#pragma region  Lobby
	S_C_ENTRY_LOBBY, // 유저 프로필 전달

	// 로비 방 정보 UI 업데이트
	S_C_UPDATE_LOBBY_ROOM_INFO,

	// 방 목록에서 특정 방 클릭시 입장하는 경우
	C_S_ENTRY_ROOM,
	S_C_ENTRY_ROOM,

	// 방 생성 버튼을 누른 경우
	C_S_CREATE_ROOM,

	// 로비의 방 목록 UI를 추가해야할 경우
	S_C_CREATE_LOBBY_ROOM_INFO,

	// 랜덤 입장 버튼을 누른 경우
	C_S_ENTRY_RANDOMROOM,
	S_C_ENTRY_RANDOMROOM,

	// Exit 버튼 누른 경우
	C_S_MOVE_TITLE,
	S_C_MOVE_TITLE,
#pragma endregion

#pragma region  Room

	// 준비 완료 버튼을 누른경우
	C_S_READY_BTN,
	S_C_READY_BTN,

	//게임 시작 버튼을 누른경우
	C_S_GAMETSTART_BTN,
	S_C_GAMETSTART_BTN,

	// 팀 변경 버튼을 누른 경우
	C_S_TEAM_CHANGE,
	S_C_TEAM_CHANGE,

	// 방 옵션 변경을 누른 경우
	C_S_CHANGE_ROOM_OPTION,
	S_C_CHANGE_ROOM_OPTION,

	// 로비로 나가기 버튼을 누른 경우
	C_S_MOVE_LOBBY,
	S_C_MOVE_LOBBY,
#pragma endregion

	S_PLAYER_SPAWN,
	C_PLAYER_MOVE,
	S_PLAYER_MOVE
}Ptype;

#pragma pack(push, 1)  // 메모리 정렬을 1바이트로 설정
typedef struct VECTOR3
{
	float x;
	float y;
	float z;

	VECTOR3() : x(0.0f), y(0.0f), z(0.0f) {}

}Vector3;


#pragma pack(push,1)
typedef struct PACKET
{
	DWORD Length;			//길이
	Ptype Type;				//타입
}Packet;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_ID : PACKET
{
	int id;
	PACKET_S_C_ID() {	//패킷 초기화
		Type = S_C_ID;
		Length = sizeof(*this);
		id = 0;
	}

}Packet_s_c_id;
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

	PACKET_LOBBY_USERS_INFO(const char* userName)
	{
		userId = 0;
		strncpy_s(this->userName, sizeof(this->userName), userName, _TRUNCATE);
	}
}Packet_lobby_users_info;
#pragma pack(pop)

// S_C_ENTRY_LOBBY
// 유저가 로비에 입장할 떄 전체 유저를 얻기 위함
#pragma pack(push,1)
typedef struct PACKET_S_C_LOBBY_USERS_INFO_HEADER : PACKET
{
	int userCount;

	PACKET_S_C_LOBBY_USERS_INFO_HEADER() {
		Type = S_C_ENTRY_LOBBY;
		Length = sizeof(PACKET_LOBBY_USERS_INFO);
		userCount = 0;
	}
}Packet_s_c_lobby_users_info_header;
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
	ReadyState readyState;
	TeamType teamType;
	int userOrderOfTeam;

	PACKET_ROOM_USER_INFO()
	{
		userId = 0;
		memset(userName, 0, sizeof(userName));
		readyState = UNREADY;
		teamType = RED;
		userOrderOfTeam = 0;
	}

	PACKET_ROOM_USER_INFO(const char* userName)
	{
		userId = 0;
		strncpy_s(this->userName, sizeof(this->userName), userName, _TRUNCATE);
		readyState = UNREADY;
		teamType = RED;
		userOrderOfTeam = 0;
	}
}Packet_room_user_info;
#pragma pack(pop)

// 유저가 입장할 떄 방에 있는 유저들의 정보를 얻기 위함
#pragma pack(push,1)
typedef struct PACKET_S_C_ROOM_USERS_INFO_HEADER : PACKET
{
	int hostId;
	int roomNo;
	char roomName[ROOM_NAME_SIZE];
	MatchType matchType;
	int userCount;

	PACKET_S_C_ROOM_USERS_INFO_HEADER() {
		Type = S_C_ENTRY_ROOM;
		Length = sizeof(PACKET_S_C_ROOM_USERS_INFO_HEADER);  // 이후 동적으로 더함
		hostId = 0;
		roomNo = 0;
		memset(roomName, 0, sizeof(roomName));
		matchType = SOLO;
		userCount = 0;
	}
}Packet_RoomUsersHeader;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_READY_BTN : PACKET
{
	ReadyState readyState;
	TeamType teamType;
	int orderOfTeam;

	PACKET_S_C_READY_BTN()
	{
		Type = S_C_READY_BTN;
		Length = sizeof(*this);
		readyState = UNREADY;
		teamType = RED;
		orderOfTeam = 0;
	}
}Packet_s_c_ready_btn;
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
typedef struct PACKET_S_C_UPDATE_LOBBY_ROOM_INFO : PACKET
{
	int roomNo;
	char roomName[ROOM_NAME_SIZE];
	int currNumOfPeople;
	int maxNumOfPeople;
	RoomState roomState;

	PACKET_S_C_UPDATE_LOBBY_ROOM_INFO()
	{
		Type = S_C_UPDATE_LOBBY_ROOM_INFO;
		Length = sizeof(*this);
		roomNo = 0;
		memset(roomName, 0, sizeof(roomName));
		currNumOfPeople = 0;
		maxNumOfPeople = 0;
		roomState = WAITING;
	}

	PACKET_S_C_UPDATE_LOBBY_ROOM_INFO(int roomNo, const string& roomName, int currNumOfPeople, int maxNumOfPeople, RoomState roomState)
	{
		Type = S_C_UPDATE_LOBBY_ROOM_INFO;
		Length = sizeof(*this);
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
	int prvOrderOfTeam;
	int currOrderOfTeam;
	TeamType currTeamType;

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
typedef struct PACKET_S_C_SPAWN : PACKET
{
	int id;
	Vector3 Pos;
	PACKET_S_C_SPAWN() {	//패킷 초기화
		Type = S_PLAYER_SPAWN;
		Length = sizeof(*this);
		id = 0;
		Pos = VECTOR3();
	}

}Packet_s_c_spawn;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_C_S_MOVE : PACKET
{
	unsigned char Directions;
	PACKET_C_S_MOVE() {	//패킷 초기화
		Type = C_PLAYER_MOVE;
		Length = sizeof(*this);
		Directions = 0;
	}

}Packet_c_s_move;
#pragma pack(pop)


#pragma pack(push,1)
typedef struct PACKET_S_MOVE : PACKET
{
	int Id;
	Vector3 Pos;
	PACKET_S_MOVE() {	//패킷 초기화
		Type = S_PLAYER_MOVE;
		Length = sizeof(*this);
		Id = 0;
		Pos = Vector3();
	}

}Packet_s_move;
#pragma pack(pop)