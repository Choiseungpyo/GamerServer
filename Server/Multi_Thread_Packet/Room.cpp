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
	
	clientMap[clientId] = client;
	TeamType teamType = JoinAvailableTeam(clientId);
	roomUserInfoMap[clientId] = RoomUserInfo(teamType);
	user->SetState(ROOM);

	// 팀의 몇번째에 위치하는지 계산해서 RoomUserInfo에 저장하는 것 필요

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

void Room::ChangeReadyState(int userId)
{
	if (roomUserInfoMap[userId].readyState == UNREADY)
		roomUserInfoMap[userId].readyState = READY;
	else
		roomUserInfoMap[userId].readyState = UNREADY;
}

void Room::ChangeTeamType(int userId)
{
	if (roomUserInfoMap[userId].teamType == RED)
		roomUserInfoMap[userId].teamType = BLUE;
	else
		roomUserInfoMap[userId].teamType = RED;
}

void Room::ChangeRoomUserInfo(PACKET_CHANGE_ROOM_OPTION* pack)
{
	no = pack->roomNo;
	name = pack->roomName;
	matchType = pack->matchType;
}


