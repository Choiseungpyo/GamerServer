#pragma once
#include "Room.h"

const int NAME_SIZE = 64;
const int ROOM_NAME_SIZE = 64;

typedef enum PTYPE
{
	// A_B_COMMAND : A -> B �� COMMAND ��Ŷ ����
	NONE,
	// Title
	S_C_ID, // Ŭ�� ���� ID ����
	C_S_ENTRY_LOBBY, // �κ� ���� ��ư ���� ���
	C_S_LOGOUT, // ���� ���� ��ư ���� ���

	// Lobby
	S_C_USER_PROFILE, // ���� ������ ����

	// �� ��Ͽ��� Ư�� �� Ŭ���� �����ϴ� ���
	C_S_ENTRY_ROOM, 
	S_C_ENTRY_ROOM, 

	// �� ���� ��ư�� ���� ���
	C_S_CREATE_ROOM, 
	S_C_CREATE_ROOM,

	// ���� ���� ��ư�� ���� ���
	C_S_ENTRY_RANDOMROOM,
	S_C_ENTRY_RANDOMROOM,

	C_S_MOVE_TITLE, // Exit ��ư ���� ���
	S_C_MOVE_TITLE, // Exit ��ư ���� ���
	
	// Room

	S_PLAYER_SPAWN,
	C_PLAYER_MOVE,
	S_PLAYER_MOVE
}Ptype;

#pragma pack(push, 1)  // �޸� ������ 1����Ʈ�� ����
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
	DWORD Length;			//����
	Ptype Type;				//Ÿ��
}Packet;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_ID : PACKET
{
	int id;
	PACKET_S_C_ID() {	//��Ŷ �ʱ�ȭ
		Type = S_C_ID;
		Length = sizeof(*this);
		id = 0;
	}

}Packet_s_c_id;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_C_S_ENTRY_LOBBY : PACKET
{
	int id;
	char name[NAME_SIZE];

	PACKET_C_S_ENTRY_LOBBY() {	//��Ŷ �ʱ�ȭ
		Type = C_S_ENTRY_LOBBY;
		Length = sizeof(*this);
		id = 0;
		memset(name, 0, sizeof(name));
	}

}Packet_c_s_entry_lobby;
#pragma pack(pop)


#pragma pack(push,1)
typedef struct PACKET_BOOL : PACKET
{
	int id;
	unsigned char isTrue;

	PACKET_BOOL() {	//��Ŷ �ʱ�ȭ
		Type = NONE;
		Length = sizeof(*this);
		id = 0;
		isTrue = false;
	}

}Packet_bool;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_C_S_ENTRY_ROOM : PACKET
{
	int id;
	int roomNo;

	PACKET_C_S_ENTRY_ROOM() {	//��Ŷ �ʱ�ȭ
		Type = C_S_ENTRY_ROOM;
		Length = sizeof(*this);
		id = 0;
		roomNo = 0;
	}

}Packet_c_s_entry_room;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_ENTRY_ROOM : PACKET
{
	int id;
	int roomNo;
	TeamType teamType;

	PACKET_S_C_ENTRY_ROOM() {	//��Ŷ �ʱ�ȭ
		Type = S_C_ENTRY_ROOM;
		Length = sizeof(*this);
		id = 0;
		roomNo = 0;
	}

}Packet_s_c_entry_room;
#pragma pack(pop)


#pragma pack(push,1)
typedef struct PACKET_C_S_CREATE_ROOM : PACKET
{
	int id;
	char roomName[ROOM_NAME_SIZE];
	MatchType matchType;

	PACKET_C_S_CREATE_ROOM() {	//��Ŷ �ʱ�ȭ
		Type = C_S_CREATE_ROOM;
		Length = sizeof(*this);
		id = 0;
		memset(roomName, 0, sizeof(*this));
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

	PACKET_S_C_CREATE_ROOM() {	//��Ŷ �ʱ�ȭ
		Type = C_S_CREATE_ROOM;
		Length = sizeof(*this);
		id = 0;
		roomNo = 0;
		memset(roomName, 0, sizeof(*this));
		matchType = SOLO;
	}

}Packet_s_c_create_room;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_C_SPAWN : PACKET
{
	int id;
	Vector3 Pos;
	PACKET_S_C_SPAWN() {	//��Ŷ �ʱ�ȭ
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
	PACKET_C_S_MOVE() {	//��Ŷ �ʱ�ȭ
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
	PACKET_S_MOVE() {	//��Ŷ �ʱ�ȭ
		Type = S_PLAYER_MOVE;
		Length = sizeof(*this);
		Id = 0;
		Pos = Vector3();
	}

}Packet_s_move;
#pragma pack(pop)
