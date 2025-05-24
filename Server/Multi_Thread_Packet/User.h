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
	string name; // ���� �̸� �� �ϳ� ���� ����
	UserState state;
	int currRoomNum; // -1 : �濡 ���� ����

public:
	User()
		:name(""), state(TITLE), currRoomNum(-1)
	{

	}

	~User()
	{
	}

	void SetName(const string& name)
	{
		this->name = name;
	}
	
	void SetState(UserState state)
	{
		this->state = state;
	}

	void SetRoomNum(int roomNum)
	{
		currRoomNum = roomNum;
	}
};

