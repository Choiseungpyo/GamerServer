#pragma once

#include "Room.h"
#define MAXROOMNUM 8


class LobbyManager
{
	// int : Room No
	static unordered_map<int, Room*> roomMap; // 모든 방 정보
	static LobbyManager* instance;

	static bool CanMakeRoom();

	static Room* GetJoinableRandomRoom();

public:
	LobbyManager() {}

	~LobbyManager();

	static LobbyManager* GetInstance();

	/// <summary>
	/// 방 생성
	/// </summary>
	/// <param name="client">클라이언트</param>
	/// <param name="roomOption">방 옵션</param>
	static void CreateRoom(const Packet* packet, const ClientSession* client);

	/// <summary>
	/// 랜덤 방 입장
	/// </summary>
	static void EntryRandomRoom(const ClientSession* client);

	static void EntryRoom(const Packet* pack, const ClientSession* client);
};

