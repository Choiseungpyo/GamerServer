#pragma once
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

	//플레이어 움직임 설정 함수
	void SetPlayerMovement(PACKET* pack);

	// 플레이어를 스폰 한다.
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
