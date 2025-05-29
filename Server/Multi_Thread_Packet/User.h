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
	int id;
	string name; // 동물 이름 중 하나 랜덤 배정
	UserState state;
	int currRoomNum; // -1 : 방에 들어가지 않음

public:
	User(int id);

	~User();

	int GetId() const { return id; }
	int SetId(int id) { this->id = id; }

	const string& GetName() const { return name; }
	void SetName(const string& name);
	
	UserState GetState() const { return state; }
	void SetState(UserState state);

	int GetRoomNum() const { return currRoomNum; }  
	void SetRoomNum(int roomNum);
};

