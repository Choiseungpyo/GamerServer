#pragma once
#include <unordered_map>
#include "Game.h"
#include <unordered_set>
#include "Packet.h"

class ClientSession;
class User;

enum TeamType {
	RED,
	BLUE
};

enum RoomState {
	WAITING,
	PLAYING
};

enum MatchType {
	SOLO = 1,
	DUO = 2,
	SQUAD = 4
};

enum ReadyState {
	UNREADY,
	READY
};

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

	RoomUserInfo()
	{
		this->teamType = RED;
		readyState = UNREADY;
		isHost = true;
	}

	RoomUserInfo(TeamType teamType)
	{
		this->teamType = teamType;
		readyState = UNREADY;
		isHost = false;
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

	void ChangeReadyState(int userId)
	{
		if (roomUserInfoMap[userId].readyState == UNREADY)
			roomUserInfoMap[userId].readyState = READY;
		else
			roomUserInfoMap[userId].readyState = UNREADY;
	}

	void ChangeTeamType(int userId)
	{
		if (roomUserInfoMap[userId].teamType == RED)
			roomUserInfoMap[userId].teamType = BLUE;
		else
			roomUserInfoMap[userId].teamType = RED;
	}


	void AddUser(const ClientSession* client);

	TeamType JoinAvailableTeam(int clientId);

	bool CanJoinRoom();


	void Send_InRoom_UsersData();




};

