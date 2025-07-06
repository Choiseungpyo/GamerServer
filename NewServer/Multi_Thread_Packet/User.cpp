#include "stdafx.h"

User::User(int id)
	:id(id), state(TITLE), currRoomNum(-1), game(nullptr)
{
	name = UserIcon::GetIconName(id);
}

User::~User()
{
}

int User::GetId() const
{
	return id;
}
void User::SetId(int id) 
{
	this->id = id;
}

const char* User::GetName() const 
{
	return name.c_str();
}

void User::SetName(const string& name)
{
	this->name = name;
}

UserState User::GetState() const 
{
	return state;
}

void User::SetState(UserState state)
{
	this->state = state;
}

int User::GetRoomNo() const 
{
	return currRoomNum;
}

void User::SetCurrRoomNum(int roomNum) 
{
	this->currRoomNum = roomNum;
}