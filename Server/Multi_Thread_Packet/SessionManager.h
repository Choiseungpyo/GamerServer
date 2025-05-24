#pragma once
#include <unordered_map>
/*
Ŭ���̾�Ʈ ������.
Ŭ���̾�Ʈ �����̳� ����, �ı����� ����.
���� ������ ���� CRITICAL_SECTION�� �����.
���� ���� ����.
Ŭ���̾�Ʈ ������ ���� stl list ���
���� stl map���� ���� ����.
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

		// Ű ���� �ִ� ���
		if (it != clientMap.end()) {
			return it->second;
		}
		
		cout << "socket : " << sock << " �������� ����";
		return nullptr;
	}

private:
	static SessionManager* instance;
	unordered_map<SOCKET, ClientSession*> clientMap;

	int mClientCount;


	CRITICAL_SECTION cs;
};