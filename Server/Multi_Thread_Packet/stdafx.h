// stdafx.h : 자주 사용하지만 자주 변경되지는 않는
// 표준 시스템 포함 파일 또는 프로젝트 관련 포함 파일이
// 들어 있는 포함 파일입니다.
//

#pragma once

#include "targetver.h"

#define _WINSOCK_DEPRECATED_NO_WARNINGS


#include <stdio.h>
#include <tchar.h>

#include <process.h>
#include <assert.h>
#include <limits.h>


#include <WinSock2.h>

#include <cstdint>

#include <iostream>
#include <list>

using namespace std;


#pragma comment(lib,"ws2_32")


#define SERVERPORT 8080
#define BUFSIZE 1024
#include "Packet.h"
#include "ClientSession.h"
#include "SessionManager.h"

// TODO: 프로그램에 필요한 추가 헤더는 여기에서 참조합니다.
