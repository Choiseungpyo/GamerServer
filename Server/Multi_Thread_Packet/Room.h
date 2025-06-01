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
		if (isHost)
			inRoomUserState = IDLE;
		else
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

	// <userId, 팀내순서>
	unordered_map<int, int> redTeamUserOrder;
	unordered_map<int, int> blueTeamUserOrder;

	int no;
	string name;
	RoomState state;
	MatchType matchType;
	int hostId;

	Game game;

	mutable shared_mutex mutex;

public:
	Room(int no, RoomOption roomOption);
	~Room();

	int GetUserCount() const;
	int GetMaxUserCount() const;

	std::vector<int> GetAllClientId();

	PACKET_ROOM_USER_INFO GetPacketRoomUserInfo(int id);
	RoomUserInfo* GetRoomUserInfo(int id) const;

	int GetHostId() const;

	void SetNo(int no);
	int GetNo() const;

	void SetName(const string& name);
	const string& GetName() const;

	void SetRoomState(RoomState state);
	RoomState GetRoomState() const;

	void SetMatchType(MatchType matchType);
	MatchType GetMatchType() const;

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

	void SendToAllUserInRoom(vector<char> buffer);

	void SendToAllUserInRoom(const Packet* pack);
};

