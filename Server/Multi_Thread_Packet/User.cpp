#include "stdafx.h"
#include "User.h"

User::User()
	:name(""), state(TITLE), currRoomNum(-1)
{

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