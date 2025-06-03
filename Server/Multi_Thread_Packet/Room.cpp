#include "stdafx.h"

Room::Room(int no, RoomOption roomOption)
	:no(no), name(roomOption.roomName), state(RoomState::WAITING), matchType(roomOption.matchType), hostId(0), game(nullptr)
{}

Room::~Room() 
{
	clientMap.clear(); // Const ClientSession*는 SessionManager에서 삭제
	for (auto& roomUserInfoItem : roomUserInfoMap) {
		delete roomUserInfoItem.second;
	}
	roomUserInfoMap.clear();

	delete game;
}


int Room::GetUserCount() const
{
	shared_lock<shared_mutex> lock(mutex);
	return (int)clientMap.size();
};

int Room::GetMaxUserCount() const
{
	shared_lock<shared_mutex> lock(mutex);
	return (int)matchType * 2;
};

std::vector<int> Room::GetAllClientId()
{
	shared_lock<shared_mutex> lock(mutex);
	std::vector<int> allClientId;
	for (const auto& pair : clientMap)
	{
		allClientId.push_back(pair.first);
	}

	return allClientId;
}

int Room::GetRedTeamUserOrder(int userId) const
{
	for (int i = 0; i < redTeamUserOrder.size(); i++)
	{
		if (redTeamUserOrder[i] == userId)
			return i;
	}

	return -1;
}

int Room::GetBlueTeamUserOrder(int userId) const
{
	for (int i = 0; i < blueTeamUserOrder.size(); i++)
	{
		if (blueTeamUserOrder[i] == userId)
			return i;
	}

	return -1;

}

PACKET_ROOM_USER_INFO Room::GetPacketRoomUserInfo(int id)
{
	shared_lock<shared_mutex> lock(mutex);

	PACKET_ROOM_USER_INFO pack;
	User* user = clientMap[id]->GetUser();
	auto roomUserInfo = roomUserInfoMap[id];

	pack.userId = id;
	strncpy_s(pack.userName, sizeof(pack.userName), user->GetName(), _TRUNCATE);
	pack.inRoomUserState = roomUserInfo->inRoomUserState;
	pack.teamType = roomUserInfo->teamType;
	pack.userOrderOfTeam = roomUserInfo->userOrderOfTeam;

	return pack;
}


RoomUserInfo* Room::GetRoomUserInfo(int id) const
{
	shared_lock<shared_mutex> lock(mutex);

	auto it = roomUserInfoMap.find(id);

	// 소켓이 없다면
	if (it == roomUserInfoMap.end()) {
		std::cout << "roomUserInfoMap - InValid Id : " << id << std::endl;
		return NULL;
	}

	return it->second;
}

void Room::CreateNewGame()
{
	Game* newGame = new Game(this);
	this->game = newGame;
	for (auto& pair : clientMap)
	{
		auto user = pair.second->GetUser();
		user->SetGame(newGame);
	}

	newGame->SpawnAllPlayerEntity();
}

int Room::GetHostId() const
{
	shared_lock<shared_mutex> lock(mutex);
	return hostId;
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

MatchType Room::GetMatchType() const
{
	shared_lock<shared_mutex> lock(mutex);
	return matchType;
}

void Room::AddUser(const ClientSession* client)
{
	User* user = client->GetUser();

	int clientId = user->GetId();
	
	{
		unique_lock<shared_mutex> lock(mutex);
		clientMap[clientId] = client;
	}

	auto data = JoinAvailableTeam(clientId);

	{
		unique_lock<shared_mutex> lock(mutex);
		TeamType team = std::get<0>(data);
		int orderOfTeam = std::get<1>(data);

		bool isHost = clientMap.size() == 1;
		if (isHost)
			hostId = user->GetId();
		roomUserInfoMap[clientId] = new RoomUserInfo(isHost, team, orderOfTeam);
	}

	user->SetState(ROOM);

	// 팀의 몇번째에 위치하는지 계산해서 RoomUserInfo에 저장하는 것 필요

	cout << "User" << clientId << "has entered the room.";
}

/// <summary>
/// 유저 삭제
/// </summary>
/// <param name="userId"></param>
/// <returns>방에 남은 유저 수</returns>
int Room::DeleteUser(int userId)
{
	unique_lock<shared_mutex> lock(mutex);
	auto it = roomUserInfoMap.find(userId);

	// 못찾았을 경우
	if (it == roomUserInfoMap.end()) {
		cout << "roomUserInfoMap - Invalid Id : " << userId << endl;
		return -1;
	}

	delete it->second;
	roomUserInfoMap.erase(it);

	clientMap.erase(userId);
	return (int)clientMap.size();
}

// 유저가 무조건 들어올 수 있다는 가정하에 동작
tuple<TeamType, int> Room::JoinAvailableTeam(int clientId)
{
	unique_lock<shared_mutex> lock(mutex);

	int redTeamSize = (int)redTeamUserOrder.size();
	int blueTeamSize = (int)blueTeamUserOrder.size();

	if (redTeamSize > blueTeamSize) {
		blueTeamUserOrder.push_back(clientId);
		return make_tuple(BLUE, blueTeamUserOrder.size()-1); // 팀에서의 자기 위치 index : 0~max-1
	}

	redTeamUserOrder.push_back(clientId);
	return make_tuple(RED, redTeamUserOrder.size()-1); // 팀에서의 자기 위치 index : 0~max-1
}

bool Room::CanJoinRoom()
{
	// 게임 중일 경우
	if (state == PLAYING)
		return false;

	// 최대 인원에 도달했을 경우
	if (GetUserCount() >= GetMaxUserCount())
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
		strncpy_s(roomUserData.userName, sizeof(roomUserData.userName), user->GetName(), _TRUNCATE);
		roomUserData.isReady = info.inRoomUserState;
		roomUserData.isHost = info.isHost;
		roomUserData.teamType = info.teamType;

		result.push_back(roomUserData);
	}
}


void Room::SendToAllUserInRoom(vector<char> buffer) const
{
	vector<const ClientSession*> clients;

	{
		shared_lock<shared_mutex> lock(mutex);

		for (auto& item : clientMap)
			clients.push_back(item.second);
	}

	for (auto client : clients)
	{
		send(client->GetSocket(), buffer.data(), (int)buffer.size(), 0);
	}
}

void Room::SendToAllUserInRoom(const Packet* pack) const
{
	vector<const ClientSession*> clients;

	{
		shared_lock<shared_mutex> lock(mutex);

		for (auto& item : clientMap)
			clients.push_back(item.second);
	}

	for (auto client : clients)
	{
		client->Send(pack);
	}
}

const RoomUserInfo* Room::ChangeInRoomUserState(int userId)
{
	unique_lock<shared_mutex> lock(mutex);

	auto roomUserInfo = roomUserInfoMap[userId];
	if (roomUserInfo->isHost)
	{
		if (roomUserInfo->inRoomUserState == InRoomUserState::IDLE)
			roomUserInfo->inRoomUserState = START;
		else
			roomUserInfo->inRoomUserState = InRoomUserState::IDLE;
	}
	else
	{
		if (roomUserInfo->inRoomUserState == UNREADY)
			roomUserInfo->inRoomUserState = READY;
		else
			roomUserInfo->inRoomUserState = UNREADY;
	}
	
	return roomUserInfo;
}

PACKET_S_C_TEAM_CHANGE Room::ChangeTeamType(RoomUserInfo* roomUserInfo, int userId)
{
	unique_lock<shared_mutex> lock(mutex);
	
	int userOrderOfTeam;
	int prvOrdereOfTeam;

	// 팀 변경하기
	// 현재 팀이 레드팀인 경우
	if (roomUserInfo->teamType == RED)
	{
		prvOrdereOfTeam = redTeamUserOrder[userId];
		roomUserInfo->teamType = BLUE;

		int order = GetRedTeamUserOrder(userId);

		redTeamUserOrder.erase(redTeamUserOrder.begin() + order);
		userOrderOfTeam = (int)blueTeamUserOrder.size();
		blueTeamUserOrder[userId] = userOrderOfTeam;
		roomUserInfo->userOrderOfTeam = userOrderOfTeam;
	}
	else
	{
		prvOrdereOfTeam = blueTeamUserOrder[userId];
		roomUserInfo->teamType = RED;
		
		int order = GetBlueTeamUserOrder(userId);

		blueTeamUserOrder.erase(blueTeamUserOrder.begin() + order);
		userOrderOfTeam = (int)redTeamUserOrder.size();
		redTeamUserOrder[userId] = userOrderOfTeam;
		roomUserInfo->userOrderOfTeam = userOrderOfTeam;
	}
	
	PACKET_S_C_TEAM_CHANGE pack;

	pack.currOrderOfTeam = userOrderOfTeam;
	pack.prvOrderOfTeam = prvOrdereOfTeam;
	pack.currTeamType = roomUserInfo->teamType;
	
	return pack;
}

bool Room::CanChangeTeam(const RoomUserInfo* roomUserInfo)
{
	if (roomUserInfo->teamType == RED)
	{
		if (redTeamUserOrder.size() >= matchType)
			return false;
		return true;
	}

	if (blueTeamUserOrder.size() >= matchType)
		return false;

	return true;
}

void Room::ChangeRoomUserInfo(const PACKET_CHANGE_ROOM_OPTION* pack)
{
	unique_lock<shared_mutex> lock(mutex);

	name = pack->roomName;
	matchType = pack->matchType;
}


