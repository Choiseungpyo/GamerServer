#include "stdafx.h"

SessionManager* SessionManager::instance = nullptr;
unordered_map<SOCKET, ClientSession*> SessionManager::clientMap;
int SessionManager::mClientCount = 0;
shared_mutex SessionManager::mutex;

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
함수명 : CreateClient()
인자값 : SOCKET sock
기능 : clientsession을 만들고 관리 객체에 등록함.
*/
ClientSession * SessionManager::CreateClient(SOCKET sock)
{
	unique_lock<shared_mutex> lock(mutex);
	auto it = clientMap.find(sock);

	// 소켓이 없다면
	if (it == clientMap.end()) {
		std::cout << "create ClientSession.." << std::endl;
		ClientSession* newClient = new ClientSession(sock, clientMap.size());
		clientMap[sock] = newClient;
		return newClient;
	}

	// 이미 존재하는 소켓이라면
	std::cout << "Warning : Client Socket[" << sock << "] is already created." << std::endl;
	return it->second;  // 이미 있는 클라이언트
}

/*
함수명 : DeleteClient()
인자값 : ClientSession * Client
기능 : 접속이 끊기는 clientsession을 삭제함.
*/
void SessionManager::DeleteClient(SOCKET sock, ClientSession* client)
{
	unique_lock<shared_mutex> lock(mutex);
	client->Disconnect();
	clientMap.erase(sock);
}

/*
함수명 : IncreaseClientCount()
인자값 : 
기능 : 접속된 clientsession이 생기면 전체 클라이언트 갯수를 증가시킴. 
	   서로 다른 객체들끼리 동기화시키기 위해 CRITICAL_SECTION을 사용.
*/
int SessionManager::IncreaseClientCount()
{
	unique_lock<shared_mutex> lock(mutex);
	mClientCount++;
	cout << "client count increase.." << endl;

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
	unique_lock<shared_mutex> lock(mutex);
	mClientCount--;
	cout << "client count decrease.." << endl;

	return mClientCount;
}

void SessionManager::Broadcast(const Packet* packet)
{
	shared_lock<shared_mutex> lock(mutex);

	for (auto& client : clientMap)
	{
		if (client.second->IsConnected())
		{
			client.second->Send(packet);
		}
	}

}

void SessionManager::BroadcastExceptOneself(const Packet* packet, ClientSession* oneself)
{
	shared_lock<shared_mutex> lock(mutex);

	for (auto& client : clientMap)
	{
		if (oneself == client.second)
			continue;

		if (client.second->IsConnected())
		{
			client.second->Send(packet);
		}
	}

}

void SessionManager::SendTo(const Packet* packet, SOCKET targetSocket)
{
	shared_lock<shared_mutex> lock(mutex);

	auto it = clientMap.find(targetSocket);

	// 키 값이 있는 경우
	if (it != clientMap.end()) {
		ClientSession* client = it->second;
		client->Send(packet);
	}
	// 키 값이 없는 경우
	else
		cout << "target socket : " << targetSocket << " 존재하지 않음";
}


int SessionManager::GetClientSize() { return mClientCount; }


ClientSession* SessionManager::GetClient(SOCKET sock)
{
	shared_lock<shared_mutex> lock(mutex);
	auto it = clientMap.find(sock);

	// 키 값이 있는 경우
	if (it != clientMap.end()) {
		return it->second;
	}

	cout << "socket : " << sock << " 존재하지 않음";
	return nullptr;
}