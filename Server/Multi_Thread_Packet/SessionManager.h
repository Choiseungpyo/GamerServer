#pragma once

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
		clientlist.clear();
		InitializeCriticalSection(&cs);
	}
	~SessionManager();

	static SessionManager* GetInstance();

	ClientSession* CreateClient(SOCKET sock);

	void DeleteClient(ClientSession* client);

	int IncreaseClientCount();
	int DecreaseClientCount();

	void Broadcast(Packet* packet);
	void BroadcastExceptOneself(Packet* packet, ClientSession* oneself);

	//bool RegisterNickname(ClientSession* client, string nickname)
	//{
	//	// 이미 닉네임을 등록한 경우
	//	if (client->GetNickname() != "")
	//		return false;

	//	for (auto client : clientlist)
	//	{
	//		auto curr_client_nickname = client->GetNickname();

	//		// 이미 닉네임이 존재하는 경우
	//		if (curr_client_nickname == nickname)
	//			return false;
	//	}

	//	client->SetNickname(nickname);
	//	return true;
	//}

	//void SendTo(Packet* packet, std::string nickname)
	//{
	//	EnterCriticalSection(&cs);

	//	for (auto& client : clientlist)
	//	{
	//		if (!client->IsConnected())
	//			continue;

	//		if (!client->GetNickname()._Equal(nickname))
	//			continue;

	//		client->Send(packet);
	//	}

	//	LeaveCriticalSection(&cs);
	//}

	void SendTo(Packet* packet, int id)
	{
		EnterCriticalSection(&cs);

		for (auto& client : clientlist)
		{
			if (!client->IsConnected())
				continue;

			if (client->GetId() != id)
				continue;

			client->Send(packet);
		}

		LeaveCriticalSection(&cs);
	}

	void SetClients_FDSET(fd_set& readfds)
	{
		for (auto clientSession : clientlist) {
			FD_SET(clientSession->mSocket, &readfds);
		}
	}

	int GetClientSize() const { return mClientCount; }

	/*ClientSession* GetClient(int index)
	{
		if (index < 0 || index >= mClientCount)
			return NULL;
		
		ClientList::iterator it = clientlist.begin();

		std::advance(it, index);

		return *it;
	}*/

	ClientSession* GetClient(SOCKET sock)
	{
		for (auto client : clientlist)
		{
			if (client->GetSocket() == sock)
				return client;
		}
		return NULL;
	}

private:
	static SessionManager* instance;
	typedef list<ClientSession*> ClientList;
	ClientList clientlist;

	int mClientCount;


	CRITICAL_SECTION cs;
};