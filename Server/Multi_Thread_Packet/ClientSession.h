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

	// 클라로 패킷을 보내는 함수들
	void Send(Ptype pType)
	{
		switch (pType)
		{
			case S_C_ID:
			{
				PACKET_S_C_ID packet;
				packet.id = id;

				Send(&packet);
			}
			break;

			case S_C_ENTRY_LOBBY:
			{
				PACKET_S_C_ENTRY_LOBBY packet;
				packet.id = id;
				strncpy_s(packet.name, "123", sizeof(packet.name));
				packet.name[sizeof(packet.name) - 1] = '\0';  // 꼭 종료문자 추가

				Send(&packet);
			}
			break;

			case S_C_ENTRY_ROOM:
			//{
			//	PACKET_S_C_ENTRY_ROOM packet;
			//	packet.id = id;
			//	packet.roomNo = LobbyManager.instance.
			//	strcpy(packet.name, "123");

			//	Send(&packet);
			// }
			break;

		case S_C_CREATE_ROOM:

			break;

		case S_C_ENTRY_RANDOMROOM:

			break;

		case S_C_MOVE_TITLE:

			break;
		
		default:
			break;
		}
	}


	// 패킷 파싱시 하는 함수들
	void EntryLobby();
	void MoveTitle();

private:
	SessionManager* sessionManager;
	bool			mConnected;
	SOCKET			mSocket;

	SOCKADDR_IN		mClientAddr;
	
	int id;
	
	User* user;

	friend class SessionManager;
};
