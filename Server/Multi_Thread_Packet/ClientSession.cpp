#include "stdafx.h"

ClientSession::ClientSession(SOCKET sock, int id) : mSocket(sock), mConnected(false), sessionManager(SessionManager::GetInstance())
{
	unique_lock<shared_mutex> lock(mutex);
	memset(&mClientAddr, 0, sizeof(SOCKADDR_IN));
	user = new User(id);
}

ClientSession::~ClientSession()
{
	unique_lock<shared_mutex> lock(mutex);
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
	unique_lock<shared_mutex> lock(mutex);
	memcpy(&mClientAddr, addr, sizeof(SOCKADDR_IN));
	
	int addrlen = sizeof(SOCKADDR_IN);
	getpeername(mSocket, (SOCKADDR *)&mClientAddr, &addrlen);

	cout << "client Connected : IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	mConnected = true;

	return true;
}

bool ClientSession::IsConnected() const
{
	shared_lock<shared_mutex> lock(mutex);
	return mConnected;
}



// 플레이어를 스폰 한다.
//void ClientSession::SpawnPlayers()
//{
//	PACKET_S_ID pack;
//	pack.id = id;
//	Send(&pack);
//}


/*
함수명 : Disconnect()
인자값 :
기능 : 접속 종료된 socket을 지움.
*/
void ClientSession::Disconnect()
{
	unique_lock<shared_mutex> lock(mutex);
	cout << "client disconnected IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	closesocket(mSocket);
	
	mConnected = false;
}

bool ClientSession::Send(const Packet* pack) const
{
	shared_lock<shared_mutex> lock(mutex);

	if (!IsConnected())
		return false;

	cout << "send in...." << endl;

	char* sendBuf = new char[pack->Length];
	memcpy(sendBuf, pack, pack->Length);
	int re = send(mSocket, sendBuf, pack->Length, 0);
	if (re == SOCKET_ERROR)
	{
		delete[] sendBuf;
		cout << "send error..." << endl;
		return false;
	}

	cout << "Type : " << pack->Type << endl << "  IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;
	delete[] sendBuf;
	return true;
}

SOCKET ClientSession::GetSocket() const
{
	shared_lock<shared_mutex> lock(mutex);
	return mSocket;
}

User* ClientSession::GetUser() const
{
	shared_lock<shared_mutex> lock(mutex);
	return user;
}

void ClientSession::Send(PTYPE ptype) const
{
	PACKET pack;
	pack.Type = ptype;
	pack.Length = sizeof(PACKET);

	Send(&pack);
}

void ClientSession::Send_Id()
{
	PACKET_S_C_ID pack;
	pack.id = user->GetId();
	Send(&pack);
}