#include "Util.h"
#include <cstring>
#include "Logger.h"
#include <sstream>

std::string SafeString(const char* buf, size_t maxLen)
{
    if (!buf)
    {
        Logger::Instance().Write(LogTag::SYSTEMERROR, "SafeString: buf is null");
        return std::string();
    }

    size_t n = 0;
    while (n < maxLen && buf[n] != '\0') n++;

    if (n == maxLen)
    {
        std::ostringstream oss;
        oss << "SafeString: no null-terminator within maxLen=" << (unsigned long long)maxLen;
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
    }

    return std::string(buf, n);
}

void FillFixedString(char* dest, size_t destSize, const std::string& src)
{
    if (!dest || destSize == 0)
    {
        Logger::Instance().Write(LogTag::SYSTEMERROR, "FillFixedString: invalid dest/destSize");
        return;
    }

    std::memset(dest, 0, destSize);
    size_t n = src.size();
    if (n >= destSize) n = destSize - 1;
    std::memcpy(dest, src.data(), n);
}