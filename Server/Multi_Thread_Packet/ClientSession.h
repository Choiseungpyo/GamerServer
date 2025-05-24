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
	~ClientSession() {}

	bool	OnConnect(SOCKADDR_IN* addr);
	bool	IsConnected() const { return mConnected; }

	bool Send(Packet * pack);

	void Disconnect();

	int GetId() const { return id; }

	SOCKET GetSocket() const { return mSocket; }

private:
	SessionManager* sessionManager;
	bool			mConnected;
	SOCKET			mSocket;

	SOCKADDR_IN		mClientAddr;
	
	int id;
	
	Vector3 pos;
	User* user;

	friend class SessionManager;
};
