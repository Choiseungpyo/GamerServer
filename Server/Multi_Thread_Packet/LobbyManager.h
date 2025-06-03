#pragma once
#define MAXROOMNUM 8

class LobbyManager
{
	// int : Room No
	static unordered_map<int, Room*> roomMap; // 모든 방 정보

	// int : 소켓 번호
	static unordered_map<SOCKET, const ClientSession*> lobbyUserMap; // 로비에 있는 유저 정보
	static LobbyManager* instance;

	static shared_mutex mutex;

	static bool CanMakeRoom();

	static Room* GetJoinableRandomRoom();
	static void UpdateLobbyRoomInfo(const ClientSession* client, const Room* room);

	static void UpdateInRoomInfo(const ClientSession* client, PTYPE type, Room* room, int roomNo);

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
	static void SetRoomOption(const PACKET* packet, const ClientSession* client);
	
	static void EntryLobby(const ClientSession* client);
	
	static void UpdateLobbyAllRoomInfo();

	static void SendToAllLobbyUser(vector<char> buffer);
	static void SendToAllLobbyUser(const Packet* pack);
	
	static void ExitLobby(const ClientSession* client);

	static void ExitRoom(const ClientSession* client);

	static void DeleteRoom(int roomId);

	static void RegisterNewGame(const ClientSession* client)
	{
		auto user = client->GetUser();
		auto roomNo = user->GetRoomNo();

		auto it = roomMap.find(roomNo);
		
		if(it == roomMap.end())
		{
			cout << "roomMap - Invalid roomNo : " << roomNo << endl;
			return;
		}

		auto currRoom = it->second;
		currRoom->CreateNewGame();
	}
};

