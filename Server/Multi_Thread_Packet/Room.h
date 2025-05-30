#pragma once
#include "stdafx.h"



struct RoomUserData {
	int userId;
	char userName[20];
	TeamType teamType;
	unsigned char isReady;
	unsigned char isHost;
};

struct RoomUserInfo {
	TeamType teamType;
	ReadyState readyState;
	bool isHost;
	int userOrderOfTeam;

	RoomUserInfo()
	{
		this->teamType = RED;
		readyState = UNREADY;
		isHost = true;
		userOrderOfTeam = 0;
	}

	RoomUserInfo(TeamType teamType)
	{
		this->teamType = teamType;
		readyState = UNREADY;
		isHost = false;
		userOrderOfTeam = 0;
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
	// int : id°ª
	unordered_map<int, const ClientSession*> clientMap;
	unordered_map<int, RoomUserInfo> roomUserInfoMap;
	unordered_set<int> redTeamIds;
	unordered_set<int> blueTeamIds;

	int no;
	string name;
	RoomState state;
	MatchType matchType;

	Game game;


public:
	Room(int no, const RoomOption& roomOption);
	~Room();

	void SetNo(int no) { this->no = no; }

	void SetName(const string& name) { this->name = name; }

	void ChangeReadyState(int userId);

	void ChangeTeamType(int userId);

	void ChangeRoomUserInfo(PACKET_CHANGE_ROOM_OPTION* pack);

	void AddUser(const ClientSession* client);

	TeamType JoinAvailableTeam(int clientId);

	bool CanJoinRoom();


	void Send_InRoom_UsersData();
};

