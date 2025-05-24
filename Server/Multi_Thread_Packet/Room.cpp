#include "stdafx.h"
#include "Room.h"

Room::Room(int no, const RoomOption& roomOption)
	:no(no), name(roomOption.roomName), state(WAITING), matchType(roomOption.matchType)
{}


void Room::AddUser(const ClientSession* client)
{
	int clientId = client->GetId();
	User* user = client->GetUser();

	
	clientMap[client->GetSocket()] = client;
	TeamType teamType = JoinAvailableTeam(client->GetId());
	roomUserInfoMap[clientId] = RoomUserInfo(teamType);
	user->SetState(ROOM);

	cout << "User" << client->GetId() << "has entered the room.";
}

TeamType Room::JoinAvailableTeam(int clientId)
{
	if (redTeamIds.size() >= (int)matchType) {
		blueTeamIds.insert(clientId);
		return BLUE;
	}

	redTeamIds.insert(clientId);
	return RED;
}

bool Room::CanJoinRoom()
{
	// 게임 중일 경우
	if (state == PLAYING)
		return false;

	// 최대 인원에 도달했을 경우
	if (clientMap.size() >= (int)matchType)
		return false;

	return true;
}