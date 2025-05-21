#include "stdafx.h"
#include "ClientSession.h"
#include "MTServerManager.h"
#include "SessionManager.h"

ClientSession::ClientSession(SOCKET sock) : mSocket(sock), mConnected(false), id(0), sessionManager(SessionManager::GetInstance())
{
	memset(&mClientAddr, 0, sizeof(SOCKADDR_IN));
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

	/*
		1. 기존에 있는 플레이어 데이터 적용
		2. 새로운 플레이어 추가	
	*/

	SpawnPlayers();

	return true;
}



// 플레이어를 스폰 한다.
void ClientSession::SpawnPlayers()
{
	PACKET_S_ID pack;
	pack.id = id;
	Send(&pack);
}


void ClientSession::SetPlayerMovement(PACKET* pack)
{
	PACKET_C_MOVE* packet_c_move = (PACKET_C_MOVE*)pack;
	mConnected = true;

	std::string message = "Dir Input : ";
	for (int i = 0; i < 4; i++) //sizeof(ps->Directions)
	{
		message += (packet_c_move->Directions & (1 << i)) == 1 ? "true " : "false ";
	}
	cout << message << endl;
	// cout << "Name : " << ps->Name << endl << "  recv message : " << ps->Data << "  IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	float movementSize = 0.05f;
	// 방향키를 눌렀을 경우
	 // 상 방향(0번 비트)이 눌렸는지 확인
	if (packet_c_move->Directions & (1 << UP))
		pos.z += movementSize;

	// 하 방향(1번 비트)이 눌렸는지 확인
	if (packet_c_move->Directions & (1 << DOWN))
		pos.z -= movementSize;

	// 좌 방향(2번 비트)이 눌렸는지 확인
	if (packet_c_move->Directions & (1 << LEFT))
		pos.x -= movementSize;

	// 우 방향(3번 비트)이 눌렸는지 확인
	if (packet_c_move->Directions & (1 << RIGHT))
		pos.x += movementSize;

	PACKET_S_MOVE packet_s_move;
	packet_s_move.Id = id;

	packet_s_move.Pos = pos;

	sessionManager->Broadcast(&packet_s_move);
}


/*
함수명 : Send()
인자값 : Packet * pack
기능 : 전달받은 Packet을 client에 전송함.
*/
bool ClientSession::Send(Packet * pack)
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