#pragma once

class PlayerEntity;

enum UserState {
	TITLE,
	LOBBY,
	ROOM,
	INGAME
};

class User
{
	string name; // 동물 이름 중 하나 랜덤 배정
	UserState state;
	int currRoomNum; // -1 : 방에 들어가지 않음

public:
	User();

	~User();

	void SetName(const string& name);
	
	void SetState(UserState state);

	void SetRoomNum(int roomNum);
};

