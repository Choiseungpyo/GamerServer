#include "stdafx.h"
#include "MTServerManager.h"
#include "ClientSession.h"
#include "SessionManager.h"

HWND MTServerManager::g_hMsgWnd = NULL;
MTServerManager* GMTServerManager = nullptr;
SOCK_SET MTServerManager::socks;
SOCKET MTServerManager::m_hsoListen = NULL;
MTServerManager* MTServerManager::instance = NULL;
SessionManager* MTServerManager::sessionManager = SessionManager::GetInstance();

MTServerManager* MTServerManager::GetInstance()
{
	if (!instance)
		instance = new MTServerManager();

	return instance;
}

BOOL MTServerManager::CtrlHandler(DWORD fdwCtrlType)
{
	if (g_hMsgWnd != NULL)
		PostMessage(g_hMsgWnd, WM_DESTROY, 0, 0);
	return TRUE;
}

SOCKET MTServerManager::GetListenSocket(short shPortNo, int nBacklog = SOMAXCONN)
{
	SOCKET hsoListen = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (hsoListen == INVALID_SOCKET)
	{
		cout << "socket failed, code : " << WSAGetLastError() << endl;
		return INVALID_SOCKET;
	}

	SOCKADDR_IN	sa;
	memset(&sa, 0, sizeof(SOCKADDR_IN));
	sa.sin_family = AF_INET;
	sa.sin_port = htons(shPortNo);
	sa.sin_addr.s_addr = htonl(INADDR_ANY);
	LONG lSockRet = bind(hsoListen, (PSOCKADDR)&sa, sizeof(SOCKADDR_IN));
	if (lSockRet == SOCKET_ERROR)
	{
		cout << "bind failed, code : " << WSAGetLastError() << endl;
		closesocket(hsoListen);
		return INVALID_SOCKET;
	}

	lSockRet = listen(hsoListen, nBacklog);
	if (lSockRet == SOCKET_ERROR)
	{
		cout << "listen failed, code : " << WSAGetLastError() << endl;
		closesocket(hsoListen);
		return INVALID_SOCKET;
	}

	return hsoListen;
}


MTServerManager::MTServerManager()
{
	sessionManager = SessionManager::GetInstance();
}

MTServerManager::~MTServerManager()
{
	delete instance;
}

/*
함수명 : Recv()
인자값 :
기능 : client가 전송한 data를 수신함.
*/
char* MTServerManager::Recv(ClientSession* client, SOCKET socket)
{
	char buf[BUFSIZE * 4];

	cout << "recv in...." << endl;

	int re = 0;

	re = recv(socket, buf, sizeof(buf), 0);
	if (re == SOCKET_ERROR || re == 0)
	{
		cout << "recv error.." << endl;
		closesocket(socket);
		sessionManager->DeleteClient(client);
		return NULL;
	}
	buf[re] = '\0';


	return buf;
}

bool MTServerManager::PacketParsing(ClientSession* client, char* buf)
{
	Packet* packet;

	packet = (Packet*)buf;

	cout << "packet type : " << packet->Type << endl;

	switch (packet->Type)
	{
	case C_PLAYER_MOVE:
		client->SetPlayerMovement(packet);
		break;
	}

	if (!client->IsConnected())
		return false;

	return true;
}




LRESULT CALLBACK MTServerManager::WndSockProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	static SOCK_SET* s_pSocks = NULL;

	switch (uMsg)
	{
	case WM_CREATE:
	{
		LPCREATESTRUCT pCS = (LPCREATESTRUCT)lParam;
		s_pSocks = (SOCK_SET*)pCS->lpCreateParams;
	}
	return 0;

	case WM_DESTROY:
	{
		PostQuitMessage(0);
	}
	return 0;

	case WM_ASYNC_SOCKET:
	{
		LONG lErrCode = WSAGETSELECTERROR(lParam);
		if (lErrCode != 0)	//WSAECONNABORTED
		{
			SOCKET sock = (SOCKET)wParam;
			cout << "~~~ socket " << sock << " failed: " << lErrCode << endl;
			closesocket(sock);
			s_pSocks->erase(sock);
			return 0;
		}

		switch (WSAGETSELECTEVENT(lParam))
		{
		case FD_ACCEPT:
		{
			SOCKET hsoListen = (SOCKET)wParam;
			SOCKET sock = accept(hsoListen, NULL, NULL);
			if (sock == INVALID_SOCKET)
			{
				cout << "accept failed, code : " << WSAGetLastError() << endl;
				break;
			}

			WSAAsyncSelect(sock, hWnd, WM_ASYNC_SOCKET, FD_READ | FD_CLOSE);
			s_pSocks->insert(sock);

			//++
			SOCKADDR_IN clientAddr;
			int AddrLen = sizeof(clientAddr);
			getpeername(sock, (SOCKADDR*)&clientAddr, &AddrLen);

			//클라 세션 생성.
			/*
			객체 동적할당후 매니저클래스의 생성함수 불러와서 생성.
			*/
			ClientSession* client = sessionManager->CreateClient(sock);

			//클라접속처리.
			/*
			클라객체 접속함수 호출.
			실패시 접속 종료및 객체 삭제.
			*/
			if (client->OnConnect(&clientAddr) == false)
			{
				cout << "connect error..." << endl;
				client->Disconnect();
				sessionManager->DeleteClient(client);
			}
		}
		break;

		case FD_READ:
		{
			SOCKET sock = (SOCKET)wParam;
			ClientSession* client = sessionManager->GetClient(sock);
			
			if (!client)
				return false;

			char* buf = Recv(client, sock);

			if (!buf)
			{
				sessionManager->DeleteClient(client);
				return 0;
			}
			if (PacketParsing(client, buf) == false)
			{
				sessionManager->DeleteClient(client);
				return 0;
			}
		}
		break;

		case FD_CLOSE:
		{
			SOCKET sock = (SOCKET)wParam;
			closesocket(sock);
			s_pSocks->erase(sock);
			cout << " ==> Client " << sock << " disconnected..." << endl;
		}
		break;
		}
	}
	return 0;
	}

	return DefWindowProc(hWnd, uMsg, wParam, lParam);
}


bool MTServerManager::InitServer()
{
	WSADATA	wsd;
	int nErrCode = WSAStartup(MAKEWORD(2, 2), &wsd);
	if (nErrCode)
	{
		cout << "WSAStartup failed with error : " << nErrCode << endl;
		return false;
	}
	if (!SetConsoleCtrlHandler((PHANDLER_ROUTINE)CtrlHandler, TRUE))
	{
		cout << "SetConsoleCtrlHandler failed, code : " << GetLastError() << endl;
		return false;
	}

	WNDCLASS wcls;
	memset(&wcls, 0, sizeof(wcls));
	wcls.lpfnWndProc = WndSockProc;
	wcls.hInstance = GetModuleHandle(NULL);
	wcls.lpszClassName = _T("WSAAyncSelect");
	if (!RegisterClass(&wcls))
	{
		cout << "send failed, code : " << GetLastError() << endl;
		return false;
	}

	HWND hWnd = CreateWindowEx
	(
		0, wcls.lpszClassName, NULL, 100, 100, 0, 0, 0,
		HWND_MESSAGE, NULL, wcls.hInstance,
		&socks
	);
	if (!hWnd)
	{
		cout << "send failed, code : " << GetLastError() << endl;
		return false;
	}
	g_hMsgWnd = hWnd;
	cout << " ==> Creating hidden window success!!!" << endl;

	m_hsoListen = GetListenSocket(9001);
	if (m_hsoListen == INVALID_SOCKET)
	{
		WSACleanup();
		return false;
	}
	WSAAsyncSelect(m_hsoListen, hWnd, WM_ASYNC_SOCKET, FD_ACCEPT);
	cout << "Start Server..." << endl;
	cout << " ==> Waiting for client's connection......" << endl;
}

void MTServerManager::MesseageLoop()
{
	////////////////////////////////////////////////////////////////////////////
	// Message Loop
	MSG msg;
	while (GetMessage(&msg, NULL, 0, 0))
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
	////////////////////////////////////////////////////////////////////////////
}

void MTServerManager::CloseServer()
{
	if (m_hsoListen != INVALID_SOCKET)
		closesocket(m_hsoListen);
	for (SOCK_SET::iterator it = socks.begin(); it != socks.end(); it++)
		closesocket(*it);
	cout << "Listen socket closed, program terminates..." << endl;
	cout << "End Server..." << endl;
	WSACleanup();
}