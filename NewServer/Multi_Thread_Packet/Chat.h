#pragma once
#include <stack>

class Chat
{
	stack<string> msgs;

public:
	Chat() {}
	~Chat() {}

	void AddMsg(const string& msg)
	{
		if (msgs.size() >= 20)
			msgs.pop();

		msgs.push(msg);
	}
};

