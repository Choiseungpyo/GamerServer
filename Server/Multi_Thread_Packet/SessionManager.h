#pragma once
#include <unordered_map>
/*
Ŭ���̾�Ʈ ������.
Ŭ���̾�Ʈ �����̳� ����, �ı����� ����.
���� ������ ���� CRITICAL_SECTION�� �����.
���� ���� ����.
Ŭ���̾�Ʈ ������ ���� stl unorderer_map ���
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

		// Ű ���� �ִ� ���
		if (it != clientMap.end()) {
			ClientSession* client = it->second;
			client->Send(packet);
		}
		// Ű ���� ���� ���
		else 
			cout << "target socket : " << targetSocket << " �������� ����";
		

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

		// Ű ���� �ִ� ���
		if (it != clientMap.end()) {
			return it->second;
		}
		
		cout << "socket : " << sock << " �������� ����";
		return nullptr;
	}

private:
	static SessionManager* instance;
	static unordered_map<SOCKET, ClientSession*> clientMap;

	static int mClientCount;


	static CRITICAL_SECTION cs;
};