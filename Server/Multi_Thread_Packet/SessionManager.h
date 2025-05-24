#pragma once
#include <unordered_map>
/*
클라이언트 관리자.
클라이언트 증감이나 생성, 파괴등을 맡음.
접근 제한을 위해 CRITICAL_SECTION을 사용함.
추후 변경 가능.
클라이언트 관리를 위한 stl list 사용
추후 stl map으로 변경 예정.
*/

class ClientSession;

class SessionManager
{
public:
	SessionManager() : mClientCount(0)
	{
		clientMap.clear();
		InitializeCriticalSection(&cs);
	}
	~SessionManager();

	static SessionManager* GetInstance();

	ClientSession* CreateClient(SOCKET sock);

	void DeleteClient(SOCKET sock, ClientSession* client);

	int IncreaseClientCount();
	int DecreaseClientCount();

	void Broadcast(Packet* packet);
	void BroadcastExceptOneself(Packet* packet, ClientSession* oneself);

	void SendTo(Packet* packet, SOCKET targetSocket)
	{
		EnterCriticalSection(&cs);
		
		auto it = clientMap.find(targetSocket);

		// 키 값이 있는 경우
		if (it != clientMap.end()) {
			ClientSession* client = it->second;
			client->Send(packet);
		}
		// 키 값이 없는 경우
		else 
			cout << "target socket : " << targetSocket << " 존재하지 않음";
		

		LeaveCriticalSection(&cs);
	}

	void SetClients_FDSET(fd_set& readfds)
	{
		for (const auto& pair : clientMap) {
			ClientSession* clientSession = pair.second; 
			if (clientSession) {
				FD_SET(clientSession->mSocket, &readfds);
			}
		}
	}

	int GetClientSize() const { return mClientCount; }


	ClientSession* GetClient(SOCKET sock)
	{
		auto it = clientMap.find(sock);

		// 키 값이 있는 경우
		if (it != clientMap.end()) {
			return it->second;
		}
		
		cout << "socket : " << sock << " 존재하지 않음";
		return nullptr;
	}

private:
	static SessionManager* instance;
	unordered_map<SOCKET, ClientSession*> clientMap;

	int mClientCount;


	CRITICAL_SECTION cs;
};