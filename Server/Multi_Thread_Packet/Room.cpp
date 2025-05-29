#include "stdafx.h"
#include "Room.h"

Room::Room(int no, const RoomOption& roomOption)
	:no(no), name(roomOption.roomName), state(RoomState::WAITING), matchType(roomOption.matchType)
{}

Room::~Room() {}


void Room::AddUser(const ClientSession* client)
{
	User* user = client->GetUser();

	int clientId = user->GetId();
	
	clientMap[client->GetSocket()] = client;
	TeamType teamType = JoinAvailableTeam(clientId);
	roomUserInfoMap[clientId] = RoomUserInfo(teamType);
	user->SetState(ROOM);

	cout << "User" << clientId << "has entered the room.";
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

void Room::Send_InRoom_UsersData()
{
	std::vector<RoomUserData> result;

	for (auto& client : clientMap) {
		RoomUserInfo& info = roomUserInfoMap[client.first];
		User* user = client.second->GetUser();

		if (user == nullptr) continue;

		RoomUserData roomUserData;
		roomUserData.userId = user->GetId();
		//strcpy(roomUserData.userName, user->GetName().c_str());
		roomUserData.isReady = info.readyState;
		roomUserData.isHost = info.isHost;
		roomUserData.teamType = info.teamType;

		result.push_back(roomUserData);
	}
}


