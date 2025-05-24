#pragma once

#include "Room.h"
#define MAXROOMNUM 8


class LobbyManager
{
	// int : Room No
	unordered_map<int, Room*> roomMap; // 모든 방 정보
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
	/// 방 생성
	/// </summary>
	/// <param name="client">클라이언트</param>
	/// <param name="roomOption">방 옵션</param>
	void CreateRoom(const ClientSession* client, RoomOption roomOption)
	{
		Room* newRoom = new Room(roomMap.size(), roomOption);
		roomMap[roomMap.size()] = newRoom;

		newRoom->AddUser(client);
	}

	/// <summary>
	/// 랜덤 방 입장
	/// </summary>
	void EntryRandomRoom(const ClientSession* client)
	{
		Room* randomRoom = GetJoinableRandomRoom();

		// 입장할 수 있는 방이 없을 경우
		if (!randomRoom)
			return;

		// 해당 방에 유저 추가
		randomRoom->AddUser(client);
	}
};

