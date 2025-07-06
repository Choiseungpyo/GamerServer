// Multi_Thread_Packet.cpp : 콘솔 응용 프로그램에 대한 진입점을 정의합니다.
//

#include "stdafx.h"
#include "MTServerManager.h"
#include "SessionManager.h"
#include "ClientSession.h"
/*
프로그램 설명
Multi Thread 기반의 TCP/IP Echo server
stl list를 사용하여 접속한 client들을 저장해둠.
extern을 이용하여 Manager클래스들을 전역 생성함.
*/


int _tmain(int argc, _TCHAR* argv[])
{
	SessionManager* sessionManager = SessionManager::GetInstance();
	MTServerManager* GMTServerManager = MTServerManager::GetInstance();

	if (GMTServerManager->InitServer() == false)
		return -1;

	GMTServerManager->MesseageLoop();
	GMTServerManager->CloseServer();

	return 0;
}

