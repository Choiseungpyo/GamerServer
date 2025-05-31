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
	
	mutable shared_mutex mutex;

public:
	User(int id);

	~User();

	int GetId() const;
	void SetId(int id);

	const char* GetName() const;
	void SetName(const string& name);
	
	UserState GetState() const;
	void SetState(UserState state);

	int GetRoomNum() const;
	void SetRoomNum(int roomNum);
};

