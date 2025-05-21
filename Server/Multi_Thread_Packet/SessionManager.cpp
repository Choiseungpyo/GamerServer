#include "stdafx.h"



SessionManager* SessionManager::instance = nullptr;


SessionManager::~SessionManager()
{
	clientlist.clear();

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
	ClientSession* client = new ClientSession(sock);

	cout << "create ClientSession.." << endl;
	clientlist.push_back(ClientList::value_type(client));
	return client;

}

/*
�Լ��� : DeleteClient()
���ڰ� : ClientSession * Client
��� : ������ ����� clientsession�� ������.
*/
void SessionManager::DeleteClient(ClientSession * client)
{
	list<ClientSession*>::iterator iter;
	for (iter = clientlist.begin(); iter != clientlist.end(); )
	{
		if(*iter == client)
		{
			client->Disconnect();
			iter = clientlist.erase(iter);
			cout << "client remove.." <<endl;
		}
		else
		{
			iter++;
		}
	}
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

	for (auto& client : clientlist)
	{
		if (client->IsConnected())
		{
			client->Send(packet);
		}
	}

	LeaveCriticalSection(&cs);
}

void SessionManager::BroadcastExceptOneself(Packet* packet, ClientSession* oneself)
{
	EnterCriticalSection(&cs);

	for (auto& client : clientlist)
	{
		if (oneself == client)
			continue;

		if (client->IsConnected())
		{
			client->Send(packet);
		}
	}

	LeaveCriticalSection(&cs);
}