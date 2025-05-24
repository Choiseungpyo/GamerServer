#pragma once
#include "Game.h"
#include <unordered_set>

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
	// int : id값
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
	Room(int no, RoomOption roomOption) 
		:no(no), name(roomOption.roomName), state(WAITING), matchType(roomOption.matchType)
	{}
	
	void SetNo(int no) { this->no = no; }
	
	void SetName(const string& name) { this->name = name; }
	
	void AddUser(const ClientSession* client)
	{
		clientMap[client->GetSocket()] = client;
		int clientId = client->GetId();

		TeamType teamType = JoinAvailableTeam(client->GetId());
		roomUserInfoMap[clientId] = RoomUserInfo(teamType);
	}

	TeamType JoinAvailableTeam(int clientId)
	{
		if (redTeamIds.size() >= (int)matchType) {
			blueTeamIds.insert(clientId);
			return BLUE;
		}
	
		redTeamIds.insert(clientId);
		return RED;
	}

	bool CanJoinRoom()
	{
		// 게임 중일 경우
		if (state == PLAYING)
			return false;

		// 최대 인원에 도달했을 경우
		if (clientMap.size() >= (int)matchType)
			return false;

		return true;
	}

};

