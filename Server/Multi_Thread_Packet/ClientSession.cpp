#include "stdafx.h"
#include "ClientSession.h"
#include "MTServerManager.h"
#include "SessionManager.h"

ClientSession::ClientSession(SOCKET sock) : mSocket(sock), mConnected(false), id(0), sessionManager(SessionManager::GetInstance())
{
	memset(&mClientAddr, 0, sizeof(SOCKADDR_IN));
	 
}

ClientSession::~ClientSession()
{
	if (user)
		delete user;
	delete sessionManager;
}

/*
함수명 : OnConnect()
인자값 : SOCKADDR_IN * addr
기능 : 전달받은 주소를 저장하고
	   전달받은 주소에 대한 클라이언트가 존재하는지 파악함.
*/
bool ClientSession::OnConnect(SOCKADDR_IN* addr)
{
	memcpy(&mClientAddr, addr, sizeof(SOCKADDR_IN));
	
	int addrlen = sizeof(SOCKADDR_IN);
	getpeername(mSocket, (SOCKADDR *)&mClientAddr, &addrlen);

	cout << "client Connected : IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	mConnected = true;
	id = sessionManager->IncreaseClientCount() - 1;

	return true;
}



// 플레이어를 스폰 한다.
//void ClientSession::SpawnPlayers()
//{
//	PACKET_S_ID pack;
//	pack.id = id;
//	Send(&pack);
//}

/*
함수명 : Send()
인자값 : Packet * pack
기능 : 전달받은 Packet을 client에 전송함.
*/
bool ClientSession::Send(const Packet * pack)
{
	if (!IsConnected())
		return false;

	cout << "send in...." << endl;

	char* sendBuf = new char[pack->Length];
	memcpy(sendBuf, pack, pack->Length);
	int re = send(mSocket, sendBuf, pack->Length, 0);
	if (re == SOCKET_ERROR)
	{
		delete[] sendBuf;
		cout << "send error..." <<endl; 
		return false;
	}

	cout << "Type : " << pack->Type << endl << "  IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;
	delete[] sendBuf;
	return true;
}


/*
함수명 : Disconnect()
인자값 :
기능 : 접속 종료된 socket을 지움.
*/
void ClientSession::Disconnect()
{
	cout << "client disconnected IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	sessionManager->DecreaseClientCount();

	//세션 매니저에서 수 감소
	closesocket(mSocket);
	
	mConnected = false;
}