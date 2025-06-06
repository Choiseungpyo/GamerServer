#include "stdafx.h"

unordered_map<int, Room*> LobbyManager::roomMap; // 모든 방 정보
unordered_map<SOCKET, const ClientSession*> LobbyManager::lobbyUserMap; // 로비에 있는 유저 정보
LobbyManager* LobbyManager::instance = GetInstance();
shared_mutex LobbyManager::mutex;
Chat LobbyManager::chat;

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

	delete instance;
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
	auto user = client->GetUser();

	int roomNo = (int)roomMap.size();
	Room* newRoom = new Room(roomNo, roomOption, user->GetId());

	{
		unique_lock<shared_mutex> lock(mutex);

		auto it = roomMap.find(roomNo);

		// 이미 방이 생성된 경우
		if (it != roomMap.end())
		{
			cout << "roomMap[" << roomNo << "] is already created" << endl;
			return;
		}

		roomMap[roomNo] = newRoom;
		newRoom->AddUser(client);
	}
	
	UpdateInRoomInfo(client, S_C_INROOM_INFO, newRoom, roomNo);
	
	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap.erase(client->GetSocket());
		
	}

	UpdateLobbyRoomInfo(newRoom);
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

	randomRoom->AddUser(client);


	UpdateInRoomInfo(client, S_C_INROOM_INFO, randomRoom, randomRoom->GetNo());
	
	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap.erase(client->GetSocket());
	}
		
	UpdateLobbyRoomInfo(randomRoom);	
}

void LobbyManager::EntryRoom(const Packet* packet, const ClientSession* client)
{	
	Packet_c_s_entry_room* pack = (Packet_c_s_entry_room*)packet;

	Room* room = nullptr;
	int roomNo = 0;

	{
		unique_lock<shared_mutex> lock(mutex);

		const auto it = roomMap.find(pack->roomNo);
		if (it == roomMap.end()) {
			cout << "roomMap - Invalid roomNo : " << pack->roomNo << endl;
			return;
		}

		room = it->second;
		roomNo = it->first;

		if (!room->CanJoinRoom())
			return;

		room->AddUser(client);
	}

	UpdateInRoomInfo(client, S_C_INROOM_INFO, room, roomNo);

	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap.erase(client->GetSocket());
	}

	UpdateLobbyRoomInfo(room);
}

void LobbyManager::UpdateInRoomInfo(const ClientSession* client, PTYPE type, Room* room, int roomNo)
{
	string roomName = room->GetName();
	User* user = client->GetUser();
	user->SetCurrRoomNum(roomNo);

	vector<char> buffer;
	Packet_RoomUsersHeader header;
	header.Type = type;
	header.hostId = room->GetHostId();
	header.roomNo = roomNo;
	header.matchType = room->GetMatchType();
	header.userCount = room->GetUserCount();
	strncpy_s(header.roomName, sizeof(header.roomName), roomName.c_str(), _TRUNCATE);

	header.Length = sizeof(Packet_RoomUsersHeader) + header.userCount * sizeof(PACKET_ROOM_USER_INFO);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(header));

	size_t offset = sizeof(Packet_RoomUsersHeader);
	auto allClientId = room->GetAllClientId();
	for (auto id : allClientId)
	{
		auto userInfo = room->GetPacketRoomUserInfo(id);
		memcpy(buffer.data() + offset, &userInfo, sizeof(PACKET_ROOM_USER_INFO));
		offset += sizeof(PACKET_ROOM_USER_INFO);
	}

	room->SendToAllUserInRoom(buffer);
}

void LobbyManager::SetReadyState(const ClientSession* client)
{
	User* user = client->GetUser();
	int roomNum = user->GetRoomNo();
	Room* room;
	{
		shared_lock<shared_mutex> lock(mutex);
		room = roomMap[roomNum];
	}
	int userId = user->GetId();
	auto roomUserInfo = room->ChangeInRoomUserState(userId);

	// 방장이 시작했을 경우
	if (roomUserInfo->inRoomUserState == START)
	{
		room->CreateNewGame();
		UpdateLobbyRoomInfo(room);
	}
	else
	{
		PACKET_S_C_CHANGE_INROOM_USERSTATE pack;
		pack.orderOfTeam = roomUserInfo->userOrderOfTeam;
		pack.inRoomUserState = roomUserInfo->inRoomUserState;
		pack.teamType = roomUserInfo->teamType;


		// 버튼 상태는 자기 자신한테만 보내기
		client->Send(&pack);
		// 유저 상태는 해당 방 전체에 보내기
		UpdateInRoomInfo(client, S_C_INROOM_INFO, room, roomNum);
	}
}

void LobbyManager::SetTeamType(const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);


	User* user = client->GetUser();
	int id = user->GetId();
	Room* room = roomMap[user->GetRoomNo()];
	auto roomUserInfo = room->GetRoomUserInfo(id);

	// 팀을 바꿀 수 없는 경우
	if (!room->CanChangeTeam(roomUserInfo))
		return;

	auto pack = room->ChangeTeamType(roomUserInfo, user->GetId());
	
	room->SendToAllUserInRoom(&pack);
}

void LobbyManager::SetRoomOption(const PACKET* packet, const ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);
	PACKET_CHANGE_ROOM_OPTION* pack = (PACKET_CHANGE_ROOM_OPTION*)packet;

	User* user = client->GetUser();
	int roomNum = user->GetRoomNo();
	Room* room = roomMap[roomNum];
	room->ChangeRoomUserInfo(pack);

	pack->Type = S_C_CHANGE_ROOM_OPTION;
	strncpy_s(pack->roomName, sizeof(pack->roomName), room->GetName().c_str(), _TRUNCATE);
	pack->matchType = room->GetMatchType();
	pack->roomNo = room->GetNo();

	room->SendToAllUserInRoom(pack);

	UpdateLobbyRoomInfo(room);
}

void LobbyManager::EntryLobby(const ClientSession* client)
{
	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap[client->GetSocket()] = client;
	}
	
	UpdateLobbyUserProfile(client);
	UpdateLobbyAllRoomInfo();
}

void LobbyManager::SendLobbyUserProtile()
{
	vector<const ClientSession*> allClients = SessionManager::GetClientAll();

	int userSize = (int)allClients.size();
	vector<char> buffer;
	PACKET_INFO_HEADER header(userSize);
	header.Type = S_C_USERS_PROFILE;

	header.Length = sizeof(PACKET_INFO_HEADER) + userSize * sizeof(PACKET_LOBBY_USERS_INFO);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(header));

	size_t offset = sizeof(PACKET_INFO_HEADER);

	// 전체 유저만큼 데이터 붙이기
	for (auto client : allClients)
	{
		auto user = client->GetUser();

		PACKET_LOBBY_USERS_INFO userInfo;
		userInfo.userId = user->GetId();
		strncpy_s(userInfo.userName, sizeof(userInfo.userName), user->GetName(), _TRUNCATE);
		memcpy(buffer.data() + offset, &userInfo, sizeof(PACKET_LOBBY_USERS_INFO));
		offset += sizeof(PACKET_LOBBY_USERS_INFO);
	}

	// 로비에 있는 유저들한테 전부 보내기
	SendToAllLobbyUser(buffer);
}

void LobbyManager::UpdateLobbyUserProfile(const ClientSession* client)
{
	User* currUser = client->GetUser();
	currUser->SetState(LOBBY);

	SendLobbyUserProtile();
}

void LobbyManager::SendToAllLobbyUser(vector<char> buffer)
{
	vector<SOCKET> sockets;

	{
		shared_lock<shared_mutex> lock(mutex);
		for (auto item : lobbyUserMap)
		{
			sockets.push_back(item.first);
		}
	}

	for (auto socket : sockets)
		send(socket, buffer.data(), (int)buffer.size(), 0);
}

void LobbyManager::SendToAllLobbyUser(const Packet* pack)
{
	vector<const ClientSession*> clients;

	{
		shared_lock<shared_mutex> lock(mutex);
		for (auto item : lobbyUserMap)
		{
			clients.push_back(item.second);
		}
	}

	for (auto client : clients)
		client->Send(pack);
}

// 방정보 하나만을 수정했을 경우 - 현재 로비에 있는 모든 클라한테 로비 방 정보 UI 업데이트할 수 있도록 뿌림
void LobbyManager::UpdateLobbyRoomInfo(const Room* room)
{
	vector<char> buffer;
	PACKET_INFO_HEADER header(1);
	header.Type = S_C_LOBBY_ROOM_INFO;

	header.Length = sizeof(PACKET_INFO_HEADER) + sizeof(PACKET_S_C_UPDATE_LOBBY_ROOM_INFO);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(header));

	size_t offset = sizeof(PACKET_INFO_HEADER);

	PACKET_S_C_UPDATE_LOBBY_ROOM_INFO pack;
	pack.roomNo = room->GetNo();
	strncpy_s(pack.roomName, sizeof(pack.roomName), room->GetName().c_str(), _TRUNCATE);
	pack.currNumOfPeople = room->GetUserCount();
	pack.maxNumOfPeople = room->GetMaxUserCount();
	pack.roomState = room->GetRoomState();

	memcpy(buffer.data() + offset, &pack, sizeof(PACKET_S_C_UPDATE_LOBBY_ROOM_INFO));

	SendToAllLobbyUser(buffer);
}

// 모든 방정보를 전달할 경우 - 현재 로비에 있는 모든 클라한테 모든 로비 방 정보 UI 업데이트할 수 있도록 뿌림
void LobbyManager::UpdateLobbyAllRoomInfo()
{
	vector<char> buffer;
	PACKET_INFO_HEADER header((int)roomMap.size());
	header.Type = S_C_LOBBY_ALL_ROOM_INFO;
	header.Length = sizeof(PACKET_INFO_HEADER) + header.count * sizeof(PACKET_S_C_UPDATE_LOBBY_ROOM_INFO);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(header));

	size_t offset = sizeof(PACKET_INFO_HEADER);

	// 헤더에 생성된 방만큼 붙이기
	for (auto roomitem : roomMap)
	{
		Room* newRoom = roomitem.second;

		PACKET_S_C_UPDATE_LOBBY_ROOM_INFO pack;
		pack.roomNo = newRoom->GetNo();
		strncpy_s(pack.roomName, sizeof(pack.roomName), newRoom->GetName().c_str(), _TRUNCATE);
		pack.currNumOfPeople = newRoom->GetUserCount();
		pack.maxNumOfPeople = newRoom->GetMaxUserCount();
		pack.roomState = newRoom->GetRoomState();

		memcpy(buffer.data() + offset, &pack, sizeof(PACKET_S_C_UPDATE_LOBBY_ROOM_INFO));
		offset += sizeof(PACKET_S_C_UPDATE_LOBBY_ROOM_INFO);
	}

	SendToAllLobbyUser(buffer);
}

void LobbyManager::ExitLobby(const ClientSession* client)
{
	
	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap.erase(client->GetSocket());
	}
	
	client->Send(S_C_EXIT_LOBBY);

	PACKET pack;
	pack.Type = S_C_EXIT_ROOM;

	SendToAllLobbyUser(&pack);
}

void LobbyManager::ExitRoom(const ClientSession* client)
{
	int roomNum = 0;
	Room* room = nullptr;

	auto user = client->GetUser();
	roomNum = user->GetRoomNo();
	
	{
		shared_lock<shared_mutex> lock(mutex);

		auto it = roomMap.find(roomNum);
		if (it == roomMap.end())
		{
			cout << "ExitRoom - Invalid roomNum: " << roomNum << endl;
			return;
		}

		room = it->second;
	}

	int remainingUserNum = -1;
	if(room != nullptr)
		remainingUserNum = room->DeleteUser(user->GetId());

	// 마지막 유저가 방에서 나갈 경우
	if (remainingUserNum == 0)
	{
		DeleteRoom(roomNum);
		room = nullptr; // 삭제된 방은 nullptr 처리
	}

	{
		unique_lock<shared_mutex> lock(mutex);
		lobbyUserMap[client->GetSocket()] = client;
	}

	user->SetCurrRoomNum(-1);

	// 자기 자신이 방에서 나가는건 자기한테만 보내기
	client->Send(S_C_EXIT_ROOM);

	if (room != nullptr)
	{
		// 현재 방에 있는 유저들한테 최신화
		UpdateInRoomInfo(client, S_C_INROOM_INFO, room, roomNum);
	}

	// 로비에 있는 유저들한테 최신화
	UpdateLobbyAllRoomInfo();
}

void LobbyManager::DeleteRoom(int roomId)
{
	unique_lock<shared_mutex> lock(mutex);
	auto it = roomMap.find(roomId);
	if (it == roomMap.end())
	{
		cout << "Delete Room - Invalid roomId : " << roomId << endl;
		return;
	}

	delete it->second;
	roomMap.erase(it);
}