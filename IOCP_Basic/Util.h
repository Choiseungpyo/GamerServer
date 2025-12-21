#pragma once
#include <string>
#include <cstddef>

std::string SafeString(const char* buf, size_t maxLen);
void FillFixedString(char* dest, size_t destSize, const std::string& src);