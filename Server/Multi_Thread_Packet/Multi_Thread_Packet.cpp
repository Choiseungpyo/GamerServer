// Multi_Thread_Packet.cpp : �ܼ� ���� ���α׷��� ���� �������� �����մϴ�.
//

#include "stdafx.h"
#include "MTServerManager.h"
#include "SessionManager.h"
#include "ClientSession.h"
/*
���α׷� ����
Multi Thread ����� TCP/IP Echo server
stl list�� ����Ͽ� ������ client���� �����ص�.
extern�� �̿��Ͽ� ManagerŬ�������� ���� ������.
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

