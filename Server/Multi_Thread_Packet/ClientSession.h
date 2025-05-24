#pragma once
#include "User.h"

//클라이언트 객체용 클래스
/*
연결용 함수
연결 확인 함수
연결 해제 함수.
송신, 수신함수.
*/

class ClientSession;
class SessionManager;

class ClientSession
{
public:
	ClientSession(SOCKET sock);
	
	~ClientSession();

	bool	OnConnect(SOCKADDR_IN* addr);
	bool	IsConnected() const { return mConnected; }

	bool Send(const Packet * pack);

	void Disconnect();

	int GetId() const { return id; }

	SOCKET GetSocket() const { return mSocket; }
	User* GetUser() const {return user; }

	// 패킷 파싱시 하는 함수들
	void EntryLobby() 
	{
		user->SetState(LOBBY);
	}

	void MoveTitle() 
	{
		user->SetState(TITLE);
	}

private:
	SessionManager* sessionManager;
	bool			mConnected;
	SOCKET			mSocket;

	SOCKADDR_IN		mClientAddr;
	
	int id;
	
	User* user;

	friend class SessionManager;
};
