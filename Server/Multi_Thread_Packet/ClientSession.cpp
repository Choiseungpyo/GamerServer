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

	return true;
}



// �÷��̾ ���� �Ѵ�.
//void ClientSession::SpawnPlayers()
//{
//	PACKET_S_ID pack;
//	pack.id = id;
//	Send(&pack);
//}

/*
�Լ��� : Send()
���ڰ� : Packet * pack
��� : ���޹��� Packet�� client�� ������.
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