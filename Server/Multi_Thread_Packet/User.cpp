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
	std::shared_lock<std::shared_mutex> lock(mutex);
	return id;
}
void User::SetId(int id) 
{
	std::unique_lock<std::shared_mutex> lock(mutex);
	this->id = id;
}

const char* User::GetName() const 
{
	std::shared_lock<std::shared_mutex> lock(mutex);
	return name.c_str();
}

void User::SetName(const string& name)
{
	std::unique_lock<std::shared_mutex> lock(mutex);
	this->name = name;
}

UserState User::GetState() const 
{
	std::shared_lock<std::shared_mutex> lock(mutex);
	return state;
}

void User::SetState(UserState state)
{
	std::unique_lock<std::shared_mutex> lock(mutex);
	this->state = state;
}

int User::GetRoomNo() const 
{
	std::shared_lock<std::shared_mutex> lock(mutex);
	return currRoomNum;
}

void User::SetCurrRoomNum(int roomNum) 
{
	std::unique_lock<std::shared_mutex> lock(mutex);
	this->currRoomNum = roomNum;
}