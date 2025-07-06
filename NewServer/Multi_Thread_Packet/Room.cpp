#include "stdafx.h"

Room::Room(int no, RoomOption roomOption, int hostId, const ClientSession* client)
	:no(no), name(roomOption.roomName), state(RoomState::WAITING), 
	matchType(roomOption.matchType), hostId(hostId), game(nullptr), readyNum(0)
{
	AddUser(client);
}

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
	return (int)clientMap.size();
};

int Room::GetMaxUserCount() const
{
	return (int)matchType * 2;
};

std::vector<int> Room::GetAllClientId()
{
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
	state = PLAYING;

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
	return hostId;
}

void Room::SetNo(int no)
{
	this->no = no;
}

int Room::GetNo() const
{
	return no;
}

void Room::SetName(const string& name)
{
	this->name = name;
}

const string& Room::GetName() const
{
	return name;
}

void Room::SetRoomState(RoomState state)
{
	this->state = state;
}

RoomState Room::GetRoomState() const
{
	return state;
}

void Room::SetMatchType(MatchType matchType)
{
	this->matchType = matchType;
}

MatchType Room::GetMatchType() const
{
	return matchType;
}

void Room::AddUser(const ClientSession* client)
{
	User* user = client->GetUser();

	int clientId = user->GetId();
	
	clientMap[clientId] = client;

	auto data = JoinAvailableTeam(clientId);

	TeamType team = std::get<0>(data);
	int orderOfTeam = std::get<1>(data);

	bool isHost = clientMap.size() == 1;
	if (isHost)
		hostId = user->GetId();
	roomUserInfoMap[clientId] = new RoomUserInfo(isHost, team, orderOfTeam);

	user->SetState(ROOM);

	cout << "User" << clientId << "has entered the room.";
}

/// <summary>
/// 유저 삭제
/// </summary>
/// <param name="userId"></param>
/// <returns>방에 남은 유저 수</returns>
int Room::DeleteUser(int userId)
{
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

	for (auto& item : clientMap)
		clients.push_back(item.second);

	for (auto client : clients)
	{
		send(client->GetSocket(), buffer.data(), (int)buffer.size(), 0);
	}
}

void Room::SendToAllUserInRoom(const Packet* pack) const
{
	vector<const ClientSession*> clients;

	for (auto& item : clientMap)
		clients.push_back(item.second);

	for (auto client : clients)
	{
		client->Send(pack);
	}
}

const RoomUserInfo* Room::ChangeInRoomUserState(int userId)
{
	auto roomUserInfo = roomUserInfoMap[userId];
	if (roomUserInfo->isHost)
	{
		// 모두 준비하지 않은 경우
		if (readyNum < 2 * matchType - 1)
			return roomUserInfo;
	}

	if (roomUserInfo->inRoomUserState == UNREADY)
	{
		roomUserInfo->inRoomUserState = READY;
		readyNum++;
	}
	else
	{
		roomUserInfo->inRoomUserState = UNREADY;
		readyNum--;
	}
	return roomUserInfo;
}

PACKET_S_C_TEAM_CHANGE Room::ChangeTeamType(RoomUserInfo* roomUserInfo, int userId)
{
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
	name = pack->roomName;
	matchType = pack->matchType;
}


