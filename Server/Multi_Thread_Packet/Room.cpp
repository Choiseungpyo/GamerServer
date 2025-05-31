#include "stdafx.h"

Room::Room(int no, RoomOption roomOption)
	:no(no), name(roomOption.roomName), state(RoomState::WAITING), matchType(roomOption.matchType)
{}

Room::~Room() {}


int Room::GetUserCount() const
{
	shared_lock<shared_mutex> lock(mutex);
	return clientMap.size();
};

int Room::GetMaxUserCount() const
{
	shared_lock<shared_mutex> lock(mutex);
	return matchType;
};

std::vector<const ClientSession*>Room::GetAllClients()
{
	shared_lock<shared_mutex> lock(mutex);
	std::vector<const ClientSession*> clients;
	for (const auto& pair : clientMap)
	{
		clients.push_back(pair.second);
	}
	return clients;
}


void Room::SetNo(int no)
{
	unique_lock<shared_mutex> lock(mutex);
	this->no = no;
}

int Room::GetNo() const
{
	shared_lock<shared_mutex> lock(mutex);
	return no;
}

void Room::SetName(const string& name)
{
	unique_lock<shared_mutex> lock(mutex);
	this->name = name;
}

const string& Room::GetName() const
{
	shared_lock<shared_mutex> lock(mutex);
	return name;
}

void Room::SetRoomState(RoomState state)
{
	unique_lock<shared_mutex> lock(mutex);
	this->state = state;
}

RoomState Room::GetRoomState() const
{
	shared_lock<shared_mutex> lock(mutex);
	return state;
}

void Room::SetMatchType(MatchType matchType)
{
	unique_lock<shared_mutex> lock(mutex);
	this->matchType = matchType;
}

MatchType Room::GetMatch() const
{
	shared_lock<shared_mutex> lock(mutex);
	return matchType;
}

void Room::AddUser(const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);
	User* user = client->GetUser();

	int clientId = user->GetId();
	
	clientMap[clientId] = client;
	TeamType teamType = JoinAvailableTeam(clientId);
	roomUserInfoMap[clientId] = new RoomUserInfo(teamType);
	user->SetState(ROOM);

	// 팀의 몇번째에 위치하는지 계산해서 RoomUserInfo에 저장하는 것 필요

	cout << "User" << clientId << "has entered the room.";
}

// 호출에서 락걸어줘야 함
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
	shared_lock<shared_mutex> lock(mutex);
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
	shared_lock<shared_mutex> lock(mutex);
	std::vector<RoomUserData> result;

	for (auto& client : clientMap) {
		const RoomUserInfo& info = *roomUserInfoMap[client.first];
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
	unique_lock<shared_mutex> lock(mutex);
	if (roomUserInfoMap[userId]->readyState == UNREADY)
		roomUserInfoMap[userId]->readyState = READY;
	else
		roomUserInfoMap[userId]->readyState = UNREADY;
}

void Room::ChangeTeamType(int userId)
{
	unique_lock<shared_mutex> lock(mutex);
	if (roomUserInfoMap[userId]->teamType == RED)
		roomUserInfoMap[userId]->teamType = BLUE;
	else
		roomUserInfoMap[userId]->teamType = RED;
}

void Room::ChangeRoomUserInfo(PACKET_CHANGE_ROOM_OPTION* pack)
{
	unique_lock<shared_mutex> lock(mutex);
	no = pack->roomNo;
	name = pack->roomName;
	matchType = pack->matchType;
}


