#pragma once
#include <stack>

class Chat
{

	shared_mutex mutex;
	stack<string> msgs;

public:
	Chat() {}
	~Chat() {}

	void AddMsg(const string& msg);
};

