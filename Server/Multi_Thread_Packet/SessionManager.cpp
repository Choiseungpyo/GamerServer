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
함수명 : CreateClient()
인자값 : SOCKET sock
기능 : clientsession을 만들고 관리 객체에 등록함.
*/
ClientSession * SessionManager::CreateClient(SOCKET sock)
{
	ClientSession* client = new ClientSession(sock);

	cout << "create ClientSession.." << endl;
	clientlist.push_back(ClientList::value_type(client));
	return client;

}

/*
함수명 : DeleteClient()
인자값 : ClientSession * Client
기능 : 접속이 끊기는 clientsession을 삭제함.
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
함수명 : IncreaseClientCount()
인자값 : 
기능 : 접속된 clientsession이 생기면 전체 클라이언트 갯수를 증가시킴. 
	   서로 다른 객체들끼리 동기화시키기 위해 CRITICAL_SECTION을 사용.
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
함수명 : DecreaseClientCount()
인자값 :
기능 : 접속이 끊기는 clientsession이 생기면 전체 클라이언트 갯수를 감소시킴.
	   서로 다른 객체들끼리 동기화시키기 위해 CRITICAL_SECTION을 사용.
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