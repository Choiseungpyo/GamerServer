#pragma once

#include "Room.h"
#define MAXROOMNUM 8


class LobbyManager
{
	// int : Room No
	static unordered_map<int, Room*> roomMap; // ��� �� ����
	static LobbyManager* instance;

	static bool CanMakeRoom();

	static Room* GetJoinableRandomRoom();

public:
	LobbyManager() {}

	~LobbyManager();

	static LobbyManager* GetInstance();

	/// <summary>
	/// �� ����
	/// </summary>
	/// <param name="client">Ŭ���̾�Ʈ</param>
	/// <param name="roomOption">�� �ɼ�</param>
	static void CreateRoom(const Packet* packet, const ClientSession* client);

	/// <summary>
	/// ���� �� ����
	/// </summary>
	static void EntryRandomRoom(const ClientSession* client);

	static void EntryRoom(const Packet* pack, const ClientSession* client);
};

