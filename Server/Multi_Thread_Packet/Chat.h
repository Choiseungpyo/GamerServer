#pragma once
#include <stack>

class Chat
{

	shared_mutex mutex;
	stack<string> msgs;

public:
	Chat() {}
	~Chat() {}

	void AddMsg(const string& msg)
	{
		unique_lock<shared_mutex> lock(mutex);

		if (msgs.size() >= 20)
			msgs.pop();

		msgs.push(msg);
	}
};

