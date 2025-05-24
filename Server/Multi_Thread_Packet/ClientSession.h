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
	
	~ClientSession();

	bool	OnConnect(SOCKADDR_IN* addr);
	bool	IsConnected() const { return mConnected; }

	bool Send(const Packet * pack);

	void Disconnect();

	int GetId() const { return id; }

	SOCKET GetSocket() const { return mSocket; }
	User* GetUser() const {return user; }

	// ��Ŷ �Ľ̽� �ϴ� �Լ���
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
