#include "stdafx.h"

SessionManager* SessionManager::instance = nullptr;
unordered_map<SOCKET, ClientSession*> SessionManager::clientMap;
int SessionManager::mClientCount = 0;

SessionManager::~SessionManager()
{
	clientMap.clear();

	delete instance;
}
SessionManager* SessionManager::GetInstance()
{
	//  대신 맨 처음에만 초기화하는 용도로 once_flag  사용
	static std::once_flag flag;
	std::call_once(flag, []() {
		instance = new SessionManager();
		});
	return instance;
}



/*
함수명 : CreateClient()
인자값 : SOCKET sock
기능 : clientsession을 만들고 관리 객체에 등록함.
*/
ClientSession * SessionManager::CreateClient(SOCKET sock)
{
	auto it = clientMap.find(sock);

	// 소켓이 없다면
	if (it == clientMap.end()) {
		std::cout << "create ClientSession.." << std::endl;
		ClientSession* newClient = new ClientSession(sock, mClientCount++);
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
	client->Disconnect();
	clientMap.erase(sock);
	mClientCount--;
}


void SessionManager::Broadcast(const Packet* packet)
{
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
	auto it = clientMap.find(targetSocket);

	// 키 값이 없는 경우
	if (it == clientMap.end()) {
		cout << "target socket : " << targetSocket << " 존재하지 않음";
		return;
	}
	
	ClientSession* client = it->second;
	client->Send(packet);
}


int SessionManager::GetClientSize() 
{
	return mClientCount; 
}


ClientSession* SessionManager::GetClient(SOCKET sock)
{
	auto it = clientMap.find(sock);

	// 키 값이 없는 경우
	if (it == clientMap.end()) 
	{
		cout << "socket : " << sock << " 존재하지 않음";
		return nullptr;
	
	}

	return it->second;
}

vector<const ClientSession*> SessionManager::GetClientAll() {
	vector<const ClientSession*> clients;
	for (auto it : clientMap)
	{
		clients.push_back(it.second);
	}

	return clients;
}