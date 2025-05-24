#pragma once
#include <unordered_map>
#include "Game.h"
#include <unordered_set>

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

struct RoomUserInfo {
	TeamType teamType;
	bool isReady;

	RoomUserInfo()
	{
		this->teamType = RED;
		isReady = false;
	}

	RoomUserInfo(TeamType teamType)
	{
		this->teamType = teamType;
		isReady = false;
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

	void AddUser(const ClientSession* client);

	TeamType JoinAvailableTeam(int clientId);

	bool CanJoinRoom();

};

