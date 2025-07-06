#pragma once
#define MAXROOMNUM 8

class LobbyManager
{
	// int : Room No
	static unordered_map<int, Room*> roomMap; // 모든 방 정보
	static set<int> reusableRoomNos; // 삭제된 방 번호 정보

	// int : 소켓 번호
	static unordered_map<SOCKET, const ClientSession*> lobbyUserMap; // 로비에 있는 유저 정보
	static LobbyManager* instance;

	static Chat chat;

	static int GetNewRoomNo()
	{
		int id = (int)roomMap.size();
		if (reusableRoomNos.empty())
			return id;

		id = *reusableRoomNos.begin();
		reusableRoomNos.erase(reusableRoomNos.begin());
		return id;
	}

	static bool CanMakeRoom();

	static Room* GetJoinableRandomRoom();
	static void UpdateLobbyRoomInfo(const Room* room);

	static void UpdateInRoomInfo(const ClientSession* client, PTYPE type, Room* room, int roomNo);

public:
	LobbyManager() {}

	~LobbyManager();

	static LobbyManager* GetInstance();

	static void CreateRoom(const Packet* packet, const ClientSession* client);
	static void EntryRandomRoom(const ClientSession* client);
	static void EntryRoom(const Packet* packet, const ClientSession* client);

	static void SetReadyState(const ClientSession* client);
	static void SetTeamType(const ClientSession* client);
	static void SetRoomOption(const PACKET* packet, const ClientSession* client);
	
	static void EntryLobby(const ClientSession* client);
	
	static void UpdateLobbyAllRoomInfo();

	static void SendToAllLobbyUser(vector<char> buffer);
	static void SendToAllLobbyUser(const Packet* pack);

	static void SendLobbyUserProtile();
	static void UpdateLobbyUserProfile(const ClientSession* client);
	
	static void ExitLobby(const ClientSession* client);

	static void ExitRoom(const ClientSession* client);

	static void DeleteRoom(int roomId);

	static void SendMsgToLobby(const Packet* packet)
	{
		auto pack = (PACKET_CHAT*)packet;
		string msg = pack->msg;
		chat.AddMsg(msg);

		PACKET_CHAT* pack_chat = new PACKET_CHAT;
		pack_chat->Type = S_C_CHAT_LOBBY;
		pack_chat->Length = sizeof(PACKET_CHAT);
		strncpy_s(pack_chat->msg, sizeof(pack_chat->msg), msg.c_str(), _TRUNCATE);

		SendToAllLobbyUser(pack_chat);
	}

	static void SendMsgToRoom(const Packet* packet, const ClientSession* client)
	{
		auto pack = (PACKET_CHAT*)packet;
		string msg = pack->msg;
		auto user = client->GetUser();
		auto roomNo = user->GetRoomNo();
		Room* room;

		room = roomMap[roomNo];
		
		chat.AddMsg(msg);

		PACKET_CHAT* pack_chat = new PACKET_CHAT;
		pack_chat->Type = S_C_CHAT_ROOM;
		pack_chat->Length = sizeof(PACKET_CHAT);
		strncpy_s(pack_chat->msg, sizeof(pack_chat->msg), msg.c_str(), _TRUNCATE);

		room->SendToAllUserInRoom(pack);
	}

	
};

