#include "stdafx.h"
#include "LobbyManager.h"


// int : Room No
unordered_map<int, Room*> LobbyManager::roomMap; // ��� �� ����
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
/// �� ����
/// </summary>
/// <param name="client">Ŭ���̾�Ʈ</param>
/// <param name="roomOption">�� �ɼ�</param>
void  LobbyManager::CreateRoom(const Packet* packet, const ClientSession* client)
{
	Packet_c_s_create_room* pack = (Packet_c_s_create_room*)packet;
	RoomOption roomOption(pack->roomName, pack->matchType);

	Room* newRoom = new Room(roomMap.size(), roomOption);
	roomMap[roomMap.size()] = newRoom;

	newRoom->AddUser(client);
}

/// <summary>
/// ���� �� ����
/// </summary>
void  LobbyManager::EntryRandomRoom(const ClientSession* client)
{
	Room* randomRoom = GetJoinableRandomRoom();

	// ������ �� �ִ� ���� ���� ���
	if (!randomRoom)
		return;

	// �ش� �濡 ���� �߰�
	randomRoom->AddUser(client);
}

void LobbyManager::EntryRoom(const Packet* packet, const ClientSession* client)
{
	Packet_c_s_entry_room* pack = (Packet_c_s_entry_room*)packet;
	
	auto it = roomMap.find(pack->roomNo);

	// ���� �ִ� �� ��ȣ�� ���
	if (it != roomMap.end()) {
		(*it).second->AddUser(client);
	}
	// ���� �� ��ȣ�� ���
	else
		cout << "The Room Num(" << pack->roomNo << ") does not exist.";
}