#pragma once
class User;

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
	ClientSession(SOCKET sock, int id);
	
	~ClientSession();

	bool	OnConnect(SOCKADDR_IN* addr);
	bool	IsConnected() const { return mConnected; }

	bool Send(const Packet * pack);

	void Disconnect();

	SOCKET GetSocket() const { return mSocket; }
	User* GetUser() const {return user; }

	// 클라로 전송
	void Send_Id();

	// 패킷 파싱시 하는 함수들
	void EntryLobby();
	void MoveTitle();

private:
	SessionManager* sessionManager;
	bool			mConnected;
	SOCKET			mSocket;

	SOCKADDR_IN		mClientAddr;
	
	User* user;

	friend class SessionManager;
};
