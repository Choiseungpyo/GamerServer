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
	// ���� ���� ���
	if (state == PLAYING)
		return false;

	// �ִ� �ο��� �������� ���
	if (clientMap.size() >= (int)matchType)
		return false;

	return true;
}