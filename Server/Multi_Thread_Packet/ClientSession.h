#pragma once
#include "User.h"

//Ŭ���̾�Ʈ ��ü�� Ŭ����
/*
����� �Լ�
���� Ȯ�� �Լ�
���� ���� �Լ�.
�۽�, �����Լ�.
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
