#define ENABLE_MYSQL
#include "IOCP_EchoServer.h"
#include "Logger.h"
#include <iostream>

int main()
{
    Logger::Instance().Write(LogTag::SERVERSTART, "main 시작");

    IOCP_EchoServer server(9000);
    if (server.Start())
    {
        Logger::Instance().Write(LogTag::SERVERSTART, "server.Start 성공");

        std::cout << "server started. press q to quit.\n";
        char c;
        while (std::cin >> c)
        {
            if (c == 'q' || c == 'Q') break;
        }

        Logger::Instance().Write(LogTag::SERVERSTOP, "server.Stop 호출");
        server.Stop();
    }
    else
    {
        Logger::Instance().Write(LogTag::SYSTEMERROR, "server.Start 실패");
        std::cout << "server start failed\n";
    }

    Logger::Instance().Write(LogTag::SERVERSTOP, "main 종료");
    return 0;
}