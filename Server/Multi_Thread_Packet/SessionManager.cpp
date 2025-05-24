#include "stdafx.h"



SessionManager* SessionManager::instance = nullptr;


SessionManager::~SessionManager()
{
	clientMap.clear();

	delete instance;
}
SessionManager* SessionManager::GetInstance()
{
	if (!instance)
		return new SessionManager();

	return instance;
}



/*
�Լ��� : CreateClient()
���ڰ� : SOCKET sock
��� : clientsession�� ����� ���� ��ü�� �����.
*/
ClientSession * SessionManager::CreateClient(SOCKET sock)
{
	auto it = clientMap.find(sock);
	if (it != clientMap.end()) {
		cout << "create ClientSession.." << endl;
		clientMap[sock] = new ClientSession(sock);
		return clientMap[sock];
	}
	std::cout << "Warning : Client Socket[" << sock << "] is already created. " << std::endl;
}

/*
�Լ��� : DeleteClient()
���ڰ� : ClientSession * Client
��� : ������ ����� clientsession�� ������.
*/
void SessionManager::DeleteClient(SOCKET sock, ClientSession* client)
{
	client->Disconnect();
	clientMap.erase(sock);
}

/*
�Լ��� : IncreaseClientCount()
���ڰ� : 
��� : ���ӵ� clientsession�� ����� ��ü Ŭ���̾�Ʈ ������ ������Ŵ. 
	   ���� �ٸ� ��ü�鳢�� ����ȭ��Ű�� ���� CRITICAL_SECTION�� ���.
*/
int SessionManager::IncreaseClientCount()
{
	EnterCriticalSection(&cs);
	mClientCount++;
	cout << "client count increase.." << endl;
	LeaveCriticalSection(&cs);

	return mClientCount;
}

/*
�Լ��� : DecreaseClientCount()
���ڰ� :
��� : ������ ����� clientsession�� ����� ��ü Ŭ���̾�Ʈ ������ ���ҽ�Ŵ.
	   ���� �ٸ� ��ü�鳢�� ����ȭ��Ű�� ���� CRITICAL_SECTION�� ���.
*/
int SessionManager::DecreaseClientCount()
{
	EnterCriticalSection(&cs);
	mClientCount--;
	cout << "client count decrease.." << endl;
	LeaveCriticalSection(&cs);

	return mClientCount;
}

void SessionManager::Broadcast(Packet* packet)
{
	EnterCriticalSection(&cs);

	for (auto& client : clientMap)
	{
		if (client.second->IsConnected())
		{
			client.second->Send(packet);
		}
	}

	LeaveCriticalSection(&cs);
}

void SessionManager::BroadcastExceptOneself(Packet* packet, ClientSession* oneself)
{
	EnterCriticalSection(&cs);

	for (auto& client : clientMap)
	{
		if (oneself == client.second)
			continue;

		if (client.second->IsConnected())
		{
			client.second->Send(packet);
		}
	}

	LeaveCriticalSection(&cs);
}