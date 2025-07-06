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

	bool OnConnect(SOCKADDR_IN* addr);
	bool IsConnected() const;

	void Disconnect();

	bool Send(const Packet* pack) const;

	SOCKET GetSocket() const;

	User* GetUser() const;

	// 클라로 전송
	void Send_Id();
	void Send(PTYPE ptype) const;

private:
	SessionManager* sessionManager;
	bool			mConnected;
	SOCKET			mSocket;

	SOCKADDR_IN		mClientAddr;
	
	User* user;

	friend class SessionManager;
};
