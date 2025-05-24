#pragma once

#include "Room.h"
#define MAXROOMNUM 8


class LobbyManager
{
	// int : Room No
	unordered_map<int, Room*> roomMap; // ��� �� ����
	static LobbyManager* instance;

	bool CanMakeRoom()
	{
		if (roomMap.size() >= MAXROOMNUM)
			return false;
		return true;
	}

	Room* GetJoinableRandomRoom()
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

public:
	LobbyManager() {}

	~LobbyManager()
	{
		for (auto& room : roomMap)
		{
			delete room.second;
		}
		roomMap.clear();
	}

	static LobbyManager* GetInstance()
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
	void CreateRoom(const ClientSession* client, RoomOption roomOption)
	{
		Room* newRoom = new Room(roomMap.size(), roomOption);
		roomMap[roomMap.size()] = newRoom;

		newRoom->AddUser(client);
	}

	/// <summary>
	/// ���� �� ����
	/// </summary>
	void EntryRandomRoom(const ClientSession* client)
	{
		Room* randomRoom = GetJoinableRandomRoom();

		// ������ �� �ִ� ���� ���� ���
		if (!randomRoom)
			return;

		// �ش� �濡 ���� �߰�
		randomRoom->AddUser(client);
	}
};

