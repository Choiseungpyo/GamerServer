// stdafx.h : ���� ��������� ���� ��������� �ʴ�
// ǥ�� �ý��� ���� ���� �Ǵ� ������Ʈ ���� ���� ������
// ��� �ִ� ���� �����Դϴ�.
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

// TODO: ���α׷��� �ʿ��� �߰� ����� ���⿡�� �����մϴ�.
