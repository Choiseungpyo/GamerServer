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

	Game* game;
	int currRoomNum; // -1 : 방에 들어가지 않음

public:
	User(int id);

	~User();

	int GetId() const;
	void SetId(int id);

	const char* GetName() const;
	void SetName(const string& name);
	
	UserState GetState() const;
	void SetState(UserState state);

	int GetRoomNo() const;
	void SetCurrRoomNum(int roomNum);

	Game* GetGame() const
	{
		return game;
	}
	void SetGame(Game* game)
	{
		this->game = game;
	}
};

