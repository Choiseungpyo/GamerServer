#pragma once

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

	}
	~SessionManager();

	static SessionManager* GetInstance();

	static ClientSession* CreateClient(SOCKET sock);

	static void DeleteClient(SOCKET sock, ClientSession* client);

	static void Broadcast(const Packet* packet);
	static void BroadcastExceptOneself(const Packet* packet, ClientSession* oneself);

	static void SendTo(const Packet* packet, SOCKET targetSocket);

	static int GetClientSize();


	static ClientSession* GetClient(SOCKET sock);
	static vector<const ClientSession*> GetClientAll();

private:
	static SessionManager* instance;
	static unordered_map<SOCKET, ClientSession*> clientMap;

	static int mClientCount;


	static shared_mutex mutex;
};