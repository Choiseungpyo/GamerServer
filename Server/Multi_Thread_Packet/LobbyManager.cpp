#include "stdafx.h"
#include "LobbyManager.h"


// int : Room No
unordered_map<int, Room*> LobbyManager::roomMap; // 모든 방 정보
LobbyManager* LobbyManager::instance = GetInstance();

bool LobbyManager::CanMakeRoom()
{
	if (roomMap.size() >= MAXROOMNUM)
		return false;
	return true;
}

Room* LobbyManager::GetJoinableRandomRoom()
{
	vector<int> joinableRoomNums;
	int joiableRoomMaxnum = 0;

	for (auto& room : roomMap)
	{
		if (!room.second->CanJoinRoom())
			continue;

		joinableRoomNums.push_back(room.first);
	}

	joiableRoomMaxnum = joinableRoomNums.size();

	if (joiableRoomMaxnum <= 0)
		return nullptr;

	int randomRoomNum = rand() % joiableRoomMaxnum;

	return roomMap[randomRoomNum];
}

LobbyManager::~LobbyManager()
{
	for (auto& room : roomMap)
	{
		delete room.second;
	}
	roomMap.clear();
}

LobbyManager* LobbyManager::GetInstance()
{
	if (!instance)
		return new LobbyManager();

	return instance;
}

/// <summary>
/// 방 생성
/// </summary>
/// <param name="client">클라이언트</param>
/// <param name="roomOption">방 옵션</param>
void  LobbyManager::CreateRoom(const Packet* packet, const ClientSession* client)
{
	Packet_c_s_create_room* pack = (Packet_c_s_create_room*)packet;
	RoomOption roomOption(pack->roomName, pack->matchType);

	Room* newRoom = new Room(roomMap.size(), roomOption);
	roomMap[roomMap.size()] = newRoom;

	newRoom->AddUser(client);
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
	randomRoom->AddUser(client);
}

void LobbyManager::EntryRoom(const Packet* packet, const ClientSession* client)
{
	Packet_c_s_entry_room* pack = (Packet_c_s_entry_room*)packet;
	
	auto it = roomMap.find(pack->roomNo);

	// 실제 있는 방 번호인 경우
	if (it != roomMap.end()) {
		(*it).second->AddUser(client);
	}
	// 없는 방 번호인 경우
	else
		cout << "The Room Num(" << pack->roomNo << ") does not exist.";
}