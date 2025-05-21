#pragma once
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

	//�÷��̾� ������ ���� �Լ�
	void SetPlayerMovement(PACKET* pack);

	// �÷��̾ ���� �Ѵ�.
	void SpawnPlayers();
	

	std::string GetNickname() const
	{
		return nickname;
	}

	void SetNickname(std::string nickname)
	{
		this->nickname = nickname;
	}

	int GetId() const { return id; }

	SOCKET GetSocket() const { return mSocket; }

private:
	SessionManager* sessionManager;
	bool			mConnected;
	SOCKET			mSocket;

	SOCKADDR_IN		mClientAddr;
	
	int id;
	
	Vector3 pos;

	std::string nickname;
	
	friend class SessionManager;
};
