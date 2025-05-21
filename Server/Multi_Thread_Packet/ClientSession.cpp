#include "stdafx.h"
#include "ClientSession.h"
#include "MTServerManager.h"
#include "SessionManager.h"

ClientSession::ClientSession(SOCKET sock) : mSocket(sock), mConnected(false), id(0), sessionManager(SessionManager::GetInstance())
{
	memset(&mClientAddr, 0, sizeof(SOCKADDR_IN));
}

/*
�Լ��� : OnConnect()
���ڰ� : SOCKADDR_IN * addr
��� : ���޹��� �ּҸ� �����ϰ�
	   ���޹��� �ּҿ� ���� Ŭ���̾�Ʈ�� �����ϴ��� �ľ���.
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
		1. ������ �ִ� �÷��̾� ������ ����
		2. ���ο� �÷��̾� �߰�	
	*/

	SpawnPlayers();

	return true;
}



// �÷��̾ ���� �Ѵ�.
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
	// ����Ű�� ������ ���
	 // �� ����(0�� ��Ʈ)�� ���ȴ��� Ȯ��
	if (packet_c_move->Directions & (1 << UP))
		pos.z += movementSize;

	// �� ����(1�� ��Ʈ)�� ���ȴ��� Ȯ��
	if (packet_c_move->Directions & (1 << DOWN))
		pos.z -= movementSize;

	// �� ����(2�� ��Ʈ)�� ���ȴ��� Ȯ��
	if (packet_c_move->Directions & (1 << LEFT))
		pos.x -= movementSize;

	// �� ����(3�� ��Ʈ)�� ���ȴ��� Ȯ��
	if (packet_c_move->Directions & (1 << RIGHT))
		pos.x += movementSize;

	PACKET_S_MOVE packet_s_move;
	packet_s_move.Id = id;

	packet_s_move.Pos = pos;

	sessionManager->Broadcast(&packet_s_move);
}


/*
�Լ��� : Send()
���ڰ� : Packet * pack
��� : ���޹��� Packet�� client�� ������.
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
�Լ��� : Disconnect()
���ڰ� :
��� : ���� ����� socket�� ����.
*/
void ClientSession::Disconnect()
{
	cout << "client disconnected IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	sessionManager->DecreaseClientCount();

	//���� �Ŵ������� �� ����
	closesocket(mSocket);
	
	mConnected = false;
}