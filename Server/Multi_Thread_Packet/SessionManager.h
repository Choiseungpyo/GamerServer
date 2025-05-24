#pragma once
#include <unordered_map>
/*
클라이언트 관리자.
클라이언트 증감이나 생성, 파괴등을 맡음.
접근 제한을 위해 CRITICAL_SECTION을 사용함.
추후 변경 가능.
클라이언트 관리를 위한 stl unorderer_map 사용
*/

class ClientSession;

class SessionManager
{
public:
	SessionManager()
	{
		clientMap.clear();
		InitializeCriticalSection(&cs);
	}
	~SessionManager();

	static SessionManager* GetInstance();

	static ClientSession* CreateClient(SOCKET sock);

	static void DeleteClient(SOCKET sock, ClientSession* client);

	static int IncreaseClientCount();
	static int DecreaseClientCount();

	static void Broadcast(const Packet* packet);
	static void BroadcastExceptOneself(const Packet* packet, ClientSession* oneself);

	static void SendTo(const Packet* packet, SOCKET targetSocket)
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

	static void SetClients_FDSET(fd_set& readfds)
	{
		for (const auto& pair : clientMap) {
			ClientSession* clientSession = pair.second; 
			if (clientSession) {
				FD_SET(clientSession->mSocket, &readfds);
			}
		}
	}

	static int GetClientSize() { return mClientCount; }


	static ClientSession* GetClient(SOCKET sock)
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
	static unordered_map<SOCKET, ClientSession*> clientMap;

	static int mClientCount;


	static CRITICAL_SECTION cs;
};