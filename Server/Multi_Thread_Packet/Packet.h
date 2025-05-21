#pragma once

const int NICKNAME_SIZE = 64;

typedef enum PTYPE
{
	NONE,
	S_PLAYER_ID,
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

enum Direction
{
	UP = 0,
	DOWN,
	LEFT,
	RIGHT
};

#pragma pack(push,1)
typedef struct PACKET
{
	DWORD Length;			//����
	Ptype Type;				//Ÿ��
}Packet;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_S_ID : PACKET
{
	int id;
	PACKET_S_ID() {	//��Ŷ �ʱ�ȭ
		Type = S_PLAYER_ID;
		Length = sizeof(*this);
		id = 0;
	}

}Packet_s_id;
#pragma pack(pop)



#pragma pack(push,1)
typedef struct PACKET_C_NICKNAME : PACKET
{
	char Nickname[NICKNAME_SIZE];
	PACKET_C_NICKNAME() {	//��Ŷ �ʱ�ȭ
		Type = S_PLAYER_ID;
		Length = sizeof(*this);
		memset(Nickname, 0, sizeof(Nickname));
	}

}Packet_c_nickname;
#pragma pack(pop)


#pragma pack(push,1)
typedef struct PACKET_S_SPAWN : PACKET
{
	int id;
	Vector3 Pos;
	PACKET_S_SPAWN() {	//��Ŷ �ʱ�ȭ
		Type = S_PLAYER_SPAWN;
		Length = sizeof(*this);
		id = 0;
		Pos = VECTOR3();
	}

}Packet_s_spawn;
#pragma pack(pop)

#pragma pack(push,1)
typedef struct PACKET_C_MOVE : PACKET
{
	unsigned char Directions;
	PACKET_C_MOVE() {	//��Ŷ �ʱ�ȭ
		Type = C_PLAYER_MOVE;
		Length = sizeof(*this);
		Directions = 0;
	}

}Packet_c_move;
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