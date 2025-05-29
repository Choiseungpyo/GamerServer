#include "stdafx.h"
#include "User.h"

User::User(int id)
	:id(id), state(TITLE), currRoomNum(-1)
{
	name = "User" + id;
}

User::~User()
{
}

void User::SetName(const string& name)
{
	this->name = name;
}

void User::SetState(UserState state)
{
	this->state = state;
}

void User::SetRoomNum(int roomNum)
{
	currRoomNum = roomNum;
}