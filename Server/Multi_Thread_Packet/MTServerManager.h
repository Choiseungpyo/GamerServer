#pragma once

/*
서버 관리자
서버 모델이 변경될때마다 제일많이 변경될 부분..
extern 부분을 나중에 싱글턴으로 변경예정.
*/
#include "Winsock2.h"
#include "set"


#define WM_ASYNC_SOCKET	WM_USER + 500
typedef std::set<SOCKET> SOCK_SET;


class MTServerManager
{
public:
	MTServerManager();
	~MTServerManager();
	
	static MTServerManager* GetInstance();
	static bool InitServer();
	void MesseageLoop();
	void CloseServer();


private:
	static HWND g_hMsgWnd;
	static SOCK_SET socks;
	static SOCKET m_hsoListen;

	static MTServerManager* instance;
	static SessionManager* sessionManager;

	static LRESULT CALLBACK WndSockProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	static BOOL CtrlHandler(DWORD fdwCtrlType);
	static SOCKET GetListenSocket(short shPortNo, int nBacklog);
	static char* Recv(ClientSession* client, SOCKET socket);
	static bool PacketParsing(ClientSession* client, char* buf);
};