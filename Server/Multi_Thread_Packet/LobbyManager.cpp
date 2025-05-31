#include "stdafx.h"

// int : Room No
unordered_map<int, Room*> LobbyManager::roomMap; // 모든 방 정보
unordered_map<int, const ClientSession*> LobbyManager::lobbyUserMap; // 로비에 있는 유저 정보
LobbyManager* LobbyManager::instance = GetInstance();
shared_mutex LobbyManager::mutex;

bool LobbyManager::CanMakeRoom()
{
	shared_lock<shared_mutex> lock(mutex);
	if (roomMap.size() >= MAXROOMNUM)
		return false;
	return true;
}

Room* LobbyManager::GetJoinableRandomRoom()
{
	shared_lock<shared_mutex> lock(mutex);

	vector<int> joinableRoomNums;
	int joiableRoomMaxnum = 0;

	for (auto& room : roomMap)
	{
		if (!room.second->CanJoinRoom())
			continue;

		joinableRoomNums.push_back(room.first);
	}

	joiableRoomMaxnum = (int)joinableRoomNums.size();

	if (joiableRoomMaxnum <= 0)
		return nullptr;

	int randomIndex = rand() % joiableRoomMaxnum;  // 벡터 인덱스
	int randomRoomNum = joinableRoomNums[randomIndex];  // 실제 방 번호

	return roomMap[randomRoomNum];
}

LobbyManager::~LobbyManager()
{
	unique_lock<shared_mutex> lock(mutex);

	for (auto& room : roomMap)
	{
		delete room.second;
	}
	roomMap.clear();
}

LobbyManager* LobbyManager::GetInstance()
{
	// shared_mutex 대신 맨 처음에만 초기화하는 용도로 once_flag  사용
	static std::once_flag flag;
	std::call_once(flag, []() {
		instance = new LobbyManager();
		});
	return instance;
}

/// <summary>
/// 방 생성
/// </summary>
/// <param name="client">클라이언트</param>
/// <param name="roomOption">방 옵션</param>
void  LobbyManager::CreateRoom(const Packet* packet, const ClientSession* client)
{
	const Packet_c_s_create_room* pack = (Packet_c_s_create_room*)packet;
	string roomName = pack->roomName;
	RoomOption roomOption(roomName, pack->matchType);

	int roomNo = roomMap.size();
	Room* newRoom = new Room(roomNo, roomOption);

	{
		unique_lock<shared_mutex> lock(mutex);
		roomMap[roomMap.size()] = newRoom;
	}
	
	EntryRoom(client, S_C_ENTRY_ROOM ,newRoom, roomNo);

	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap.erase(client->GetSocket());
	}
}

/// <summary>
/// 랜덤 방 입장
/// </summary>
void  LobbyManager::EntryRandomRoom(const ClientSession* client)
{
	Room* randomRoom = GetJoinableRandomRoom();

	// 입장할 수 있는 방이 없을 경우
	if (!randomRoom)
		return;

	// 해당 방에 유저 추가
	EntryRoom(client, S_C_ENTRY_RANDOMROOM,randomRoom, randomRoom->GetNo());
	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap.erase(client->GetSocket());
	}
}

void LobbyManager::EntryRoom(const Packet* packet, const ClientSession* client)
{	
	unique_lock<shared_mutex> lock(mutex);
	const Packet_c_s_entry_room* pack = (Packet_c_s_entry_room*)packet;
	const auto it = roomMap.find(pack->roomNo);

	// 없는 방 번호인 경우
	if (it != roomMap.end()) {
		cout << "The Room Num(" << pack->roomNo << ") does not exist.";
	}
	
	EntryRoom(client, S_C_ENTRY_ROOM, (*it).second, (*it).first);
	lobbyUserMap.erase(client->GetSocket());
}

void LobbyManager::EntryRoom(const ClientSession* client, PTYPE type, Room* room, int roomNo)
{
	string roomName = room->GetName();

	room->AddUser(client);

	User* user = client->GetUser();

	vector<char> buffer;
	Packet_RoomUsersHeader header;
	header.Type = type;
	header.hostId = user->GetId();
	header.userCount = room->GetUserCount();
	strncpy_s(header.roomName, sizeof(header.roomName), roomName.c_str(), _TRUNCATE);

	header.Length = sizeof(Packet_RoomUsersHeader) + sizeof(PACKET_ROOM_USER_INFO);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(header));

	size_t offset = sizeof(Packet_RoomUsersHeader);
	vector<const ClientSession*> allClients = room->GetAllClients();
	for (auto client : allClients)
	{
		auto user = client->GetUser();

		PACKET_ROOM_USER_INFO userInfo(user->GetName());
		memcpy(buffer.data() + offset, &userInfo, sizeof(PACKET_ROOM_USER_INFO));
		offset += sizeof(PACKET_ROOM_USER_INFO);
	}

	send(client->GetSocket(), buffer.data(), buffer.size(), 0);
	UpdateLobbyRoomInfo(client, room);
}

void LobbyManager::SetReadyState(const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);

	User* user = client->GetUser();
	roomMap[user->GetRoomNum()]->ChangeReadyState(user->GetId());
}

void LobbyManager::SetTeamType(const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);

	User* user = client->GetUser();
	roomMap[user->GetRoomNum()]->ChangeTeamType(user->GetId());
}

void LobbyManager::SetRoomOption(const PACKET* packet, const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);
	PACKET_CHANGE_ROOM_OPTION* pack = (PACKET_CHANGE_ROOM_OPTION*)packet;

	User* user = client->GetUser();
	Room* room = roomMap[pack->roomNo];
	room->ChangeRoomUserInfo(pack);
	UpdateLobbyRoomInfo(client, room);
}

void LobbyManager::EntryLobby(const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);

	lobbyUserMap[client->GetSocket()] = client;
	User* currUser = client->GetUser();
	currUser->SetState(LOBBY);

	vector<char> buffer;
	PACKET_S_C_LOBBY_USERS_INFO_HEADER header;
	header.userCount = SessionManager::GetClientSize();

	header.Length = sizeof(PACKET_S_C_LOBBY_USERS_INFO_HEADER) + sizeof(PACKET_LOBBY_USERS_INFO);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(header));

	size_t offset = sizeof(PACKET_S_C_LOBBY_USERS_INFO_HEADER);
	vector<const ClientSession*> allClients = SessionManager::GetClientAll();
	for (auto client : allClients)
	{
		auto user = client->GetUser();

		PACKET_LOBBY_USERS_INFO userInfo(user->GetName());
		memcpy(buffer.data() + offset, &userInfo, sizeof(PACKET_LOBBY_USERS_INFO));
		offset += sizeof(PACKET_LOBBY_USERS_INFO);
	}

	send(client->GetSocket(), buffer.data(), buffer.size(), 0);
}

// 현재 로비에 있는 모든 클라한테 로비 방 정보 UI 업데이트할 수 있도록 뿌림
void LobbyManager::UpdateLobbyRoomInfo(const ClientSession* client, const Room* room)
{
	for (auto item : lobbyUserMap)
	{
		PACKET_S_C_UPDATE_LOBBY_ROOM_INFO pack;
		pack.roomNo = room->GetNo();
		strncpy_s(pack.roomName, sizeof(pack.roomName), room->GetName().c_str(), _TRUNCATE);
		pack.currNumOfPeople = room->GetUserCount();
		pack.maxNumOfPeople = room->GetMaxUserCount();
		pack.roomState = room->GetRoomState();

		item.second->Send(&pack);
	}
}