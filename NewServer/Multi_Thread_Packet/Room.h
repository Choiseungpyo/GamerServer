#pragma once

struct RoomUserData {
	int userId;
	char userName[NAME_SIZE];
	TeamType teamType;
	unsigned char isReady;
	unsigned char isHost;
};

struct RoomUserInfo {
	TeamType teamType;
	InRoomUserState inRoomUserState;
	bool isHost;
	int userOrderOfTeam;

	RoomUserInfo(bool isHost, TeamType teamType, int userOrderOfTeam)
	{
		inRoomUserState = UNREADY;

		this->teamType = teamType;
		this->isHost = isHost;
		this->userOrderOfTeam = userOrderOfTeam;
	}
};

struct RoomOption
{
	string roomName;
	MatchType matchType;

	RoomOption(const string& roomName, MatchType matchType)
	{
		this->roomName = roomName;
		this->matchType = matchType;
	}
};

class Room
{
	// int : id값
	unordered_map<int, const ClientSession*> clientMap;
	unordered_map<int, RoomUserInfo*> roomUserInfoMap;

	// index : 팀 내 순서	값 : userId
	vector<int> redTeamUserOrder;
	vector<int> blueTeamUserOrder;

	int no;
	string name;
	RoomState state;
	MatchType matchType;
	int hostId;
	int readyNum;

	const Game* game;

public:
	Room(int no, RoomOption roomOption, int hostId, const ClientSession* client);
	~Room();

	int GetUserCount() const;
	int GetMaxUserCount() const;

	std::vector<int> GetAllClientId();

	int GetRedTeamUserOrder(int userId) const;
	int GetBlueTeamUserOrder(int userId) const;

	PACKET_ROOM_USER_INFO GetPacketRoomUserInfo(int id);
	RoomUserInfo* GetRoomUserInfo(int id) const;

	void CreateNewGame();

	int GetHostId() const;

	void SetNo(int no);
	int GetNo() const;

	void SetName(const string& name);
	const string& GetName() const;

	void SetRoomState(RoomState state);
	RoomState GetRoomState() const;

	void SetMatchType(MatchType matchType);
	MatchType GetMatchType() const;

	bool IsAllReady()
	{
		return readyNum == matchType * 2;
	}

	const RoomUserInfo* ChangeInRoomUserState(int userId);

	PACKET_S_C_TEAM_CHANGE ChangeTeamType(RoomUserInfo* roomUserInfo, int userId);
	bool CanChangeTeam(const RoomUserInfo* pack);

	void ChangeRoomUserInfo(const PACKET_CHANGE_ROOM_OPTION* pack);

	void AddUser(const ClientSession* client);

	/// <summary>
	/// 유저 삭제
	/// </summary>
	/// <param name="userId"></param>
	/// <returns>방에 남은 유저 수</returns>
	int DeleteUser(int userId);

	tuple<TeamType, int> JoinAvailableTeam(int clientId);

	bool CanJoinRoom();

	void Send_InRoom_UsersData();

	void SendToAllUserInRoom(vector<char> buffer) const;

	void SendToAllUserInRoom(const Packet* pack) const;

	const unordered_map<int, RoomUserInfo*>& GetRoomUserInfoMap() const
	{
		return roomUserInfoMap;
	}

	const unordered_map<int, const ClientSession*>& GetClientMap() const
	{
		return clientMap;
	}

};

