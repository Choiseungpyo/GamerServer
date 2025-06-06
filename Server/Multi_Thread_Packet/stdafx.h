// stdafx.h : 자주 사용하지만 자주 변경되지는 않는
// 표준 시스템 포함 파일 또는 프로젝트 관련 포함 파일이
// 들어 있는 포함 파일입니다.
//

#pragma once

#include "targetver.h"

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#define _CRT_NO_SECURE_WARNINGS


#include <stdio.h>
#include <tchar.h>

#include <process.h>
#include <assert.h>
#include <limits.h>


#include <WinSock2.h>

#include <cstdint>

#include <iostream>
#include <list>
#include <string>
#include "Winsock2.h"
#include "set"
typedef std::set<SOCKET> SOCK_SET;

using namespace std;


#pragma comment(lib,"ws2_32")


#define SERVERPORT 8080
#define BUFSIZE 1024
constexpr int NAME_SIZE = 30;
constexpr int ROOM_NAME_SIZE = 64;
constexpr int CHAT_SIZE = 30;


enum TeamType : int{
	RED,
	BLUE
};

enum RoomState : int{
	WAITING,
	PLAYING
};

enum MatchType : int{
	SOLO = 1,
	DUO = 2,
	SQUAD = 4
};

enum InRoomUserState : int {
	UNREADY,
	READY,
	IDLE,
	START
};

enum class PlayerState {
	IDLE,
	MOVE,
	SHOOT,
	RELOAD,
	DEAD
};


#include <tuple>
#include <unordered_map>
#include <shared_mutex>

#include "Packet.h"
#include "MTServerManager.h"
#include "ClientSession.h"
#include "SessionManager.h"
#include "Chat.h"

// 데이터
#include "UserIcon.h"


#include "Game.h"
#include "Room.h"

#include "User.h"
#include "LobbyManager.h"
#include "PlayerEntity.h"