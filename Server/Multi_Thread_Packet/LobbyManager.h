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

	static void Send_EntryRoomPacket(SOCKET targetSock,  int userId)
	{
		PACKET_S_C_ENTRY_LOBBY packet;
		packet.id = userId;
		strncpy_s(packet.name, "123", sizeof(packet.name));
		packet.name[sizeof(packet.name) - 1] = '\0';  // 꼭 종료문자 추가

		SessionManager::SendTo(&packet, targetSock);
	}
	static void Send_Lobby_Room_Users_Info(SOCKET targetSock)
	{
		//std::vector<char> buffer;
		//PACKET_S_C_ROOM_USERS_INFO_HEADER header;
		//header.roomId = 1001;
		//header.userCount = roomMap.size();
		//header.Length += sizeof(PACKET_ROOM_USER_INFO) * roomMap.size(); // 전체 패킷 크기

		//buffer.resize(header.Length);
		//memcpy(buffer.data(), &header, sizeof(header));

		//for (size_t i = 0; i < header.userCount; ++i)
		//{
		//	memcpy(buffer.data() + sizeof(header) + i * sizeof(PACKET_ROOM_USER_INFO),
		//		&users[i], sizeof(PACKET_ROOM_USER_INFO));
		//}

		//send(targetSock, buffer.data(), buffer.size(), 0);
	}

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

	static void EntryRoom(const Packet* packet, const ClientSession* client);

	static void SetReadyState(const ClientSession* client);
	static void SetTeamType(const ClientSession* client);
	static void SetRoomOption(const ClientSession* client, PACKET_S_C_CHANGE_ROOM_OPTION pack);
};

